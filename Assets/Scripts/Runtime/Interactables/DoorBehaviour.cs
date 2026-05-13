/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description: This script controls the behavior of doors in the game.
* It allows players to interact with doors, rotating them by 90 degrees and playing a sound effect when toggled.
* The door can be either locked or unlocked, and this script checks if the player has a keycard to unlock it.
* The door can be toggled between open and closed states, with visual feedback provided through a DoorLockVisual component.
*/

using UnityEngine;

public class DoorBehaviour : MonoBehaviour
{
    [SerializeField] bool isLocked = false;
    [SerializeField] DoorLockVisual lockVisual;
    private bool isOpen = false;
    AudioSource doorAudioSource; // Reference to the AudioSource component for playing sounds

    /// <summary>
    /// Method to initialize the door's state and audio source
    /// </summary>
    void Start()
    {
        // Initialize lock visual
        if (lockVisual != null)
        {
            lockVisual.SetLocked(isLocked);
        }

        // Get the AudioSource component attached to this GameObject
        doorAudioSource = GetComponent<AudioSource>();
    }
    
    /// <summary>
    /// Method to interact with the door
    /// This method will be called when the player interacts with the door
    /// It takes a PlayerBehaviour object as a parameter
    /// This allows the door to check if the player has a keycard to unlock it
    /// The method is public so it can be accessed from other scripts
    /// /// </summary>
    public void Interact(PlayerBehaviour player)
    {
        // Check if the door is locked
        if (isLocked)
        {
            // If the door is locked, check if the player has a keycard
            if (player.HasKeycard())
            {
                // If the player has a keycard, unlock the door
                isLocked = false;
                if (lockVisual != null)
                {
                    lockVisual.SetLocked(false);
                }

                Debug.Log("Door unlocked!");
                ToggleDoor(); // Call the Interact method to open the door
            }
            else
            {
                // If the player does not have a keycard, show a message
                Debug.Log("Door is locked! Find a keycard to unlock it.");
            }
        }
        else
        {
            // If the door is not locked, toggle its state (open/close)
            ToggleDoor();
        }
    }

    /// <summary>
    /// Method to toggle the door's state between open and closed
    /// This method is called when the player interacts with the door and has unlocked it
    /// It plays a sound effect and rotates the door by 90 degrees clockwise or counterclockwise
    /// depending on its current state
    /// </summary>
    public void ToggleDoor()
    {
        Vector3 doorRotation = transform.rotation.eulerAngles;
        if (isOpen)
        {
            // Play the door sound
            if (doorAudioSource != null)
            {
                doorAudioSource.Play();
            }
            // Close the door
            doorRotation.y -= 90f;
            isOpen = false;
        }
        else
        {
            // Play the door sound
            if (doorAudioSource != null)
            {
                doorAudioSource.Play();
            }
            // Open the door
            doorRotation.y += 90f;
            isOpen = true;
        }
        transform.eulerAngles = doorRotation;
    }
}
