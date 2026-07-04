using UnityEngine;

public class MaterialPickup : MonoBehaviour
{
    [Header("Material Settings")]
    public MaterialInventory.MaterialType materialType;
    public int pickupAmount = 1;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.gameObject);
    }

    private void TryCollect(GameObject otherObject)
    {
        if (collected) return;

        MaterialInventory materialInventory = otherObject.GetComponentInParent<MaterialInventory>();

        if (materialInventory == null)
        {
            materialInventory = otherObject.GetComponentInChildren<MaterialInventory>();
        }

        if (materialInventory == null) return;

        collected = true;

        materialInventory.AddMaterial(materialType, pickupAmount);

        Destroy(gameObject);
    }
}