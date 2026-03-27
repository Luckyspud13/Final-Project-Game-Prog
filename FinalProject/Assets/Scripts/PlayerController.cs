using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = 9.7f;
    [SerializeField] private float airControl = 5f;
    [SerializeField] private float glidingGravity = 2f;

    [Header("Slide Settings")] 
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchSpeed = 5f;
    [SerializeField] private float slideSpeed = 20f;
    [SerializeField] private float slideDecay = 10f;

    [Header("Create Platform Settings")] 
    [SerializeField] private GameObject platform;
    [SerializeField] private float platformDuration = 4f;
    
    private Vector3 velocity;
    private Vector3 moveInput;
    private float currentSpeed;
    private bool isGliding = false;
<<<<<<< Updated upstream
    private bool boosted = false;
    public GameObject explosionEffect;
=======
    private bool isCrouching = false;
    private bool isSliding = false;
    private bool canCreatePlatform = false;
>>>>>>> Stashed changes
    
    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        HandleCrouch();
        PlayerMove();
        HandlePlatformCreation();
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (controller.isGrounded && moveInput.magnitude > 0.1f)
            {
                isSliding = true;
                currentSpeed = slideSpeed;
            }

            isCrouching = true;
            controller.height = crouchHeight;
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            isCrouching = false;
            isSliding = false;
            controller.height = standingHeight;
            currentSpeed = moveSpeed;
        }

        if (isSliding)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, crouchSpeed, slideDecay * Time.deltaTime);
            if (currentSpeed <= crouchSpeed + 0.5f) isSliding = false;
        } else if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = moveSpeed;
        }
    }

    void HandlePlatformCreation()
    {
        if (!controller.isGrounded)
        {
            canCreatePlatform = true;
        }
        else
        {
            canCreatePlatform = false;
        }

        if (Input.GetKeyDown(KeyCode.F) && canCreatePlatform)
        {
            CreatePlatform();
            canCreatePlatform = false;
        }
    }

    void CreatePlatform()
    {
        Vector3 spawnPos = transform.position + Vector3.down * 1.1f;
        GameObject newPlatform = Instantiate(platform, spawnPos, Quaternion.identity);
        Destroy(newPlatform, platformDuration);
    }

    void PlayerMove()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            isGliding = false;
        }
        
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");
        Vector3 targetInput = (transform.right * moveHorizontal + transform.forward * moveVertical).normalized;
        
        if (controller.isGrounded)
        {
            moveInput = targetInput;
            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }
        }
        else
        {
            moveInput = Vector3.Lerp(moveInput, targetInput, airControl * Time.deltaTime);
            if (velocity.y < 0 && Input.GetButton("Jump"))
            {
                isGliding = true;
            }
            else
            {
                isGliding = false;
            }
            if(Input.GetKeyDown(KeyCode.F))
            {
                Instantiate(explosionEffect, transform.position - Vector3.down, transform.rotation);
                velocity.y = Mathf.Sqrt(jumpHeight * 4f * gravity);
            }
        }

        float currentGravity = isGliding ? glidingGravity : gravity;
        velocity.y -= currentGravity * Time.deltaTime;
        Vector3 finalMove = (moveInput * currentSpeed) + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }
}