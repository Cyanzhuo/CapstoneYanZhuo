using System;
using System.Reflection;
using Game.Audio;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;

    [Header("Cursor")]
    [SerializeField] private bool showCursorWhenPaused = true;
    [SerializeField] private bool hideCursorDuringGameplay = true;

    private bool isPaused = false;

    private void Start()
    {
        ResumeGameInstant();
    }

    private void Update()
    {
        if (EscapePressed())
        {
            TogglePause();
        }
    }

    private bool EscapePressed()
    {
        // New Input System support without directly importing UnityEngine.InputSystem
        Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");

        if (keyboardType != null)
        {
            PropertyInfo currentProperty = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            object keyboard = currentProperty?.GetValue(null);

            if (keyboard != null)
            {
                PropertyInfo escapeKeyProperty = keyboardType.GetProperty("escapeKey", BindingFlags.Public | BindingFlags.Instance);
                object escapeKey = escapeKeyProperty?.GetValue(keyboard);

                if (escapeKey != null)
                {
                    PropertyInfo wasPressedProperty = escapeKey.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);

                    if (wasPressedProperty != null)
                    {
                        return (bool)wasPressedProperty.GetValue(escapeKey);
                    }
                }
            }
        }

        // Old Input fallback
        try
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void TogglePause()
    {
        InterimAudioDirector.TryPlayUiClick();

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        if (showCursorWhenPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (hideCursorDuringGameplay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ResumeGameInstant()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (hideCursorDuringGameplay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
