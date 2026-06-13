/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description:This script controls the player's behavior in the game.
* It handles player interactions with coins, keycards, and doors, as well as player health and score management.
* The player can collect coins, pick up keycards, and interact with doors to unlock them if they have a keycard.
* The player can also fire projectiles, recover health in healing areas, and take damage from hazards like lava and spikes.
* The script uses Unity's Input System for firing projectiles and handles raycasting to detect interactable objects in the game world.
* The player's score and health are displayed on the UI, and the player respawns at a designated spawn point upon death.
* The script also includes audio feedback for firing projectiles and interacting with objects.
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerBehaviour : MonoBehaviour
{
    /// <summary>
    /// Store variables for player behaviour, for example:
    /// gunPoint is the point from which projectiles are fired,
    /// fireStrength determines how fast the projectile will travel,
    /// fireAudioSource is the AudioSource component used to play firing sounds,
    /// and projectile is the prefab for the projectile that will be fired.
    /// </summary>
    // AudioSource fireAudioSource;
    // public GameObject projectile;
    // public Transform gunPoint;
    // public float fireStrength = 5f;

    int maxHealth = 100;
    int currentHealth = 100;

    [SerializeField] TMP_Text scoreUI;
    [SerializeField] Slider healthUI;
    // [SerializeField] Image keycardUI;
    int currentScore = 0;
    // bool canInteract = false;
    private bool hasKeycard = false;
    // private bool hasCrystal = false;
    // CoinBehaviour currentCoin = null;
    // DoorBehaviour currentDoor = null;
    // KeycardBehaviour currentKeycard = null;

    // [SerializeField] float interactionDistance = 2f;

    // [SerializeField] TMP_Text congratulatoryText;

    private Vector3 spawnPoint;

    /// <summary>
    /// Initializes the player by setting up the UI texts and hiding the keycard image.
    /// It also retrieves the main camera and the AudioSource component for firing projectiles.
    /// The score and health UI texts are set to their initial values.
    /// </summary>
    void Start()
    {
        // scoreUI.text = "SCORE: " + currentScore.ToString();
        healthUI.value = currentHealth;
        // keycardUI.enabled = false; // Hide the keycard image initially
        // fireAudioSource = GetComponent<AudioSource>();
        // congratulatoryText.gameObject.SetActive(false);
        spawnPoint = transform.position;
    }

    /// <summary>
    /// Handles player interaction with collectibles, keycards, and doors.
    /// This method is triggered by the interact input action.
    /// It checks if the player can interact with a coin, keycard, or door,
    /// and performs the appropriate action based on the current object being interacted with.
    /// If a coin is collected, it calls the Collect method on the CoinBehaviour script,
    /// if a keycard is collected, it calls the Collect method on the KeycardBehaviour script,
    /// and if a door is interacted with, it calls the Interact method on the DoorBehaviour script.
    /// </summary>
    // void OnInteract(InputValue value)
    // {
    //     if (canInteract)
    //         if (currentKeycard != null)
    //         {
    //             Debug.Log("Interacting with keycard");
    //             currentKeycard.Collect(this);
    //             hasKeycard = true;
    //             // keycardUI.enabled = true; // Show the keycard image
    //             currentKeycard = null; // Reset current keycard after interaction
    //         }
    //         else if (currentDoor != null)
    //         {
    //             Debug.Log("Interacting with door");
    //             currentDoor.Interact(this);
    //         }
            
    // }

    /// <summary>
    /// Handles the firing of projectiles when the fire input is triggered.
    /// This method makes the player face the same direction as the camera when firing.
    /// This is done by using the camera's forward vector, ignoring the vertical component to keep the player upright.
    /// It instantiates a projectile at the gun point, applies a force in the forward direction of the player,
    /// and plays a firing sound effect.
    /// The projectile is destroyed after 5 seconds to prevent cluttering the scene.
    /// The fire strength determines how fast the projectile will travel.
    /// </summary>
    // void OnFire(InputValue value)
    // {
    //     if (fireAudioSource != null)
    //     {
    //         fireAudioSource.Play();
    //     }

    //     var bullet = Instantiate(projectile, gunPoint.position, transform.rotation);
    //     var fireForce = transform.forward * fireStrength;
    //     var rigidBody = bullet.GetComponent<Rigidbody>();
    //     rigidBody.AddForce(fireForce);
    //     Destroy(bullet, 5f);
    // }

    public bool HasKeycard()
    {
        return hasKeycard;
    }

    /// <summary>
    /// Modifies the player's score by a specified amount.
    /// This method updates the current score and refreshes the score UI text to reflect the new score.
    /// </summary>
    public void ModifyScore(int amount)
    {
        currentScore += amount;
        scoreUI.text = "SCORE: " + currentScore.ToString();
    }

    /// <summary>
    /// Modifies the player's health by a specified amount.
    /// This method updates the current health and refreshes the health UI text to reflect the new health.
    /// If the health exceeds the maximum health, it is capped at the maximum value.
    /// If the health drops to zero or below, the player is considered dead, and their health is reset to maximum.
    /// The player is then respawned at a designated spawn point.
    /// </summary>
    public void ModifyHealth(int amount)
    {
        if (currentHealth <= maxHealth)
        {
            currentHealth += amount;
            healthUI.value = currentHealth;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
                healthUI.value = currentHealth;
            }
            if (currentHealth <= 0)
            {
                Debug.Log("You died.");
                currentHealth = maxHealth;
                healthUI.value = currentHealth;
                transform.position = spawnPoint;
            }
        }
    }

    // void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Coin"))
    //     {
    //         currentCoin = other.GetComponent<CoinBehaviour>();
    //         currentCoin.Collect(this);
    //         currentCoin = null; // Reset current coin after interaction
    //     }
    // }
}