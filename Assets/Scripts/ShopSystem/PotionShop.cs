using TMPro;
using UnityEngine;

public class PotionShop : MonoBehaviour
{
    [Header("Prices")]
    [SerializeField] private int healthPotionCost = 10;
    [SerializeField] private int damagePotionCost = 15;

    [Header("Shop Text")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI healthPotionText;
    [SerializeField] private TextMeshProUGUI damagePotionText;
    [SerializeField] private TextMeshProUGUI messageText;

    private void OnEnable()
    {
        UpdateShopUI();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += UpdateShopUI;
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= UpdateShopUI;
        }
    }

    public void BuyHealthPotion()
    {
        if (PlayerInventory.Instance == null)
        {
            ShowMessage("Player inventory missing.");
            return;
        }

        if (PlayerInventory.Instance.SpendCoins(healthPotionCost))
        {
            PlayerInventory.Instance.AddHealthPotion(1);
            ShowMessage("Bought Health Potion.");
        }
        else
        {
            ShowMessage("Not enough coins.");
        }

        UpdateShopUI();
    }

    public void BuyDamagePotion()
    {
        if (PlayerInventory.Instance == null)
        {
            ShowMessage("Player inventory missing.");
            return;
        }

        if (PlayerInventory.Instance.SpendCoins(damagePotionCost))
        {
            PlayerInventory.Instance.AddDamagePotion(1);
            ShowMessage("Bought Damage Potion.");
        }
        else
        {
            ShowMessage("Not enough coins.");
        }

        UpdateShopUI();
    }

    private void UpdateShopUI()
    {
        if (PlayerInventory.Instance == null)
        {
            return;
        }

        if (coinText != null)
        {
            coinText.text = "Coins: " + PlayerInventory.Instance.Coins;
        }

        if (healthPotionText != null)
        {
            healthPotionText.text = "Health Potion: " + PlayerInventory.Instance.HealthPotions + " | Cost: " + healthPotionCost;
        }

        if (damagePotionText != null)
        {
            damagePotionText.text = "Damage Potion: " + PlayerInventory.Instance.DamagePotions + " | Cost: " + damagePotionCost;
        }
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        Debug.Log(message);
    }
}