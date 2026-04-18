using UnityEngine;

public class PickupBehavior : MonoBehaviour
{
    public enum ItemType { Jetpack, Platform, DoubleJump}
    public ItemType type;
    
    [Header("Visual Settings")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float floatingSpeed = 2f;
    [SerializeField] private float floatingAmount = 0.2f;

    [Header("Audio Settings")] 
    [SerializeField] private AudioClip pickupSFX;

    private Vector3 startPos;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float newY = startPos.y + Mathf.Sin(Time.time * floatingSpeed) * floatingAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player)
            {
                switch(type)
                {
                    case ItemType.Jetpack:
                        player.EnableJetpack();
                        break;
                    case ItemType.Platform:
                        player.EnablePlatformBoots();
                        break;
                    case ItemType.DoubleJump:
                        player.EnableDoubleJump();
                        break;
                }
                if (player.armsAnimator)
                {
                    player.armsAnimator.SetTrigger("OnPickup");
                }
                PlayPickupSound(other);
                Destroy(gameObject);
            }
        }
    }
    private void PlayPickupSound(Collider player)
    {
        AudioSource audio = player.GetComponent<AudioSource>();
        if (audio && pickupSFX)
        {
            audio.PlayOneShot(pickupSFX);
        }
    }

    
    
}
