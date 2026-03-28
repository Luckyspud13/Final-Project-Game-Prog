using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Unlockable Abilities (toggle in Inspector)")]
    [Tooltip("Enables double-jump with fuel cost")]
    [SerializeField] private bool hasJetpack = false;
    [Tooltip("Enables gliding after air jumps are used")]
    [SerializeField] private bool hasElytra = false;
    [Tooltip("Enables wall sliding and wall jumping")]
    [SerializeField] private bool hasWallJumpBoots = false;

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
    [SerializeField] private int maxAirJumps = 1;

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

    [Header("Elytra – Physics Flight")]
    [SerializeField] private float elytraDrag = 0.01f;
    [SerializeField] private float elytraLiftCoefficient = 0.08f;
    [SerializeField] private float elytraMaxSpeed = 50f;
    [SerializeField] private float elytraMinSpeedToGlide = 3f;
    [SerializeField] private float elytraGravity = 9.7f;
    [SerializeField] private Transform cameraTransform;

    // internal state
    private CharacterController controller;
    private PlayerStats stats;

    private Vector3 horizontalVel;
    private float verticalVel;

    private int airJumpsUsed;
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

    // public read-only for UI or other scripts
    public bool IsGrounded => controller.isGrounded;
    public bool IsElytraGliding => isElytraGliding;
    public bool IsWallSliding => isWallSliding;
    public bool IsJetpackBoosting => isJetpackBoosting;
    public bool IsSliding => isSliding;
    public bool IsSprinting => isSprinting;
    public float CurrentSpeed => isElytraGliding
        ? elytraVelocity.magnitude
        : new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;

    // public setters so other scripts can unlock abilities at runtime
    public bool HasJetpack       { get => hasJetpack;       set => hasJetpack = value; }
    public bool HasElytra        { get => hasElytra;        set => hasElytra = value; }
    public bool HasWallJumpBoots { get => hasWallJumpBoots;  set => hasWallJumpBoots = value; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<PlayerStats>();

        normalHeight = controller.height;
        normalCenter = controller.center.y;

        if (stats == null)
            Debug.LogWarning("PlayerController: no PlayerStats found – jetpack fuel ignored.");

        // auto-find camera if not manually assigned
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
            else
                Debug.LogWarning("PlayerController: no camera found – elytra won't work.");
        }
    }

    void Update()
    {
        if (isElytraGliding)
            UpdateElytraFlight();
        else
            UpdateNormalMovement();
    }

    //  normal movement
    void UpdateNormalMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVel < 0f)
        {
            verticalVel = -groundStickDownForce;
            airJumpsUsed = 0;
            isWallSliding = false;
            isJetpackBoosting = false;
            jetpackBoostTimer = 0f;

            // buffered slide — pressed slide while in the air
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

        if (grounded && stats != null)
            stats.RegenFuel(Time.deltaTime);
    }

    //  sprinting — hold shift while moving forward
    void HandleSprinting(bool grounded)
    {
        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float moveV = Input.GetAxisRaw("Vertical");
        isSprinting = wantsSprint && grounded && !isSliding && moveV > 0.1f;
    }

    //  sliding — hold left ctrl, release to stop
    //  change the key bind here
    private bool SlideKeyHeld => Input.GetKey(KeyCode.LeftControl);
    private bool SlideKeyDown => Input.GetKeyDown(KeyCode.LeftControl);

    void HandleSliding(bool grounded)
    {
        // start slide on press
        if (SlideKeyDown)
        {
            if (grounded)
                TryStartSlide();
            else
                wantsSlideOnLand = true; // buffer for when we land
        }

        // release = stop
        if (isSliding && !SlideKeyHeld)
            TryStopSlide();

        // released in air = cancel buffer
        if (!SlideKeyHeld)
            wantsSlideOnLand = false;

        if (!isSliding) return;

        // auto-stop when too slow
        float speed = new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;
        if (speed < slideMinSpeed && grounded)
        {
            TryStopSlide();
            return;
        }

        // airborne while sliding — stay crouched but skip slide physics
        if (!grounded) return;

        // downhill slope boost
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2f))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > 5f)
            {
                Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                float downhillDot = Vector3.Dot(horizontalVel.normalized, slopeDir);

                if (downhillDot > 0.1f)
                {
                    Vector3 boost = slopeDir * slideSlopeBoost * (slopeAngle / 45f) * Time.deltaTime;
                    horizontalVel += new Vector3(boost.x, 0f, boost.z);
                }
            }
        }

        // cap slide speed
        Vector3 flat = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
        if (flat.magnitude > slideMaxSpeed)
        {
            flat = flat.normalized * slideMaxSpeed;
            horizontalVel = new Vector3(flat.x, 0f, flat.z);
        }
    }

    void TryStartSlide()
    {
        float speed = new Vector3(horizontalVel.x, 0f, horizontalVel.z).magnitude;
        if (speed < slideMinSpeed) return;

        isSliding = true;
        isSprinting = false;
        controller.height = slideHeight;
        controller.center = new Vector3(0f, slideHeight * 0.5f, 0f);

        // apex-style speed kick on entry
        Vector3 flatVel = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
        if (flatVel.magnitude > 0.1f)
            horizontalVel += flatVel.normalized * slideBoostSpeed;
    }

    void TryStopSlide()
    {
        if (IsBlockedAbove()) return; // can't stand up yet

        isSliding = false;
        controller.height = normalHeight;
        controller.center = new Vector3(0f, normalCenter, 0f);
    }

    // check if ceiling is too low to uncrouch
    bool IsBlockedAbove()
    {
        float checkDist = normalHeight - slideHeight;
        Vector3 origin = transform.position + Vector3.up * slideHeight;
        return Physics.Raycast(origin, Vector3.up, checkDist + 0.1f);
    }

    //  horizontal movement
    void HandleHorizontalMovement(bool grounded)
    {
        float moveH = Input.GetAxisRaw("Horizontal");
        float moveV = Input.GetAxisRaw("Vertical");

        Vector3 wishDir = transform.right * moveH + transform.forward * moveV;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        float accel    = grounded ? groundAcceleration : airAcceleration;
        float maxSpeed = grounded ? maxGroundSpeed     : maxAirSpeed;

        // sprint boosts
        if (isSprinting)
        {
            maxSpeed *= sprintSpeedMultiplier;
            accel *= sprintAccelMultiplier;
        }

        // friction depends on state
        float friction;
        if (!grounded)
            friction = airFriction;
        else if (isSliding)
            friction = slideFriction;
        else
            friction = groundFriction;

        // sliding = no input, pure momentum coast
        if (isSliding && grounded)
        {
            Vector3 flat = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
            flat = Vector3.MoveTowards(flat, Vector3.zero, friction * Time.deltaTime);
            horizontalVel = new Vector3(flat.x, 0f, flat.z);
            return;
        }

        if (wishDir.sqrMagnitude > 0f)
        {
            horizontalVel += wishDir * accel * Time.deltaTime;
            Vector3 flat = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
            if (flat.magnitude > maxSpeed)
            {
                flat = flat.normalized * maxSpeed;
                horizontalVel = new Vector3(flat.x, 0f, flat.z);
            }
        }
        else
        {
            Vector3 flat = new Vector3(horizontalVel.x, 0f, horizontalVel.z);
            flat = Vector3.MoveTowards(flat, Vector3.zero, friction * Time.deltaTime);
            horizontalVel = new Vector3(flat.x, 0f, flat.z);
        }
    }

    //  wall detection
    void DetectWall(bool grounded)
    {
        isWallSliding = false;
        if (!hasWallJumpBoots || grounded) return;

        Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };
        foreach (var dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, wallCheckDistance, wallLayer))
            {
                if (verticalVel < 0f)
                {
                    isWallSliding = true;
                    wallNormal = hit.normal;
                }
                return;
            }
        }
    }

    //  jumping — ground → wall → jetpack → elytra
    void HandleJumping(bool grounded)
    {
        if (!Input.GetButtonDown("Jump")) return;

        // cancel buffered slide
        wantsSlideOnLand = false;

        // slide-jump keeps momentum
        if (isSliding)
            TryStopSlide();

        // ground jump
        if (grounded)
        {
            verticalVel = Mathf.Sqrt(jumpHeight * 2f * gravity);
            return;
        }

        // wall jump
        if (hasWallJumpBoots && isWallSliding)
        {
            verticalVel = wallJumpUpForce;
            horizontalVel = wallNormal * wallJumpAwayForce;
            isWallSliding = false;
            airJumpsUsed = 0;
            return;
        }

        // jetpack double-jump
        if (hasJetpack && airJumpsUsed < maxAirJumps)
        {
            bool hasFuel = stats == null || stats.TryUseFuel(jetpackFuelCost);
            if (hasFuel)
            {
                verticalVel = jetpackBoost;
                airJumpsUsed++;
                isJetpackBoosting = true;
                jetpackBoostTimer = 0f;
                return;
            }
        }

        // elytra — only when falling and air jumps are used up
        if (hasElytra && verticalVel < 0f)
        {
            bool canActivate = !hasJetpack || airJumpsUsed >= maxAirJumps;
            if (canActivate)
                EnterElytra();
        }
    }

    //  jetpack hold boost — keep holding space after double-jump
    void HandleJetpackHoldBoost(bool grounded)
    {
        if (!hasJetpack || grounded || !isJetpackBoosting) return;

        bool holding = Input.GetButton("Jump");
        bool hasTime = jetpackBoostTimer < jetpackHoldMaxDuration;
        bool hasFuel = stats == null || stats.CurrentFuel > 0f;

        if (!holding || !hasTime || !hasFuel)
        {
            isJetpackBoosting = false;
            return;
        }

        float fuelCost = jetpackHoldFuelPerSec * Time.deltaTime;
        if (stats != null && !stats.TryUseFuel(fuelCost))
        {
            isJetpackBoosting = false;
            return;
        }

        verticalVel += jetpackHoldForce * Time.deltaTime;
        jetpackBoostTimer += Time.deltaTime;
    }

    void ApplyNormalGravity(bool grounded)
    {
        if (grounded && verticalVel < 0f) return;

        float g = gravity;
        if (hasWallJumpBoots && isWallSliding)
            g = wallSlideGravity;

        verticalVel -= g * Time.deltaTime;
    }

    //  elytra flight — camera direction = flight direction
    //  look down to dive and gain speed
    //  look up to trade speed for height
    //  level flight slowly loses speed to drag
    void EnterElytra()
    {
        isElytraGliding = true;
        isJetpackBoosting = false;

        // carry current velocity into 3D flight
        elytraVelocity = horizontalVel + Vector3.up * verticalVel;

        // ensure minimum forward speed
        Vector3 flatForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 flatVel = new Vector3(elytraVelocity.x, 0f, elytraVelocity.z);
        if (flatVel.magnitude < elytraMinSpeedToGlide)
            elytraVelocity = flatForward * elytraMinSpeedToGlide + Vector3.up * elytraVelocity.y;
    }

    void ExitElytra()
    {
        isElytraGliding = false;
        horizontalVel = new Vector3(elytraVelocity.x, 0f, elytraVelocity.z);
        verticalVel = elytraVelocity.y;
    }

    void UpdateElytraFlight()
    {
        // land
        if (controller.isGrounded)
        {
            ExitElytra();
            return;
        }

        // toggle off
        if (Input.GetButtonDown("Jump"))
        {
            ExitElytra();
            return;
        }

        if (cameraTransform == null) return;

        float dt = Time.deltaTime;
        float speed = elytraVelocity.magnitude;

        // where the camera is looking = where we want to fly
        Vector3 desiredDir = cameraTransform.forward.normalized;

        // 1) gravity — constant pull, your energy source
        elytraVelocity += Vector3.down * elytraGravity * dt;

        // 2) lift — redirects velocity toward where you're looking
        //    speed × angle of attack = how hard you can turn/pull up
        speed = elytraVelocity.magnitude;
        if (speed > 0.1f)
        {
            Vector3 velDir = elytraVelocity.normalized;
            float aoA = Vector3.Angle(velDir, desiredDir);
            float liftForce = elytraLiftCoefficient * speed * aoA;

            elytraVelocity = Vector3.RotateTowards(
                elytraVelocity,
                desiredDir * speed,
                liftForce * dt * Mathf.Deg2Rad,
                0f
            );
        }

        // 3) drag — speed² air resistance
        speed = elytraVelocity.magnitude;
        float dragForce = elytraDrag * speed * speed;
        if (speed > 0.1f)
        {
            float newSpeed = Mathf.Max(speed - dragForce * dt, 0f);
            elytraVelocity = elytraVelocity.normalized * newSpeed;
        }

        // 4) speed clamp
        speed = elytraVelocity.magnitude;
        if (speed > elytraMaxSpeed)
            elytraVelocity = elytraVelocity.normalized * elytraMaxSpeed;

        // 5) stall — too slow + falling = exit flight
        Vector3 flatVel = new Vector3(elytraVelocity.x, 0f, elytraVelocity.z);
        if (flatVel.magnitude < elytraMinSpeedToGlide && elytraVelocity.y < -2f)
        {
            ExitElytra();
            return;
        }

        // 6) move
        controller.Move(elytraVelocity * dt);
    }
}