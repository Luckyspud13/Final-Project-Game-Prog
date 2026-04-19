using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100;
    [SerializeField] private float pitchMin = -90f;
    [SerializeField] private float pitchMax = 90f;

    private Transform playerPos;

    private float pitch;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerPos = transform.parent.transform;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
    }

    // Update is called once per frame
    void Update()
    {
        CameraRotation();
    }
    
    void CameraRotation()
    {
        float moveX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float moveY = Input.GetAxis("Mouse Y")  * mouseSensitivity * Time.deltaTime;
        if (playerPos)
        {
            playerPos.Rotate(Vector3.up, moveX);
        }
        pitch -= moveY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}
