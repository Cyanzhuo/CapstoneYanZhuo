using Game.Audio;
using UnityEngine;
using System.Collections.Generic;

public class PlayerProjectile : MonoBehaviour
{
    [SerializeField] public float lifetime = 1f;
    [SerializeField] private int damage = 15;
    [SerializeField] private int payback = -10;
    [SerializeField] private float hitStopDuration = 0.1f;
    
    // Track enemies hit during this attack
    private HashSet<EnemyBehaviour> hitEnemies = new HashSet<EnemyBehaviour>();
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent(out EnemyBehaviour enemy))
            {
                // Check if we've already hit this enemy during this attack
                if (hitEnemies.Contains(enemy))
                    return; // Already hit, skip
                
                // Add to hit list first (prevents re-entrancy issues)
                hitEnemies.Add(enemy);

                // Calculate knockback direction from player to enemy
                Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                knockbackDir.y = 0; // Keep it on a flat plane

                enemy.TakeDamage(damage, payback);
                enemy.Knockback(knockbackDir, 5f, true);
                
                InterimAudioDirector.TryPlayMove(InterimAudioCue.ChargedAttackHit, other.transform.position);

                // Apply hitstop
                HitStopManager.TriggerHitStop(hitStopDuration);
            }
        }
    }
}