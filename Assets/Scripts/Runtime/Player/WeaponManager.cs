using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] public Weapons[] weapons;
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

    public void UnlockWeapon(Weapons newWeapon)
    {
        if (!weapons.Contains(newWeapon))
        {
            System.Array.Resize(ref weapons, weapons.Length + 1);
            weapons[weapons.Length - 1] = newWeapon;
            Debug.Log("Unlocked weapon: " + newWeapon.weaponName);
        }
        else
        {
            Debug.Log("Weapon already unlocked: " + newWeapon.weaponName);
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