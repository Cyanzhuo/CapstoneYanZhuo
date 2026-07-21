using TMPro;
using UnityEngine;

public class PotionShop : MonoBehaviour
{
    [Header("Prices")]
    [SerializeField] private int healthPotionCost = 15;
    [SerializeField] private int damagePotionCost = 15;
    [SerializeField] private int superHealthPotionCost = 50;

    [Header("Shop Counter Text")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI healthPotionOwnedText;
    [SerializeField] private TextMeshProUGUI damagePotionOwnedText;
    [SerializeField] private TextMeshProUGUI superHealthPotionOwnedText;
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

    public void BuySuperHealthPotion()
    {
        if (PlayerInventory.Instance == null)
        {
            ShowMessage("Player inventory missing.");
            return;
        }

        if (PlayerInventory.Instance.SpendCoins(superHealthPotionCost))
        {
            PlayerInventory.Instance.AddSuperHealthPotion(1);
            ShowMessage("Bought Super Health Potion.");
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
            if (coinText != null)
            {
                coinText.text = "0";
            }

            if (healthPotionOwnedText != null)
            {
                healthPotionOwnedText.text = "0";
            }

            if (damagePotionOwnedText != null)
            {
                damagePotionOwnedText.text = "0";
            }

            if (superHealthPotionOwnedText != null)
            {
                superHealthPotionOwnedText.text = "0";
            }

            return;
        }

        if (coinText != null)
        {
            coinText.text = PlayerInventory.Instance.Coins.ToString();
        }

        if (healthPotionOwnedText != null)
        {
            healthPotionOwnedText.text = PlayerInventory.Instance.HealthPotions.ToString();
        }

        if (damagePotionOwnedText != null)
        {
            damagePotionOwnedText.text = PlayerInventory.Instance.DamagePotions.ToString();
        }

        if (superHealthPotionOwnedText != null)
        {
            superHealthPotionOwnedText.text = PlayerInventory.Instance.SuperHealthPotions.ToString();
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