using System;
using UnityEngine;
using TMPro;

public class KillCounter : MonoBehaviour
{
    public static event Action<int> OnKillRegistered;

    int killCount;

    [SerializeField] int requiredKillThreshold;
    [SerializeField] TMP_Text killCountText;

    public int KillCount
    {
        get { return killCount; }
    }

    public int RequiredKillThreshold
    {
        get { return requiredKillThreshold; }
    }

    void Start()
    {
        killCount = 0;
        UpdateKillText();
    }

    public void RegisterKill()
    {
        killCount++;

        OnKillRegistered?.Invoke(killCount);

        UpdateKillText();

        if (killCount >= requiredKillThreshold)
        {
            Destroy(gameObject);
        }
    }

    private void UpdateKillText()
    {
        if (killCountText != null)
        {
            int killsRemaining = requiredKillThreshold - killCount;

            if (killsRemaining < 0)
            {
                killsRemaining = 0;
            }

            killCountText.text = "Kills Required: " + killsRemaining.ToString();
        }
    }
}