using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Coins")]
    [SerializeField] private int coins = 50;

    [Header("Potions")]
    [SerializeField] private int healthPotions = 0;
    [SerializeField] private int damagePotions = 0;

    public int Coins => coins;
    public int HealthPotions => healthPotions;
    public int DamagePotions => damagePotions;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        OnInventoryChanged?.Invoke();

        Debug.Log("Added coins: " + amount + " | Total coins: " + coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;

        if (coins < amount)
        {
            Debug.Log("Not enough coins.");
            return false;
        }

        coins -= amount;
        OnInventoryChanged?.Invoke();

        Debug.Log("Spent coins: " + amount + " | Total coins: " + coins);
        return true;
    }

    public void AddHealthPotion(int amount)
    {
        if (amount <= 0) return;

        healthPotions += amount;
        OnInventoryChanged?.Invoke();

        Debug.Log("Health potions: " + healthPotions);
    }

    public void AddDamagePotion(int amount)
    {
        if (amount <= 0) return;

        damagePotions += amount;
        OnInventoryChanged?.Invoke();

        Debug.Log("Damage potions: " + damagePotions);
    }

    public bool UseHealthPotion()
    {
        if (healthPotions <= 0)
        {
            Debug.Log("No health potions.");
            return false;
        }

        healthPotions--;
        OnInventoryChanged?.Invoke();

        Debug.Log("Used health potion. Remaining: " + healthPotions);
        return true;
    }

    public bool UseDamagePotion()
    {
        if (damagePotions <= 0)
        {
            Debug.Log("No damage potions.");
            return false;
        }

        damagePotions--;
        OnInventoryChanged?.Invoke();

        Debug.Log("Used damage potion. Remaining: " + damagePotions);
        return true;
    }
}