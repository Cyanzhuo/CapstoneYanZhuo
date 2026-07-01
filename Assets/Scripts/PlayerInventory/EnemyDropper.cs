using UnityEngine;

public class EnemyDropper : MonoBehaviour
{
    [Header("Drop Prefabs")]
    [SerializeField] private GameObject coinPickupPrefab;
    [SerializeField] private GameObject healthPotionPickupPrefab;
    [SerializeField] private GameObject damagePotionPickupPrefab;

    [Header("Coin Drop")]
    [Range(0f, 1f)]
    [SerializeField] private float coinDropChance = 1f;
    [SerializeField] private int minCoins = 3;
    [SerializeField] private int maxCoins = 7;

    [Header("Potion Drop Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float healthPotionDropChance = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float damagePotionDropChance = 0.03f;

    [Header("Drop Position")]
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float scatterRadius = 0.6f;

    [Header("Auto Drop")]
    [SerializeField] private bool dropOnDestroy = true;
    [SerializeField] private bool dropOnDisable = true;

    private bool hasDropped = false;
    private bool hasStarted = false;
    private static bool applicationIsQuitting = false;

    private void Start()
    {
        hasStarted = true;
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (applicationIsQuitting) return;
        if (!dropOnDestroy) return;

        DropLoot();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (applicationIsQuitting) return;
        if (!hasStarted) return;
        if (!dropOnDisable) return;

        DropLoot();
    }

    [ContextMenu("Test Drop Loot")]
    public void DropLoot()
    {
        if (hasDropped)
        {
            return;
        }

        hasDropped = true;

        Debug.Log(gameObject.name + " is dropping loot.");

        TryDropCoins();
        TryDropPotion(healthPotionPickupPrefab, healthPotionDropChance, PickupType.HealthPotion);
        TryDropPotion(damagePotionPickupPrefab, damagePotionDropChance, PickupType.DamagePotion);
    }

    private void TryDropCoins()
    {
        if (coinPickupPrefab == null)
        {
            Debug.LogWarning(gameObject.name + " has no Coin Pickup Prefab assigned.");
            return;
        }

        float roll = Random.value;

        if (roll > coinDropChance)
        {
            Debug.Log(gameObject.name + " did not drop coins.");
            return;
        }

        int coinAmount = Random.Range(minCoins, maxCoins + 1);

        GameObject coinObject = Instantiate(
            coinPickupPrefab,
            GetDropPosition(),
            Quaternion.identity
        );

        DroppedPickup pickup = coinObject.GetComponent<DroppedPickup>();

        if (pickup != null)
        {
            pickup.Setup(PickupType.Coins, coinAmount);
        }

        Debug.Log(gameObject.name + " dropped " + coinAmount + " coins.");
    }

    private void TryDropPotion(GameObject potionPrefab, float dropChance, PickupType potionType)
    {
        if (potionPrefab == null)
        {
            Debug.Log(gameObject.name + " has no prefab assigned for " + potionType);
            return;
        }

        float roll = Random.value;

        if (roll > dropChance)
        {
            Debug.Log(gameObject.name + " did not drop " + potionType);
            return;
        }

        GameObject potionObject = Instantiate(
            potionPrefab,
            GetDropPosition(),
            Quaternion.identity
        );

        DroppedPickup pickup = potionObject.GetComponent<DroppedPickup>();

        if (pickup != null)
        {
            pickup.Setup(potionType, 1);
        }

        Debug.Log(gameObject.name + " dropped " + potionType);
    }

    private Vector3 GetDropPosition()
    {
        Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;

        Vector3 scatter = new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        return transform.position + dropOffset + scatter;
    }
}