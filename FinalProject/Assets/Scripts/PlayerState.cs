using System;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    // Takes inputs and tracks what state the player is in
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // get input for state changes and updates states
        GetInput();
        
    }

    void GetInput()
    {
        // takes any keyboard inputs which would change the player state
    }


}
