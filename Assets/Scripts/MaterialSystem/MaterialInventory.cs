using System;
using UnityEngine;

public class MaterialInventory : MonoBehaviour
{
    public enum MaterialType
    {
        ShatteredArmor,
        ArrowSticks,
        TatteredCloth
    }

    public event Action OnMaterialsChanged;

    [Header("Current Materials")]
    public int shatteredArmor;
    public int arrowSticks;
    public int tatteredCloth;

    [Header("Axe Recipe")]
    [SerializeField] private int requiredShatteredArmor = 3;
    [SerializeField] private int requiredArrowSticks = 3;
    [SerializeField] private int requiredTatteredCloth = 2;

    [Header("Axe Unlock State")]
    public bool axeUnlocked;
    [SerializeField] private Weapons axe;

    private WeaponManager weaponManager;

    #region Public Getters
    public int RequiredShatteredArmor
    {
        get { return requiredShatteredArmor; }
    }

    public int RequiredArrowSticks
    {
        get { return requiredArrowSticks; }
    }

    public int RequiredTatteredCloth
    {
        get { return requiredTatteredCloth; }
    }
    #endregion

    private void Start()
    {
        weaponManager = GetComponent<WeaponManager>();
    }

    #region Add Materials
    public void AddMaterial(MaterialType materialType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        switch (materialType)
        {
            case MaterialType.ShatteredArmor:
                AddShatteredArmor(amount);
                break;

            case MaterialType.ArrowSticks:
                AddArrowSticks(amount);
                break;

            case MaterialType.TatteredCloth:
                AddTatteredCloth(amount);
                break;
        }
    }

    public void AddShatteredArmor(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        shatteredArmor += amount;

        Debug.Log("Picked up Shattered Armor x" + amount);

        NotifyMaterialsChanged();
    }

    public void AddArrowSticks(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        arrowSticks += amount;

        Debug.Log("Picked up Arrow Sticks x" + amount);

        NotifyMaterialsChanged();
    }

    public void AddTatteredCloth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        tatteredCloth += amount;

        Debug.Log("Picked up Tattered Cloth x" + amount);

        NotifyMaterialsChanged();
    }
    #endregion

    #region Axe Crafting
    public bool CanCraftAxe()
    {
        if (axeUnlocked)
        {
            return false;
        }

        return shatteredArmor >= requiredShatteredArmor &&
               arrowSticks >= requiredArrowSticks &&
               tatteredCloth >= requiredTatteredCloth;
    }

    public bool CraftAxe()
    {
        if (axeUnlocked)
        {
            Debug.Log("Axe is already unlocked.");
            return false;
        }

        if (!CanCraftAxe())
        {
            Debug.Log("Not enough materials to craft the axe.");
            return false;
        }

        shatteredArmor -= requiredShatteredArmor;
        arrowSticks -= requiredArrowSticks;
        tatteredCloth -= requiredTatteredCloth;

        UnlockAxe();

        Debug.Log("Axe crafted and unlocked!");
        return true;
    }

    public void UnlockAxe()
    {
        if (axeUnlocked)
        {
            return;
        }

        axeUnlocked = true;

        if (weaponManager == null)
        {
            weaponManager = GetComponent<WeaponManager>();
        }

        if (weaponManager != null && axe != null)
        {
            weaponManager.UnlockWeapon(axe);
        }
        else
        {
            Debug.LogWarning("WeaponManager or Axe reference is missing.");
        }

        NotifyMaterialsChanged();
    }
    #endregion

    #region Helper Methods
    public int GetMaterialAmount(MaterialType materialType)
    {
        switch (materialType)
        {
            case MaterialType.ShatteredArmor:
                return shatteredArmor;

            case MaterialType.ArrowSticks:
                return arrowSticks;

            case MaterialType.TatteredCloth:
                return tatteredCloth;

            default:
                return 0;
        }
    }

    public void ResetMaterials()
    {
        shatteredArmor = 0;
        arrowSticks = 0;
        tatteredCloth = 0;
        axeUnlocked = false;

        Debug.Log("Material inventory reset.");

        NotifyMaterialsChanged();
    }

    private void NotifyMaterialsChanged()
    {
        OnMaterialsChanged?.Invoke();
    }
    #endregion
}