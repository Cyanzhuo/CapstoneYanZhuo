using UnityEngine;
using UnityEngine.InputSystem;

public class Potions : MonoBehaviour
{
    // Amount of health to recover
    [SerializeField] int healAmount = 25;
    private PlayerInputActions controls;
    HealthBehaviour playerHealth;
    PlayerBehaviour playerBehaviour;
    [SerializeField] InventoryMenuController playerInventory;
    
    void Start()
    {
        playerHealth = GetComponent<HealthBehaviour>();
        playerBehaviour = GetComponent<PlayerBehaviour>();
    }

    void Awake()
    {
        controls = new PlayerInputActions();
    }

    public void OnSpecial(InputValue value)
    {
        playerInventory.UseHealthPotion();
        playerHealth.RecoverHealth(playerBehaviour, healAmount);
    }
}
