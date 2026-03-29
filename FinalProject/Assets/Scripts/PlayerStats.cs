using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // keeps all the player's information so other functions don't have to
    private int health;
    private Vector3 direction;
    private float currentSpeed;
    private Vector3 acceleration;
    private float maxSpeed;

    [Header("Jetpack Fuel")]
    [SerializeField] private float maxFuel = 100f;

    private float currentFuel;

    // public read-only for UI
    public float CurrentFuel => currentFuel;
    public float MaxFuel => maxFuel;
    public float FuelPercent => currentFuel / maxFuel;

    void Start()
    {
        Debug.Log("PlayerStats Starts");
        currentFuel = maxFuel;
    }

    // existing getters/setters

    public int GetHealth() { return health; }
    public Vector3 GetDirection() { return direction; }
    public float GetCurrentSpeed() { return currentSpeed; }
    public Vector3 GetAcceleration() { return acceleration; }

    public void SetHealth(int newHealth) { health = newHealth; }
    public void SetDirection(Vector3 newDirection) { direction = newDirection; }
    public void SetCurrentSpeed(float newSpeed) { currentSpeed = newSpeed; }
    public void SetAcceleration(Vector3 newAcceleration) { acceleration = newAcceleration; }

    // jetpack fuel

    // try to spend fuel, returns false if not enough
    public bool TryUseFuel(float amount)
    {
        if (currentFuel < amount) return false;

        currentFuel -= amount;
        return true;
    }

    // called by battery pickups to restore fuel
    public void AddFuel(float amount)
    {
        currentFuel = Mathf.Min(currentFuel + amount, maxFuel);
    }
}