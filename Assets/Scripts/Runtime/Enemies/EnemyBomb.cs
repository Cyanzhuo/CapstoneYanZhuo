using UnityEngine;

public class EnemyBomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem explosionEffect; // Particle effect for the explosion
    [SerializeField] private float countdownTime = 3f; // Time in seconds before the bomb explodes
    [SerializeField] private float explosionLifetime = 0.5f; // Time in seconds the explosion effect lasts
    float spawnTime;
    bool hasExploded = false;
    EnemyHitbox hitbox;
    MeshRenderer meshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hitbox = GetComponent<EnemyHitbox>();
        meshRenderer = GetComponent<MeshRenderer>();
        spawnTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - spawnTime >= countdownTime && !hasExploded)
        {
            Explode();
        }
    }

    private void Explode()
    {
        hitbox.ActivateHitbox();
        explosionEffect.Play();

        // Destroy the bomb object after explosion
        Destroy(gameObject, explosionLifetime);
        meshRenderer.enabled = false; // Hide the bomb's mesh after explosion
        hasExploded = true;
    }
}
