using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = 9.7f;
    [SerializeField] private float airControl = 5f;
    [SerializeField] private float glidingGravity = 2f;

    private Vector3 velocity;
    private Vector3 moveInput;
    private bool isGliding = false;
    private bool boosted = false;
    public GameObject explosionEffect;
    
    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        PlayerMove();
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
        Vector3 finalMove = (moveInput * moveSpeed) + new Vector3(0, velocity.y, 0);
        controller.Move(finalMove * Time.deltaTime);
    }
}