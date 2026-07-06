using UnityEngine;

public class EnemyBomb : MonoBehaviour
{
    [SerializeField] private float countdownTime = 3f; // Time in seconds before the bomb explodes
    [SerializeField] private float explosionLifetime = 0.5f; // Time in seconds the explosion effect lasts
    float spawnTime;
    EnemyHitbox hitbox;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hitbox = GetComponent<EnemyHitbox>();
        spawnTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - spawnTime >= countdownTime)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hitbox.ActivateHitbox();

        // Destroy the bomb object after explosion
        Destroy(gameObject, explosionLifetime);
    }
}
