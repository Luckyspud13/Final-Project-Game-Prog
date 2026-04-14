using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    public float speed = 20f;
    public float rotationSpeed = 5f;
    public float lifetime = 5f;
    public int damage = 10;
    public GameObject bulletHitPrefab;
    private Transform target;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        if (target == null)
            return;

        // direction
        Vector3 direction = (target.position - transform.position).normalized;

        // smooth rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        // move bullet forward
        rb.linearVelocity = transform.forward * speed;
    }

    public void SetTarget(Transform currentTarget)
    {
        target = currentTarget;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bullet hit " + collision.transform.name);

        if(bulletHitPrefab)
        {
            var pos = collision.contacts[0].point;
            Instantiate(bulletHitPrefab, pos, Quaternion.identity);
        }
    }

    public int GetDamageValue()
    {
        return damage;
    }
}
