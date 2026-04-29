/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description:
*/

using UnityEngine;

public class HealthBehaviour : MonoBehaviour
{
    // Amount of health to recover
    [SerializeField]
    int healAmount = 1;
    [SerializeField]
    int DamageAmount = 1;
    [SerializeField]
    float damageInterval = 1f;
    private float lastDamageTime;
    AudioSource audioSource; // Reference to the AudioSource component for playing sounds

    void Start()
    {
        // Get the AudioSource component attached to this GameObject
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Method to recover health
    /// This method will be called when the player interacts with the recovery object
    /// It takes a PlayerBehaviour object as a parameter
    /// This allows the recovery object to modify the player's health
    /// The method is public so it can be accessed from other scripts
    /// </summary>
    public void RecoverHealth(PlayerBehaviour player)
    {
        /// <summary>
        /// Calls the ModifyHealth method on the player object
        /// The healAmount is passed as an argument to the method
        /// This allows the player to gain health as long as they touch the recovery object
        /// </summary>
        player.ModifyHealth(healAmount);
    }
    // Method to apply damage
    public void ApplyDamage(PlayerBehaviour player)
    {
        // Only damage if enough time has passed
        if (Time.time - lastDamageTime >= damageInterval)
        {
            player.ModifyHealth(-DamageAmount);
            lastDamageTime = Time.time; // Update the last damage time
            if (audioSource != null)
            {
                audioSource.Play(); // Play the lava damage sound
            }
        }
    }
}
