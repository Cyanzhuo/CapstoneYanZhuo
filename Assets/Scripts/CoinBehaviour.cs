/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description: This script handles the behaviour of collectable coins in the game.
* When a player collects a coin, it plays a sound, updates the player's score, and destroys the coin object.
* The coin can be highlighted when the player is near it, and it has a value that contributes to the player's score.
* The script also ensures that the coin can only be collected once to prevent double collection.
*/

using UnityEngine;

public class CoinBehaviour : MonoBehaviour
{
    [SerializeField]
    AudioClip coinAudioClip; // Reference to the AudioClip component for playing sounds
    MeshRenderer meshRenderer; // Reference to the MeshRenderer component for highlighting
    [SerializeField]
    Material originalMaterial; // Store the original material for the coin
    [SerializeField]
    Material highlightMaterial; // Material used for highlighting the coin
    // Coin value that will be added to the player's score
    [SerializeField]
    public int coinValue = 1;
    bool isCollected = false; // Flag to prevent double collection

    /// <summary>
    /// Method to collect the coin
    /// This method will be called when the player interacts with the coin
    /// It takes a PlayerBehaviour object as a parameter
    /// This allows the coin to modify the player's score
    /// The method is public so it can be accessed from other scripts
    /// </summary>

    public void Start()
    {
        // Get the MeshRenderer component attached to this GameObject
        meshRenderer = GetComponent<MeshRenderer>();
        // Store the original color of the coin for later use
        originalMaterial = meshRenderer.material;
    }

    public void Highlight()
    {
        /// <summary>
        /// Change the color of the coin to highlight it
        /// This is done by setting the material color to the highlight color
        /// </summary>
        meshRenderer.material = highlightMaterial;
    }
    public void Unhighlight()
    {
        /// <summary>
        /// Reset the color of the coin to its original color
        /// This is done by setting the material color back to the original color
        /// </summary>
        meshRenderer.material = originalMaterial;
    }

    public void Collect(PlayerBehaviour player)
    {
        // Logic for collecting the coin
        if (isCollected) return; // Prevent double collection
        AudioSource.PlayClipAtPoint(coinAudioClip, transform.position); // Play the coin collection sound
        isCollected = true; // Mark as collected
        Debug.Log("Coin collected!");

        /// <summary>
        /// Add the coin value to the player's score
        /// This is done by calling the ModifyScore method on the player object
        /// The coinValue is passed as an argument to the method
        /// This allows the player to gain points when they collect the coin
        /// </summary>
        player.ModifyScore(coinValue);
    
        Destroy(gameObject); // Destroy the coin object
    }
}
