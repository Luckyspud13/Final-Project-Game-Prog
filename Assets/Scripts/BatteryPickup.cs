using UnityEngine;

public class BatteryPickup : MonoBehaviour
{
    [SerializeField] private float fuelAmount = 25f;
    [SerializeField] private AudioClip pickupSound;

    void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null) return;

        stats.AddFuel(fuelAmount);

        if (pickupSound != null)
        {
            // play at the pickups position so it still plays after destroy
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        Debug.Log("Battery picked up: +" + fuelAmount + " fuel");
        Destroy(gameObject);
    }
}