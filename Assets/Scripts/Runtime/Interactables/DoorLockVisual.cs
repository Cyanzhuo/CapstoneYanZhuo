/*
* Author: Cheang Wei Cheng
* Date: 14 June 2025
* Description: This script controls the material visuals of doors in the game.
* It changes the door's material based on whether it is locked or unlocked, and provides methods to highlight and unhighlight the door.
* The script uses Unity's MeshRenderer to change the material dynamically.
* This script is seperate from the DoorBehaviour script because DoorBehaviour is mapped to the door hinge,
* while this script is mapped to the door mesh itself.
*/

using UnityEngine;

public class DoorLockVisual : MonoBehaviour
{
    [SerializeField] Material lockedMaterial;
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material highlightMaterial;
    
    private MeshRenderer meshRenderer;
    private bool isLocked;

    /// <summary>
    /// Because DoorBehaviour is mapped to the door hinge,
    /// a separate script is needed to be mapped to the door mesh itself to handle the material visuals.
    /// It initializes the MeshRenderer component and sets the material based whether the door is locked,
    /// or whether the player is looking at the door to highlight it.
    /// </summary>
    void Start()
    {
        // Get the MeshRenderer component attached to this GameObject
        meshRenderer = GetComponent<MeshRenderer>();
        UpdateMaterial();
    }

    /// <summary>
    /// Method to set the locked state of the door and update the material accordingly.
    /// </summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateMaterial();
    }

    /// <summary>
    /// Change the color of the door to highlight it
    /// This is done by setting the material color to the highlight color
    /// </summary>
    public void Highlight()
    {
        meshRenderer.material = highlightMaterial;
    }

    /// <summary>
    /// Reset the color of the door to its original color
    /// It uses the UpdateMaterial method to revert to the appropriate locked/unlocked material.
    /// </summary>
    public void Unhighlight()
    {
        UpdateMaterial(); // Revert to appropriate locked/unlocked material
    }

    /// <summary>
    /// Updates the material of the door based on whether it is locked or not.
    /// This method is called whenever the locked state changes or when the door is highlighted/unhighlighted.
    /// </summary>
    void UpdateMaterial()
    {
        // Update the material based on the locked state
        meshRenderer.material = isLocked ? lockedMaterial : defaultMaterial;
    }
}