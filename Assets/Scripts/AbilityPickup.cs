using UnityEngine;
public class AbilityPickup : MonoBehaviour
{
    [Header("Abilities to Grant")]
    [SerializeField] private bool grantsJetpack = false;
    [SerializeField] private bool grantsElytra = false;
    [SerializeField] private bool grantsWallJumpBoots = false;

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (grantsJetpack)       player.HasJetpack = true;
        if (grantsElytra)        player.HasElytra = true;
        if (grantsWallJumpBoots) player.HasWallJumpBoots = true;

        Debug.Log("Picked up abilities:"
            + (grantsJetpack ? " Jetpack" : "")
            + (grantsElytra ? " Elytra" : "")
            + (grantsWallJumpBoots ? " WallJumpBoots" : ""));

        Destroy(gameObject);
    }
}