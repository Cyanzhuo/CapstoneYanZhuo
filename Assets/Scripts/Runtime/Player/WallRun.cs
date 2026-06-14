using UnityEngine;

public class WallRun : MonoBehaviour
{
    [SerializeField] private bool isSlideRotationFrozen;

    public bool IsSlideRotationFrozen => isSlideRotationFrozen;

    public void SetSlideRotationFrozen(bool frozen)
    {
        isSlideRotationFrozen = frozen;
    }
}
