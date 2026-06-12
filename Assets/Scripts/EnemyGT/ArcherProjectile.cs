using UnityEngine;

public class ArcherProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private int damage;
    private string playerTag;
    private Rigidbody rb;
    private Transform ownerRoot;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 direction, float speed, int damageAmount, string targetTag, Transform owner)
    {
        damage = damageAmount;
        playerTag = targetTag;
        ownerRoot = owner;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit)
        {
            return;
        }

        Transform otherRoot = other.transform.root;

        // Ignore the Archer that fired the projectile
        if (ownerRoot != null && otherRoot == ownerRoot)
        {
            return;
        }

        if (other.CompareTag(playerTag) || otherRoot.CompareTag(playerTag))
        {
            hasHit = true;

            other.SendMessageUpwards(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );

            Destroy(gameObject);
            return;
        }

        // Destroy when hitting walls/floor/objects
        if (!other.isTrigger)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}