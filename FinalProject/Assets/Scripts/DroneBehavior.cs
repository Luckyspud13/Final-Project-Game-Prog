using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class DroneBehavior : MonoBehaviour
{
    public enum EnemyState {Patrol, Attack, Die};
    [Header("General Settings")]
    public EnemyState currentState = EnemyState.Patrol;
    public int baseDamageValue = 25;
    
    [Header("Navigate Settings")]
    public float rotationSpeed = 30f;
    public float detectionRange = 20f;
    public Slider healthSlider;

    [Header("Attack Settings")]
    public GameObject projectilePrefab;
    Transform firePoint;
    public float fireRate = 0.25f;
    public bool canAttack = true;

    [Header("Die Settings")]
    public int health = 100;
    public GameObject destroyPref;
    bool isEnemyDead = false;

    [Header("Navigate Settings")]
    public Transform[] waypoints;
    public int speed = 5;
    private int waypointIndex;
    private float dist;

    float fireCooldown = 0;
    Transform attackTarget;
    int maxHealth;
    void Start()
    {
        if(healthSlider)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }

        firePoint = transform;
        firePoint.position += Vector3.forward;
        waypointIndex = 0;
        transform.LookAt(waypoints[waypointIndex].position);
    }

    void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Navigate();
                break;
            case EnemyState.Attack:
                if(canAttack)
                {
                    Attack();
                }
                else
                {
                    currentState = EnemyState.Patrol;
                }
                break;
            case EnemyState.Die:
                Die();
                break;
            default:
                Debug.Log("Something's wrong.");
                break;
        }
    }

    void Navigate()
    {
        // check if the AI has reached its patrol destination
        dist = Vector3.Distance(transform.position, waypoints[waypointIndex].position);
        if(dist < 1f)
        {
            waypointIndex++;
            if(waypointIndex >= waypoints.Length)
            {
                waypointIndex = 0;
            }
            transform.LookAt(waypoints[waypointIndex].position);
        }

        // have the ai patrol
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // check for the player
        if(canAttack)
        {
            FindPlayer();
        }
    }

    void Attack()
    {
        // go back to navigate
        if(attackTarget == null || Vector3.Distance(transform.position, attackTarget.position) > detectionRange)
        {
            attackTarget = null;
            currentState = EnemyState.Patrol;
            return;
        }

        // attack
        Vector3 direction = attackTarget.position - transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);

        // check cooldown
        if(fireCooldown <= 0)
        {
            if(HasLineOfSight(attackTarget))
            {
                Shoot();
                fireCooldown = 1f / fireRate;
            }
        }
        
        fireCooldown -= Time.deltaTime;
    }

    void Die()
    {
        if(isEnemyDead) {
            return;
        }

        if(destroyPref)
        {
            Instantiate(destroyPref, transform.position, transform.rotation);
        }
        
        isEnemyDead = true;

        Destroy(gameObject);
    }

    void FindPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
        Transform nearbyPlayer = null;
        float shortestDistance = Mathf.Infinity; 

        foreach(Collider collider in colliders)
        {
            if(collider.CompareTag("Player"))
            {
                float distanceToEnemy = Vector3.Distance(transform.position, collider.transform.position);
                if(distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearbyPlayer = collider.transform;
                }
            }
        }

        if(nearbyPlayer)
        {
            attackTarget = nearbyPlayer;
            Debug.Log("Tower Detected: " + attackTarget.name);
            currentState = EnemyState.Attack;
            return;
        }
    }

    void Shoot()
    {
        if(!canAttack)
        {
            return;
        }

        var rocket = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        
        RocketBehavior rocketBehavior = rocket.GetComponent<RocketBehavior>();

        if(rocketBehavior)
        {
            rocketBehavior.SetTarget(attackTarget);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if(healthSlider)
        {
            healthSlider.value = health;
        }

        if(health <= 0)
        {
            currentState = EnemyState.Die;
            health = 0;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    /* void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.CompareTag("Bullet"))
        {
            BulletBehavior bulletBehavior = collision.gameObject.GetComponent<BulletBehavior>();
            if(bulletBehavior)
            {
                int bulletDamageValue = bulletBehavior.GetDamageValue();
                TakeDamage(bulletDamageValue);
                Debug.Log("Enemy took damage " + bulletDamageValue);
            }
            else 
            {
                Debug.LogWarning("No damage taken from the bullet. Please make sure it has a script");
            }

        }
    } */

    bool HasLineOfSight(Transform target) 
    {
        RaycastHit hit;

        Vector3 direction = (target.position - firePoint.position).normalized;

        if(Physics.Raycast(firePoint.position, direction, out hit, detectionRange))
        {
            if(hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    public int GetEnemyDamageValue()
    {
        return baseDamageValue;
    }
}