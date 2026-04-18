using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] public Animator armsAnimator;

    [Header("Unlockable Abilities")]
    [SerializeField] private bool hasJetpack = false;
    [SerializeField] private bool hasElytra = true;
    [SerializeField] private bool hasWallJumpBoots = true;
    [SerializeField] private bool hasPlatformBoots = true;
    [SerializeField] private bool hasDoubleJump = true;

    [Header("Movement")]
    [SerializeField] private float maxGroundSpeed = 10f;
    [SerializeField] private float maxAirSpeed = 10f;
    [SerializeField] private float groundAcceleration = 35f;
    [SerializeField] private float airAcceleration = 15f;
    [SerializeField] private float groundFriction = 30f;
    [SerializeField] private float airFriction = 1f;

    [Header("Sprinting")]
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;
    [SerializeField] private float sprintAccelMultiplier = 1.3f;

    [Header("Sliding")]
    [SerializeField] private float slideFriction = 3f;
    [SerializeField] private float slideBoostSpeed = 2f;
    [SerializeField] private float slideHeight = 1f;
    [SerializeField] private float slideMinSpeed = 1.5f;
    [SerializeField] private float slideSlopeBoost = 15f;
    [SerializeField] private float slideMaxSpeed = 20f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = 9.7f;
    [SerializeField] private float groundStickDownForce = 2f;

    [Header("Jetpack Double Jump")]
    [SerializeField] private float jetpackBoost = 8f;
    [SerializeField] private float jetpackFuelCost = 25f;

    [Header("Jetpack Hold Boost")]
    [SerializeField] private float jetpackHoldForce = 14f;
    [SerializeField] private float jetpackHoldMaxDuration = 0.75f;
    [SerializeField] private float jetpackHoldFuelPerSec = 40f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallJumpUpForce = 7f;
    [SerializeField] private float wallJumpAwayForce = 8f;
    [SerializeField] private float wallSlideGravity = 2f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Elytra Physics Flight")]
    [SerializeField] private float elytraDrag = 0.01f;
    [SerializeField] private float elytraLiftCoefficient = 0.08f;
    [SerializeField] private float elytraMaxSpeed = 50f;
    [SerializeField] private float elytraMinSpeedToGlide = 3f;
    [SerializeField] private float elytraGravity = 9.7f;
    [SerializeField] private float elytraSteerSpeed = 5f;
    [SerializeField] private Transform cameraTransform;

    [Header("Platform Creation")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private float platformDuration = 4f;
    [SerializeField] private AudioClip platformSound;
    [SerializeField] private int maxPlatformNum;
    private int currentPlatformNum;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landingSound;
    [SerializeField] private AudioClip jetpackBoostSound;
    [SerializeField] private AudioClip jetpackLoopSound;

    [Header("Arm Bobbing")]
    [SerializeField] private Transform armPrefab;
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSmooth = 10f;
    [Header("UI Settings")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider platformSlider;
    [SerializeField] private Slider jetpackSlider;
    [SerializeField] private RawImage hasJetpackImage;
    [SerializeField] private RawImage hasDoubleJumpImage;
    [SerializeField] private RawImage hasPlatformImage;
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color unlockedColor = Color.white;

    private float bobTimer = 0f;
    private Vector3 armDefaultPos;

    // Internal state
    private CharacterController controller;
    private PlayerStats stats;

    private bool canDoubleJump = false;

    private Vector3 horizontalVel;
    private float verticalVel;

    private bool isWallSliding;
    private Vector3 wallNormal;

    // elytra
    private bool isElytraGliding;
    private Vector3 elytraVelocity;

    // jetpack hold
    private bool isJetpackBoosting;
    private float jetpackBoostTimer;

    // sprint
    private bool isSprinting;

    // sliding
    private bool isSliding;
    private bool wantsSlideOnLand;
    private float normalHeight;
    private float normalCenter;

    // platform creation
    private bool canCreatePlatform;
    private AudioSource audioSource;

    // landing detection
    private bool wasGrounded;

    // jetpack loop sound
    private AudioSource jetpackLoopSource;

    // public read-only
    public bool IsGrounded => controller.isGrounded;
    public bool IsElytraGliding => isElytraGliding;
    public bool IsWallSliding => isWallSliding;
    public bool IsJetpackBoosting => isJetpackBoosting;
    public bool IsSliding => isSliding;
    public bool IsSprinting => isSprinting;
    public float CurrentSpeed => isElytraGliding
        ? elytraVelocity.magnitude
        : new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;

    // public unlock properties
    public bool HasJetpack        { get => hasJetpack;        set => hasJetpack = value; }
    public bool HasElytra         { get => hasElytra;         set => hasElytra = value; }
    public bool HasWallJumpBoots  { get => hasWallJumpBoots;  set => hasWallJumpBoots = value; }
    public bool HasPlatformBoots  { get => hasPlatformBoots;  set => hasPlatformBoots = value; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();
        audioSource = GetComponent<AudioSource>();

        normalHeight = controller.height;
        normalCenter = controller.center.y;

        // separate audio source for jetpack loop so it can play alongside one-shots
        if (jetpackLoopSound != null)
        {
            jetpackLoopSource = gameObject.AddComponent<AudioSource>();
            jetpackLoopSource.clip = jetpackLoopSound;
            jetpackLoopSource.loop = true;
            jetpackLoopSource.playOnAwake = false;
        }

        if (stats == null)
            Debug.LogWarning("PlayerController: No PlayerStats – jetpack fuel ignored.");

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogWarning("PlayerController: No camera found – elytra won't work.");
        }
        if (armPrefab)
        {
            armDefaultPos = armPrefab.localPosition;
        }


        if (jetpackSlider != null && stats != null)
        {
            jetpackSlider.maxValue = stats.MaxFuel; 
            jetpackSlider.value = stats.CurrentFuel;
        }
        currentPlatformNum = maxPlatformNum;
        if (platformSlider != null)
        {
            platformSlider.maxValue = maxPlatformNum;
            platformSlider.value = currentPlatformNum;
        }
        if (hpSlider != null && stats != null)
        {
            hpSlider.maxValue = stats.MaxHealth;
            hpSlider.value = stats.GetHealth();
        }
        UpdateJetpackIcon();
        UpdatePlatformIcon();
        UpdateDoubleJumpIcon();
    }

    void Update()
    {
        if (isElytraGliding)
            UpdateElytraFlight();
        else
            UpdateNormalMovement();

        HandlePlatformCreation();
        HandleJetpackLoopSound();
        HandleArmBob();
        if (hpSlider != null && stats != null)
        {
            hpSlider.value = stats.GetHealth();
        }
        if (jetpackSlider != null && stats != null)
        {
            jetpackSlider.value = stats.CurrentFuel;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void HandleJetpackLoopSound()
    {
        if (jetpackLoopSource == null) return;

        if (isJetpackBoosting && !jetpackLoopSource.isPlaying)
            jetpackLoopSource.Play();
        else if (!isJetpackBoosting && jetpackLoopSource.isPlaying)
            jetpackLoopSource.Stop();
    }

    // platform creation
    void HandlePlatformCreation()
    {
        if (!hasPlatformBoots) return;

        // reset on landing so next jump allows one platform
        if (controller.isGrounded)
            canCreatePlatform = true;

        if (Input.GetKeyDown(KeyCode.E) && canCreatePlatform && !controller.isGrounded)
        {
            CreatePlatform();
            canCreatePlatform = false;
        }
    }

    void CreatePlatform()
    {
        if (currentPlatformNum < 1)
        {
            return;
        }
        // spawn just below the player's feet
        Vector3 spawnPos = transform.position + Vector3.down * 2f;
        GameObject newPlatform = Instantiate(platformPrefab, spawnPos, Quaternion.identity);
        Destroy(newPlatform, platformDuration);

        if (platformSound && audioSource != null)
            audioSource.PlayOneShot(platformSound);
        currentPlatformNum -= 1;
        platformSlider.value = currentPlatformNum;
    }

    // normal movement
    void UpdateNormalMovement()
    {
        bool grounded = controller.isGrounded;

        // landing detection
        if (grounded && !wasGrounded)
        {
            PlaySound(landingSound);
        }
        wasGrounded = grounded;

        if (grounded && verticalVel < 0f)
        {
            verticalVel = -groundStickDownForce;
            isWallSliding = false;
            isJetpackBoosting = false;
            jetpackBoostTimer = 0f;
            canDoubleJump = true;

            // buffered slide from air
            if (wantsSlideOnLand)
            {
                wantsSlideOnLand = false;
                TryStartSlide();
            }
        }

        HandleSprinting(grounded);
        HandleSliding(grounded);
        HandleHorizontalMovement(grounded);
        DetectWall(grounded);
        HandleJumping(grounded);
        HandleJetpackHoldBoost(grounded);
        ApplyNormalGravity(grounded);

        Vector3 motion = horizontalVel + Vector3.up * verticalVel;
        controller.Move(motion * Time.deltaTime);
    }

    // sprinting: hold shift
    void HandleSprinting(bool grounded)
    {
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float moveV = Input.GetAxisRaw("Vertical");
        isSprinting = wantsSprint && grounded && !isSliding && moveV > 0.1f;
    }

    // sliding: hold to slide, let go to stop
    private bool SlideKeyHeld => Input.GetKey(KeyCode.LeftControl);
    private bool SlideKeyDown => Input.GetKeyDown(KeyCode.LeftControl);

    void HandleSliding(bool grounded)
    {
        // start slide on initial press
        if (SlideKeyDown)
        {
            if (grounded)
                TryStartSlide();
            else
                wantsSlideOnLand = true;
        }

        // let go = stop sliding
        if (isSliding && !SlideKeyHeld)
            TryStopSlide();

        // let go in air = cancel buffer
        if (!SlideKeyHeld)
            wantsSlideOnLand = false;

        if (isSliding && grounded)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, GetGroundNormal());
            Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, GetGroundNormal()).normalized;

            if (slopeAngle > 2f)
            {
                float slopeComponent = Vector3.Dot(horizontalVel.normalized, slopeDir);
                if (slopeComponent > 0f)
                    horizontalVel += slopeDir * slideSlopeBoost * Time.deltaTime;
            }

            if (horizontalVel.magnitude > slideMaxSpeed)
                horizontalVel = horizontalVel.normalized * slideMaxSpeed;

            if (horizontalVel.magnitude < slideMinSpeed)
                TryStopSlide();
        }
    }

    void TryStartSlide()
    {
        float speed = new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;
        if (speed < slideMinSpeed) return;

        isSliding = true;
        isSprinting = false;

        controller.height = slideHeight;
        controller.center = new Vector3(0f, slideHeight / 2f, 0f);

        // kick in slide direction
        Vector3 slideDir = horizontalVel.normalized;
        if (slideDir.sqrMagnitude < 0.01f)
            slideDir = transform.forward;

        horizontalVel = slideDir * (speed + slideBoostSpeed);
    }

    void TryStopSlide()
    {
        // ceiling check — stay crouched if something is above
        float checkDist = normalHeight - slideHeight;
        if (Physics.Raycast(transform.position + Vector3.up * slideHeight, Vector3.up, checkDist + 0.1f))
            return;

        isSliding = false;
        controller.height = normalHeight;
        controller.center = new Vector3(0f, normalCenter, 0f);
    }

    // horizontal movement
    void HandleHorizontalMovement(bool grounded)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 wishDir = (transform.right * h + transform.forward * v).normalized;

        if (isSliding)
        {
            // only friction while sliding, no input steering
            float friction = slideFriction;
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, friction * Time.deltaTime);
            return;
        }

        float accel, maxSpd, friction2;

        if (grounded)
        {
            accel = groundAcceleration;
            maxSpd = maxGroundSpeed;
            friction2 = groundFriction;

            if (isSprinting)
            {
                maxSpd *= sprintSpeedMultiplier;
                accel *= sprintAccelMultiplier;
            }
        }
        else
        {
            accel = airAcceleration;
            maxSpd = maxAirSpeed;
            friction2 = airFriction;
        }

        if (wishDir.sqrMagnitude > 0.01f)
        {
            float currentSpeedInWishDir = Vector3.Dot(horizontalVel, wishDir);
            float addSpeed = Mathf.Max(0f, maxSpd - currentSpeedInWishDir);
            float accelAmount = Mathf.Min(accel * Time.deltaTime, addSpeed);
            horizontalVel += wishDir * accelAmount;
        }
        else if (grounded)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, friction2 * Time.deltaTime);
        }

        if (grounded && horizontalVel.magnitude > maxSpd)
            horizontalVel = Vector3.MoveTowards(horizontalVel, horizontalVel.normalized * maxSpd, friction2 * Time.deltaTime);
    }

    // wall detection
    void DetectWall(bool grounded)
    {
        isWallSliding = false;

        if (!hasWallJumpBoots) return;
        if (grounded || verticalVel > 0f) return;

        Vector3[] dirs = { transform.forward, -transform.forward, transform.right, -transform.right };
        foreach (var dir in dirs)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, wallLayer))
            {
                isWallSliding = true;
                wallNormal = hit.normal;
                break;
            }
        }
    }

    // jumping
    void HandleJumping(bool grounded)
    {
        if (!Input.GetButtonDown("Jump")) return;

        // ground jump
        if (grounded)
        {
            verticalVel = Mathf.Sqrt(2f * jumpHeight * gravity);
            if (isSliding) TryStopSlide();
            PlaySound(jumpSound);
            return;
        }

        // wall jump
        if (isWallSliding && hasWallJumpBoots)
        {
            verticalVel = wallJumpUpForce;
            horizontalVel = wallNormal * wallJumpAwayForce;
            isWallSliding = false;
            canDoubleJump = true;
            PlaySound(jumpSound);
            return;
        }

        if (hasDoubleJump && canDoubleJump)
        {
            verticalVel = Mathf.Sqrt(2f * jumpHeight * gravity);
            canDoubleJump = false; 
            PlaySound(jumpSound);
            return;
        }

        // jetpack air jump (unlimited, only limited by fuel)
        if (hasJetpack)
        {
            if (stats != null && stats.CurrentFuel < jetpackFuelCost) return;
            stats?.TryUseFuel(jetpackFuelCost);

            verticalVel = jetpackBoost;

            // start hold boost
            isJetpackBoosting = true;
            jetpackBoostTimer = 0f;
            PlaySound(jetpackBoostSound);
            return;
        }

        // elytra — only after jetpack jumps exhausted (or no jetpack)
        if (hasElytra)
        {
            if (CurrentSpeed >= elytraMinSpeedToGlide)
                EnterElytra();
        }
    }

    // jetpack hold boost
    void HandleJetpackHoldBoost(bool grounded)
    {
        if (grounded || !isJetpackBoosting) return;

        if (Input.GetButton("Jump") && jetpackBoostTimer < jetpackHoldMaxDuration)
        {
            float fuelCost = jetpackHoldFuelPerSec * Time.deltaTime;
            if (stats != null && stats.CurrentFuel < fuelCost)
            {
                isJetpackBoosting = false;
                return;
            }
            stats?.TryUseFuel(fuelCost);

            verticalVel += jetpackHoldForce * Time.deltaTime;
            jetpackBoostTimer += Time.deltaTime;

        }
        else
        {
            isJetpackBoosting = false;
        }
    }

    // gravity
    void ApplyNormalGravity(bool grounded)
    {
        if (grounded) return;

        float grav = (isWallSliding && verticalVel < 0f) ? wallSlideGravity : gravity;
        verticalVel -= grav * Time.deltaTime;
    }

    // elytra flight
    void EnterElytra()
    {
        isElytraGliding = true;
        // carry over current velocity into elytra
        elytraVelocity = horizontalVel + Vector3.up * verticalVel;
    }

    void UpdateElytraFlight()
    {
        // landing detection for elytra too
        if (controller.isGrounded && !wasGrounded)
            PlaySound(landingSound);
        wasGrounded = controller.isGrounded;

        // land if grounded and moving downward
        if (controller.isGrounded && elytraVelocity.y <= 0f)
        {
            ExitElytra();
            return;
        }

        // press jump again to exit elytra
        if (Input.GetButtonDown("Jump"))
        {
            ExitElytra();
            return;
        }

        // fly where the camera looks
        // MouseLook already handles yaw (playerRoot) and pitch (camera local), so we just steer velocity toward the camera's forward direction.
        if (cameraTransform != null)
        {
            Vector3 lookDir = cameraTransform.forward;
            float speed = elytraVelocity.magnitude;

            // smoothly steer velocity toward where the camera points
            elytraVelocity = Vector3.Lerp(
                elytraVelocity.normalized,
                lookDir,
                elytraSteerSpeed * Time.deltaTime
            ).normalized * speed;
        }

        // gravity pulls down
        elytraVelocity += Vector3.down * (elytraGravity * Time.deltaTime);

        // lift from angle of attack
        float currentSpeed = elytraVelocity.magnitude;
        Vector3 flatVel = Vector3.ProjectOnPlane(elytraVelocity, Vector3.up);
        float aoa = Vector3.Angle(elytraVelocity, flatVel);

        // generate lift when not in a pure nosedive
        if (elytraVelocity.y > -currentSpeed * 0.95f)
        {
            float lift = elytraLiftCoefficient * currentSpeed * currentSpeed * Mathf.Sin(aoa * Mathf.Deg2Rad);
            elytraVelocity += Vector3.up * lift * Time.deltaTime;
        }

        // drag — speed squared
        float drag = elytraDrag * currentSpeed * currentSpeed;
        elytraVelocity = Vector3.MoveTowards(elytraVelocity, Vector3.zero, drag * Time.deltaTime);

        // clamp to max speed
        if (elytraVelocity.magnitude > elytraMaxSpeed)
            elytraVelocity = elytraVelocity.normalized * elytraMaxSpeed;

        // too slow — stall out
        if (elytraVelocity.magnitude < elytraMinSpeedToGlide)
        {
            ExitElytra();
            return;
        }

        controller.Move(elytraVelocity * Time.deltaTime);
    }

    void ExitElytra()
    {
        isElytraGliding = false;
        horizontalVel = new Vector3(elytraVelocity.x, 0f, elytraVelocity.z);
        verticalVel = elytraVelocity.y;
    }

    // helpers
    Vector3 GetGroundNormal()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, controller.height * 0.5f + 0.3f))
            return hit.normal;
        return Vector3.up;
    }

    void HandleArmBob()
    {
        if (armPrefab == null) return;
        float moveSpeed = new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;

        if (controller.isGrounded && moveSpeed > 0.1f)
        {
            float speedMultiplier = isSprinting ? sprintSpeedMultiplier : 1f;
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            float newY = armDefaultPos.y + Mathf.Sin(bobTimer) * bobAmount;
            armPrefab.localPosition = new Vector3(armDefaultPos.x, newY, armDefaultPos.z);
        }
        else
        {
            bobTimer = 0;
            armPrefab.localPosition = Vector3.Lerp(armPrefab.localPosition, armDefaultPos, Time.deltaTime * bobSmooth);
        }
    }

    public void EnableJetpack()
    {
        hasJetpack = true;
        UpdateJetpackIcon();
    }

    public void EnablePlatformBoots()
    {
        hasPlatformBoots = true;
        UpdatePlatformIcon();
    }

    public void EnableDoubleJump()
    {
        hasDoubleJump = true;
        UpdateDoubleJumpIcon();
    }

    public void AddCurrentPlatformNum()
    {
        if (currentPlatformNum + 1 > maxPlatformNum) return;
        currentPlatformNum++;
        if (platformSlider != null) 
        {
            platformSlider.value = currentPlatformNum;
        }
    }

    private void UpdateJetpackIcon()
    {
        if (hasJetpackImage == null) return;

        if (hasJetpack)
        {
            hasJetpackImage.color = unlockedColor;
        }
        else
        {
            hasJetpackImage.color = lockedColor;
        }
    }

    private void UpdatePlatformIcon()
    {
        if (hasPlatformImage == null) return;
        if (hasPlatformBoots)
        {
            hasPlatformImage.color = unlockedColor;
        }
        else
        {
            hasPlatformImage.color = lockedColor;
        }
    }

    private void UpdateDoubleJumpIcon()
    {
        if (hasDoubleJumpImage == null) return;
        
        if (hasDoubleJump)
            hasDoubleJumpImage.color = unlockedColor;
        else
            hasDoubleJumpImage.color = lockedColor;
    }
}