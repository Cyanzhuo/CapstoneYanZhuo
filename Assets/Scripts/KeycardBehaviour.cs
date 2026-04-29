/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description: This script controls the behavior of keycards in the game.
* When a player collects a keycard, it plays a sound effect, changes the material to highlight it,
* marks it as collected to prevent double collection, and destroys the keycard object.
* When collected, the keycard can be used to unlock locked doors.
* The script also manages the original and highlight materials for visual feedback.
*/

using UnityEngine;

public class KeycardBehaviour : MonoBehaviour
{
    [SerializeField]
    AudioClip keycardAudioClip; // Reference to the AudioClip component for playing sounds
    MeshRenderer meshRenderer; // Reference to the MeshRenderer component for highlighting
    [SerializeField]
    Material originalMaterial; // Store the original material for the keycard
    [SerializeField]
    Material highlightMaterial; // Material used for highlighting the keycard
    bool isCollected = false; // Flag to prevent double collection

    /// <summary>
    /// Method to collect the keycard
    /// This method will be called when the player interacts with the keycard
    /// It takes a PlayerBehaviour object as a parameter
    /// This allows the keycard to modify the player's inventory
    /// The keycard can be used to unlock locked doors
    /// The method is public so it can be accessed from other scripts
    /// </summary>

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
        // Logic for collecting the keycard
        if (isCollected) return; // Prevent double collection
        AudioSource.PlayClipAtPoint(keycardAudioClip, transform.position); // Play the keycard collection sound
        isCollected = true; // Mark as collected
        Debug.Log("keycard collected!");
        Destroy(gameObject);
    }
}
