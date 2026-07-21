using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuView;
    [SerializeField] private GameObject inventoryUI;

    [Header("Material Inventory")]
    [SerializeField] private MaterialInventory materialInventory;

    [Header("Inventory Images")]
    [SerializeField] private GameObject coinIconImage;
    [SerializeField] private GameObject damagePotionImage;
    [SerializeField] private GameObject healthPotionImage;
    [SerializeField] private GameObject superHealthPotionImage;

    [Header("Weapon Inventory Items")]
    [SerializeField] private GameObject axeItem;

    [Header("Potion And Coin Count Text")]
    [SerializeField] private TextMeshProUGUI coinCountText;
    [SerializeField] private TextMeshProUGUI damagePotionCountText;
    [SerializeField] private TextMeshProUGUI healthPotionCountText;
    [SerializeField] private TextMeshProUGUI superHealthPotionCountText;

    [Header("Material Count Text")]
    [SerializeField] private TextMeshProUGUI shatteredArmorCountText;
    [SerializeField] private TextMeshProUGUI arrowSticksCountText;
    [SerializeField] private TextMeshProUGUI tatteredClothCountText;

    private void Start()
    {
        FindMaterialInventory();
        SubscribeToInventoryEvents();

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
        FindMaterialInventory();
        SubscribeToInventoryEvents();
        RefreshInventory();
    }

    private void OnDisable()
    {
        UnsubscribeFromInventoryEvents();
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

        FindMaterialInventory();
        SubscribeToInventoryEvents();

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

        bool usedPotion =
            PlayerInventory.Instance.UseHealthPotion();

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

        bool usedPotion =
            PlayerInventory.Instance.UseDamagePotion();

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

        bool usedPotion =
            PlayerInventory.Instance.UseSuperHealthPotion();

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
        RefreshPotionAndCoinCounts();
        RefreshMaterialCounts();
        RefreshWeaponVisibility();
    }

    private void RefreshPotionAndCoinCounts()
    {
        if (PlayerInventory.Instance == null)
        {
            SetText(coinCountText, "0");
            SetText(damagePotionCountText, "0");
            SetText(healthPotionCountText, "0");
            SetText(superHealthPotionCountText, "0");
            return;
        }

        SetText(
            coinCountText,
            PlayerInventory.Instance.Coins.ToString()
        );

        SetText(
            damagePotionCountText,
            PlayerInventory.Instance.DamagePotions.ToString()
        );

        SetText(
            healthPotionCountText,
            PlayerInventory.Instance.HealthPotions.ToString()
        );

        SetText(
            superHealthPotionCountText,
            PlayerInventory.Instance.SuperHealthPotions.ToString()
        );
    }

    private void RefreshMaterialCounts()
    {
        FindMaterialInventory();

        if (materialInventory == null)
        {
            SetText(shatteredArmorCountText, "0");
            SetText(arrowSticksCountText, "0");
            SetText(tatteredClothCountText, "0");
            return;
        }

        SetText(
            shatteredArmorCountText,
            materialInventory.shatteredArmor.ToString()
        );

        SetText(
            arrowSticksCountText,
            materialInventory.arrowSticks.ToString()
        );

        SetText(
            tatteredClothCountText,
            materialInventory.tatteredCloth.ToString()
        );
    }

    private void RefreshWeaponVisibility()
    {
        bool axeIsUnlocked =
            materialInventory != null &&
            materialInventory.axeUnlocked;

        if (axeItem != null)
        {
            axeItem.SetActive(axeIsUnlocked);
        }
    }

    private void FindMaterialInventory()
    {
        if (materialInventory == null)
        {
            materialInventory =
                FindFirstObjectByType<MaterialInventory>();
        }
    }

    private void SubscribeToInventoryEvents()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -=
                RefreshInventory;

            PlayerInventory.Instance.OnInventoryChanged +=
                RefreshInventory;
        }

        if (materialInventory != null)
        {
            materialInventory.OnMaterialsChanged -=
                RefreshInventory;

            materialInventory.OnMaterialsChanged +=
                RefreshInventory;
        }
    }

    private void UnsubscribeFromInventoryEvents()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -=
                RefreshInventory;
        }

        if (materialInventory != null)
        {
            materialInventory.OnMaterialsChanged -=
                RefreshInventory;
        }
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

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}