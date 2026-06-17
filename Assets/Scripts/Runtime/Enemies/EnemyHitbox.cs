using Game.Audio;
using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private Collider weaponCollider;
    [SerializeField] private bool colliderOffByDefault = true;
    [SerializeField] private int DamageAmount = 10;
    [SerializeField] private InterimAudioCue hitCue = InterimAudioCue.BasicAttackHit;
    private Coroutine activeHitStop = null;
    [SerializeField] private float shortHitStopDuration = 0.05f;
    [SerializeField] private float longHitStopDuration = 0.1f;

    void Start()
    {
        if (colliderOffByDefault)
        {
            DeactivateHitbox();
        }
    }
    
    public void ActivateHitbox()
    {
        weaponCollider.enabled = true;
    }
    public void DeactivateHitbox()
    {
        weaponCollider.enabled = false;
    }

    public void SetHitCue(InterimAudioCue cue)
    {
        hitCue = cue;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HealthBehaviour playerHealth = other.GetComponentInParent<HealthBehaviour>();
            PlayerBehaviour playerBehaviour = other.GetComponentInParent<PlayerBehaviour>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(playerBehaviour, DamageAmount);
                InterimAudioDirector.TryPlayMove(hitCue, transform.position);
                HitStopManager.TriggerHitStop(shortHitStopDuration);
            }
            Debug.Log("Hit player for " + DamageAmount + " damage.");
            DeactivateHitbox();
        }
    }
}
