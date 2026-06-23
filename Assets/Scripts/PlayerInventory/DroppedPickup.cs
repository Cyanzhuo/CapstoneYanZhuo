using UnityEngine;

public enum PickupType
{
    Coins,
    HealthPotion,
    DamagePotion
}

public class DroppedPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private PickupType pickupType = PickupType.Coins;
    [SerializeField] private int coinAmount = 1;
    [SerializeField] private string playerTag = "Player";

    [Header("Visual Movement")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private bool bob = true;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 30f;

    private Vector3 startPosition;
    private bool collected = false;

    private void Awake()
    {
        startPosition = transform.position;

        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void Start()
    {
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void Update()
    {
        if (rotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        if (bob)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    public void Setup(PickupType newPickupType, int newCoinAmount)
    {
        pickupType = newPickupType;
        coinAmount = newCoinAmount;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        bool isPlayer = other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag);

        if (!isPlayer)
        {
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory is missing. Pickup cannot be collected.");
            return;
        }

        collected = true;

        switch (pickupType)
        {
            case PickupType.Coins:
                PlayerInventory.Instance.AddCoins(coinAmount);
                Debug.Log("Picked up coins: " + coinAmount);
                break;

            case PickupType.HealthPotion:
                PlayerInventory.Instance.AddHealthPotion(1);
                Debug.Log("Picked up health potion.");
                break;

            case PickupType.DamagePotion:
                PlayerInventory.Instance.AddDamagePotion(1);
                Debug.Log("Picked up damage potion.");
                break;
        }

        Destroy(gameObject);
    }
}