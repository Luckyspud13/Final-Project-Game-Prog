using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // keeps all the player's information so other functions don't have to
    private int health;
    private Vector3 direction;
    private float currentSpeed;
    private Vector3 acceleration;
    private float maxSpeed;

    // commented out, these conflict with PlayerController which has its own
    // jumpHeight, gravity, speed, and airControl values in the Inspector.
    // uncomment if you need them for something else later
    // public float jumpHeight;
    // public float playerSpeed;
    // public float gravity;
    // public float airControl;

    [Header("Jetpack Fuel")]
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelRegenRate = 20f;
    [SerializeField] private float fuelRegenDelay = 0.5f;

    private float currentFuel;
    private float lastFuelUseTime;

    // public read-only for UI
    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public float FuelPercent => currentFuel / maxFuel;

    void Start()
    {
        Debug.Log("PlayerState Starts");
        currentFuel = maxFuel;
    }

    // existing getters/setters

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

    // jetpack fuel

    // try to spend fuel, returns false if not enough
    public bool TryUseFuel(float amount)
    {
        if (currentFuel < amount) return false;

        currentFuel -= amount;
        lastFuelUseTime = Time.time;
        return true;
    }

    // called by PlayerController while grounded
    public void RegenFuel(float deltaTime)
    {
        if (Time.time - lastFuelUseTime < fuelRegenDelay) return;
        currentFuel = Mathf.Min(currentFuel + fuelRegenRate * deltaTime, maxFuel);
    }
}