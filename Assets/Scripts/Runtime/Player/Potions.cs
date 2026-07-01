using UnityEngine;
using UnityEngine.InputSystem;

public class Potions : MonoBehaviour
{
    // Amount of health to recover
    [SerializeField] int healAmount = 25;
    private PlayerInputActions controls;
    HealthBehaviour playerHealth;
    PlayerBehaviour playerBehaviour;
    PlayerInventory playerInventory;
    [SerializeField] InventoryMenuController inventoryMenuController;
    [SerializeField] GameObject projectilePrefab;
    
    void Start()
    {
        playerHealth = GetComponent<HealthBehaviour>();
        playerBehaviour = GetComponent<PlayerBehaviour>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    void Awake()
    {
        controls = new PlayerInputActions();
    }

    public void OnSpecial(InputValue value)
    {
        if (playerInventory.UseHealthPotion())
        {
            playerHealth.RecoverHealth(playerBehaviour, healAmount);

            inventoryMenuController.RefreshInventory();
        }
        else
        {
            Debug.Log("No Health Potion.");
        }
    }

    public void OnShoot(InputValue value)
    {
        if (playerInventory.UseDamagePotion())
        {
            Instantiate(projectilePrefab, transform.position, transform.rotation);

            inventoryMenuController.RefreshInventory();
        }
        else
        {
            Debug.Log("No Damage Potion.");
        }
    }
}
