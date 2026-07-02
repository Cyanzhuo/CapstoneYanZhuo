using UnityEngine;
using TMPro;

public class KillCounter : MonoBehaviour
{
    int killCount;
    [SerializeField] int requiredKillThreshold;
    [SerializeField] TMP_Text killCountText;

    void Start()
    {
        killCount = 0;
        killCountText.text = "Kills Required: " + (requiredKillThreshold - killCount).ToString();
    }

    public void RegisterKill()
    {
        killCount++;
        killCountText.text = "Kills Required: " + (requiredKillThreshold - killCount).ToString();
        
        if (killCount >= requiredKillThreshold)
        {
            Destroy(gameObject);
        }
    }
}
