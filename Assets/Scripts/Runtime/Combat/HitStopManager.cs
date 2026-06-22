using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    private static HitStopManager instance;
    private Coroutine activeHitStop = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void TriggerHitStop(float duration)
    {
        if (instance != null)
        {
            instance.StopHitStop();
            instance.activeHitStop = instance.StartCoroutine(instance.HitStopCoroutine(duration));
        }
    }

    private void StopHitStop()
    {
        if (activeHitStop != null)
        {
            StopCoroutine(activeHitStop);
            Time.timeScale = 1f;
            activeHitStop = null;
        }
    }

    private System.Collections.IEnumerator HitStopCoroutine(float duration)
    {
        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
        activeHitStop = null;
    }
}