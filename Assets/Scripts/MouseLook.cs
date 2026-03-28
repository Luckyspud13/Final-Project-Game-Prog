using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float pitchMin = -90f;
    [SerializeField] private float pitchMax = 90f;

    [Header("Camera Height")]
    [SerializeField] private float standingHeight = 1.6f;  // normal eye level
    [SerializeField] private float slidingHeight = 0.5f;   // eye level while sliding
    [SerializeField] private float heightLerpSpeed = 12f;   // how fast it transitions

    private float pitch;
    private Transform playerRoot;
    private PlayerController playerController;

    void Start()
    {
        playerRoot = transform.parent;

        if (playerRoot == null)
        {
            Debug.LogError("MouseLook: Camera must be a child of the Player object.");
            enabled = false;
            return;
        }

        playerController = playerRoot.GetComponent<PlayerController>();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float moveX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float moveY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // yaw
        playerRoot.Rotate(Vector3.up, moveX);

        // pitch
        pitch -= moveY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // smooth camera height for sliding
        float targetY = standingHeight;
        if (playerController != null && playerController.IsSliding)
            targetY = slidingHeight;

        Vector3 pos = transform.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, heightLerpSpeed * Time.deltaTime);
        transform.localPosition = pos;
    }
}