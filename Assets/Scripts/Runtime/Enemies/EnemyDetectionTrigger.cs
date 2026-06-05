using UnityEngine;

public class EnemyDetectionTrigger : MonoBehaviour
{
    private Chaser chaser;
    
    void Start()
    {
        chaser = GetComponentInParent<Chaser>();
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaser.OnPlayerDetected(other.transform);
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            chaser.OnPlayerLost();
        }
    }
}