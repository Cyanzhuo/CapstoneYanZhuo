using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AxeCraftingUI : MonoBehaviour
{
    [Header("Material Inventory")]
    [SerializeField] private MaterialInventory materialInventory;

    [Header("Material Count Text")]
    [SerializeField] private TMP_Text shatteredArmorText;
    [SerializeField] private TMP_Text arrowSticksText;
    [SerializeField] private TMP_Text tatteredClothText;

    [Header("Axe UI")]
    [SerializeField] private GameObject lockedAxeImage;
    [SerializeField] private GameObject unlockedAxeImage;
    [SerializeField] private TMP_Text axeStatusText;

    [Header("Craft Button")]
    [SerializeField] private Button craftAxeButton;
    [SerializeField] private TMP_Text craftButtonText;

    void Start()
    {
        if (materialInventory == null)
        {
            materialInventory = FindFirstObjectByType<MaterialInventory>();
        }

        RefreshUI();
    }

    void OnEnable()
    {
        if (materialInventory == null)
        {
            materialInventory = FindFirstObjectByType<MaterialInventory>();
        }

        RefreshUI();
    }

    public void CraftAxe()
    {
        if (materialInventory == null)
        {
            Debug.LogWarning("MaterialInventory is missing.");
            return;
        }

        bool crafted = materialInventory.CraftAxe();

        if (crafted)
        {
            Debug.Log("Axe crafted from materials.");
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (materialInventory == null)
        {
            SetText(shatteredArmorText, "0/3");
            SetText(arrowSticksText, "0/3");
            SetText(tatteredClothText, "0/2");
            SetText(axeStatusText, "LOCKED");
            SetText(craftButtonText, "CRAFT AXE");

            if (craftAxeButton != null)
            {
                craftAxeButton.interactable = false;
            }

            if (lockedAxeImage != null) lockedAxeImage.SetActive(true);
            if (unlockedAxeImage != null) unlockedAxeImage.SetActive(false);

            return;
        }

        SetText(shatteredArmorText, materialInventory.shatteredArmor + "/" + materialInventory.RequiredShatteredArmor);
        SetText(arrowSticksText, materialInventory.arrowSticks + "/" + materialInventory.RequiredArrowSticks);
        SetText(tatteredClothText, materialInventory.tatteredCloth + "/" + materialInventory.RequiredTatteredCloth);

        if (materialInventory.axeUnlocked)
        {
            SetText(axeStatusText, "OWNED");
            SetText(craftButtonText, "OWNED");

            if (craftAxeButton != null)
            {
                craftAxeButton.interactable = false;
            }

            if (lockedAxeImage != null) lockedAxeImage.SetActive(false);
            if (unlockedAxeImage != null) unlockedAxeImage.SetActive(true);
        }
        else
        {
            SetText(axeStatusText, "LOCKED");
            SetText(craftButtonText, "CRAFT AXE");

            if (craftAxeButton != null)
            {
                craftAxeButton.interactable = materialInventory.CanCraftAxe();
            }

            if (lockedAxeImage != null) lockedAxeImage.SetActive(true);
            if (unlockedAxeImage != null) unlockedAxeImage.SetActive(false);
        }
    }

    private void SetText(TMP_Text textObject, string value)
    {
        if (textObject != null)
        {
            textObject.text = value;
        }
    }
}