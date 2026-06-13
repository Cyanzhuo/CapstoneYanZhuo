using UnityEngine;

public class EnemyHitbox : MonoBehaviour
{
    [SerializeField] private Collider weaponCollider;
    [SerializeField] private int DamageAmount = 10;
    private Coroutine activeHitStop = null;
    [SerializeField] private float shortHitStopDuration = 0.05f;
    [SerializeField] private float longHitStopDuration = 0.1f;

    void Start()
    {
        DeactivateHitbox();
    }
    
    public void ActivateHitbox()
    {
        weaponCollider.enabled = true;
    }
    public void DeactivateHitbox()
    {
        weaponCollider.enabled = false;
    }

    System.Collections.IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.01f; // Almost pause the game
        yield return new WaitForSecondsRealtime(duration); // Wait in real time
        Time.timeScale = 1f;
        activeHitStop = null;
    }

    void StopHitStop()
    {
        if (activeHitStop != null)
        {
            StopCoroutine(activeHitStop);
            Time.timeScale = 1f;
            activeHitStop = null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float hitStopDuration = shortHitStopDuration; // Default hitstop duration
            HealthBehaviour playerHealth = other.GetComponentInParent<HealthBehaviour>();
            PlayerBehaviour playerBehaviour = other.GetComponentInParent<PlayerBehaviour>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(playerBehaviour, DamageAmount);
                StopHitStop(); // Stop any existing hitstop before starting a new one
                activeHitStop = StartCoroutine(HitStop(hitStopDuration));
            }
            Debug.Log("Hit player for " + DamageAmount + " damage.");
            DeactivateHitbox();
        }
    }
}
