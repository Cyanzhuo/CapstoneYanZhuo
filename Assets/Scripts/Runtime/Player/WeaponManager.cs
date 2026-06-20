using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Weapons[] weapons;
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private Transform weaponHolder; // Where the weapon model attaches
    private GameObject currentWeaponInstance;
    private Attack attack;
    
    void Start()
    {
        attack = GetComponent<Attack>();
        
        if (weapons.Length > 0)
        {
            EquipWeapon();
        }
    }

    public void OnSwitch(InputValue value)
    {
        if (currentWeaponIndex < weapons.Length - 1)
        {
            currentWeaponIndex++;
        }
        else
        {
            currentWeaponIndex = 0;
        }
        EquipWeapon();
    }

    private void EquipWeapon()
    {
        attack.currentWeapon = weapons[currentWeaponIndex];
        // Destroy old weapon instance
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }
        
        // Instantiate new weapon
        if (weapons[currentWeaponIndex].weaponPrefab != null && weaponHolder != null)
        {
            currentWeaponInstance = Instantiate(weapons[currentWeaponIndex].weaponPrefab, weaponHolder);
            
            // Get Hitbox from the INSTANCE (not the prefab)
            Hitbox hitbox = currentWeaponInstance.GetComponent<Hitbox>();
            if (hitbox != null)
            {
                attack.weaponHitbox = hitbox;
            }
            else
            {
            Debug.LogWarning($"Weapon {weapons[currentWeaponIndex].weaponName} has no Hitbox component!");
            }
        }
 
        Debug.Log("Equipped weapon: " + weapons[currentWeaponIndex].weaponName);
    }
}