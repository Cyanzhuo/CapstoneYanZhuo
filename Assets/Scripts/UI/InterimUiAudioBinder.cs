/*
 * InterimUiAudioBinder: adds interim ui audio relays to child selectables
 */

using UnityEngine;
using UnityEngine.UI;

public sealed class InterimUiAudioBinder : MonoBehaviour
{
    [SerializeField] private bool bindOnEnable = true;
    [SerializeField] private bool includeInactive = true;

    private void OnEnable()
    {
        if (bindOnEnable) BindAllChildren();
    }

    public void BindAllChildren()
    {
        Selectable[] selectables = GetComponentsInChildren<Selectable>(includeInactive);

        foreach (Selectable selectable in selectables)
        {
            if (selectable.GetComponent<InterimUiAudioElement>() != null) continue;

            selectable.gameObject.AddComponent<InterimUiAudioElement>();
        }
    }
}
