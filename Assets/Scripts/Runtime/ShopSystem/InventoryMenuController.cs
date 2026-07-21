using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuView;
    [SerializeField] private GameObject inventoryUI;

    [Header("Inventory Images")]
    [SerializeField] private GameObject coinIconImage;
    [SerializeField] private GameObject damagePotionImage;
    [SerializeField] private GameObject healthPotionImage;
    [SerializeField] private GameObject superHealthPotionImage;

    [Header("Inventory Count Text")]
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI damagePotionCountText;
    [SerializeField] private TextMeshProUGUI healthPotionCountText;
    [SerializeField] private TextMeshProUGUI superHealthPotionCountText;

    private void Start()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        if (mainMenuView != null)
        {
            mainMenuView.SetActive(true);
        }

        RefreshInventory();
    }

    private void OnEnable()
    {
        RefreshInventory();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += RefreshInventory;
        }
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= RefreshInventory;
        }
    }

    public void OpenInventory()
    {
        if (mainMenuView != null)
        {
            mainMenuView.SetActive(false);
        }

        if (inventoryUI != null)
        {
            inventoryUI.SetActive(true);
            inventoryUI.transform.SetAsLastSibling();
        }

        if (coinIconImage != null)
        {
            coinIconImage.SetActive(true);
        }

        if (damagePotionImage != null)
        {
            damagePotionImage.SetActive(true);
        }

        if (healthPotionImage != null)
        {
            healthPotionImage.SetActive(true);
        }

        if (superHealthPotionImage != null)
        {
            superHealthPotionImage.SetActive(true);
        }

        ClearSelectedButton();
        RefreshInventory();
    }

    public void CloseInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.SetActive(false);
        }

        if (mainMenuView != null)
        {
            mainMenuView.SetActive(true);
            mainMenuView.transform.SetAsLastSibling();
        }

        ClearSelectedButton();
    }

    public void UseHealthPotion()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory missing.");
            return;
        }

        bool usedPotion = PlayerInventory.Instance.UseHealthPotion();

        if (usedPotion)
        {
            Debug.Log("Used Health Potion.");
        }
        else
        {
            Debug.Log("No Health Potion.");
        }

        RefreshInventory();
    }

    public void UseDamagePotion()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory missing.");
            return;
        }

        bool usedPotion = PlayerInventory.Instance.UseDamagePotion();

        if (usedPotion)
        {
            Debug.Log("Used Damage Potion.");
        }
        else
        {
            Debug.Log("No Damage Potion.");
        }

        RefreshInventory();
    }

    public void UseSuperHealthPotion()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("PlayerInventory missing.");
            return;
        }

        bool usedPotion = PlayerInventory.Instance.UseSuperHealthPotion();

        if (usedPotion)
        {
            Debug.Log("Used Super Health Potion.");
        }
        else
        {
            Debug.Log("No Super Health Potion.");
        }

        RefreshInventory();
    }

    public void RefreshInventory()
    {
        if (PlayerInventory.Instance == null)
        {
            SetText(coinCountText, "0");
            SetText(damagePotionCountText, "0");
            SetText(healthPotionCountText, "0");
            SetText(superHealthPotionCountText, "0");
            return;
        }

        SetText(coinCountText, PlayerInventory.Instance.Coins.ToString());
        SetText(damagePotionCountText, PlayerInventory.Instance.DamagePotions.ToString());
        SetText(healthPotionCountText, PlayerInventory.Instance.HealthPotions.ToString());
        SetText(superHealthPotionCountText, PlayerInventory.Instance.SuperHealthPotions.ToString());
    }

    private void SetText(TextMeshProUGUI textObject, string value)
    {
        if (textObject != null)
        {
            textObject.text = value;
        }
    }

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}