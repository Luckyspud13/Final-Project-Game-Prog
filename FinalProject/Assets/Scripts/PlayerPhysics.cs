using UnityEngine;
using UnityEngine.TextCore.Text;
[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour
{
    // moves the player based on information from PlayerState and PlayerStats
    CharacterController controller;
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        
    }
}
