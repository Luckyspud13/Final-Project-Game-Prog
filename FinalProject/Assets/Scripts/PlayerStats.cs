using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // keeps all the player's information so other functions don't have to
    private int health;
    private Vector3 direction;
    private float currentSpeed;
    private Vector3 acceleration;
    private float maxSpeed;
    public float jumpHeight;
    public float playerSpeed;
    public float gravity;
    public float airControl;
    
    void Start()
    {
        Debug.Log("PlayerState Starts");
    }

    public int GetHealth()
    {
        return health;
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public Vector3 GetAcceleration()
    {
        return acceleration;
    }

    public void SetHealth(int newHealth)
    {
        health = newHealth;
    }

    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
    }

    public void SetCurrentSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
    }

    public void SetAcceleration(Vector3 newAcceleration)
    {
        acceleration = newAcceleration;
    }


}
