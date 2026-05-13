using UnityEngine;

public class CrystalBehaviour : MonoBehaviour
{
    [SerializeField]
    AudioClip AudioClip; // Reference to the AudioClip component for playing sounds
    MeshRenderer meshRenderer; // Reference to the MeshRenderer component for highlighting
    [SerializeField]
    Material originalMaterial; // Store the original material for the crystal
    [SerializeField]
    Material highlightMaterial; // Material used for highlighting the crystal
    bool isCollected = false; // Flag to prevent double collection
    
    public void Start()
    {
        // Get the MeshRenderer component attached to this GameObject
        meshRenderer = GetComponent<MeshRenderer>();
        // Store the original color of the keycard for later use
        originalMaterial = meshRenderer.material;
    }

    public void Highlight()
    {
        /// <summary>
        /// Change the color of the keycard to highlight it
        /// This is done by setting the material color to the highlight color
        /// </summary>
        meshRenderer.material = highlightMaterial;
    }
    public void Unhighlight()
    {
        /// <summary>
        /// Reset the color of the keycard to its original color
        /// This is done by setting the material color back to the original color
        /// </summary>
        meshRenderer.material = originalMaterial;
    }

    public void Collect(PlayerBehaviour player)
    {
        // Logic for collecting the crystal
        if (isCollected) return; // Prevent double collection
        AudioSource.PlayClipAtPoint(AudioClip, transform.position); // Play the crystal collection sound
        isCollected = true; // Mark as collected
        Debug.Log("Crystal collected!");
        Destroy(gameObject);
    }
}
