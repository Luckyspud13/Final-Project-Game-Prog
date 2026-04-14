using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // keeps all the player's information so other functions don't have to
    private int health = 100;
    private Vector3 direction;
    private float currentSpeed;
    private Vector3 acceleration;
    private float maxSpeed;
    float dOTTimer = 0;
    float maxDOTTime = 0.5f;
    public AudioClip acidDamageSFX;
    public AudioClip rocketDamageSFX;

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

    void Update()
    {
        // tracks damage over time timer
        if(dOTTimer > 0 || GetHealth() > 0)
        {
            dOTTimer -= Time.deltaTime;
        }
    }

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

    void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("GasCloud"))
        {
            if(dOTTimer <= 0)
            {
                AcidCloud script = other.GetComponent<AcidCloud>();
                if(!script)
                {
                    Debug.Log("Incorrect tagging of Acid Cloud");
                }

                // play the acid audio clip
                AudioSource source = gameObject.GetComponent<AudioSource>();
                if(!source)
                {
                    Debug.Log("Player has no audio source");
                }
                else
                {
                    source.PlayOneShot(acidDamageSFX);
                }

                takeDamage(script.GetDamage());
                dOTTimer = maxDOTTime;
            }
            
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.CompareTag("Rocket"))
        {
            Debug.Log("tagged and bagged");
            RocketBehavior script = other.gameObject.GetComponent<RocketBehavior>();
            if(!script)
            {
                Debug.Log("Incorrect tagging of rocket object");
                return;
            }

            takeDamage(script.GetDamageValue());
        }
    }

    void takeDamage(int damage)
    {
        int health = GetHealth();
        health -= damage;
        if(health <= 0)
        {
            // player dies
            FindAnyObjectByType<LevelManager>().LevelLost();
        }

        SetHealth(health);
    }
}