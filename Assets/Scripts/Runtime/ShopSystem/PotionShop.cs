using TMPro;
using UnityEngine;

public class PotionShop : MonoBehaviour
{
    [Header("Prices")]
    [SerializeField] private int healthPotionCost = 15;
    [SerializeField] private int damagePotionCost = 15;
    [SerializeField] private int superHealthPotionCost = 75;

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
            PlayerInventory.Instance.OnInventoryChanged -= UpdateShopUI;
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
            ShowMessage("Bought Full Health Potion.");
        }
        else
        {
            ShowMessage("Not enough coins.");
        }

        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        if (PlayerInventory.Instance == null)
        {
            SetText(coinText, "0");
            SetText(healthPotionOwnedText, "0");
            SetText(damagePotionOwnedText, "0");
            SetText(superHealthPotionOwnedText, "0");
            return;
        }

        SetText(
            coinText,
            PlayerInventory.Instance.Coins.ToString()
        );

        SetText(
            healthPotionOwnedText,
            PlayerInventory.Instance.HealthPotions.ToString()
        );

        SetText(
            damagePotionOwnedText,
            PlayerInventory.Instance.DamagePotions.ToString()
        );

        SetText(
            superHealthPotionOwnedText,
            PlayerInventory.Instance.SuperHealthPotions.ToString()
        );
    }

    private void SetText(
        TextMeshProUGUI textObject,
        string value
    )
    {
        if (textObject != null)
        {
            textObject.text = value;
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