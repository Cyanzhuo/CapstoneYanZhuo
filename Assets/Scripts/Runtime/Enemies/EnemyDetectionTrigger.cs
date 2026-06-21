using UnityEngine;

public class EnemyDetectionTrigger : MonoBehaviour
{
    private IEnemyAI enemyAI;
    
    void Start()
    {
        enemyAI = GetComponentInParent<IEnemyAI>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            enemyAI.OnPlayerDetected(other.transform);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            enemyAI.OnPlayerLost();
        }
    }
}