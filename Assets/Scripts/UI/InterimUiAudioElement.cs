/*
 * InterimUiAudioElement: hover/click relay for individual ui elements
 */

using Game.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InterimUiAudioElement : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISubmitHandler, ISelectHandler
{
    [SerializeField] private bool playHoverOnPointerEnter = true;
    [SerializeField] private bool playHoverOnSelect = true;
    [SerializeField] private bool playClickOnPointerClick = true;
    [SerializeField] private bool playClickOnSubmit = true;
    [SerializeField] private float selectRepeatGuard = 0.08f;

    private float lastHoverTime = -99f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverOnPointerEnter) PlayHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClickOnPointerClick) PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (playClickOnSubmit) PlayClick();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!playHoverOnSelect) return;
        if (Time.unscaledTime - lastHoverTime < selectRepeatGuard) return;

        PlayHover();
    }

    public void PlayHover()
    {
        lastHoverTime = Time.unscaledTime;
        InterimAudioDirector.TryPlayUiHover();
    }

    public void PlayClick()
    {
        InterimAudioDirector.TryPlayUiClick();
    }
}
