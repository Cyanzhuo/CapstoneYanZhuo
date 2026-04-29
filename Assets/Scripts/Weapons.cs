using UnityEngine;

[CreateAssetMenu(fileName = "Weapons", menuName = "Scriptable Objects/Weapons")]
public class Weapons : ScriptableObject
{
    [Header("Damage Settings")]
    public string weaponName;
    public int normalDamage = 10;
    public int finisherDamage = 15;
    public int chargeDamage = 20;
    public int maxComboStage = 4;
}
