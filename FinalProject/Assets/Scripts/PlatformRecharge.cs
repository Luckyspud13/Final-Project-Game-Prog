using UnityEngine;

public class PlatformRecharge : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            pc.AddCurrentPlatformNum();
            Destroy(gameObject);
        }
    }
}
