using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuController : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursorWhenPlaying = true;

    private bool isPaused;

    private void Start()
    {
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        SetPlayingCursor();
        ClearSelectedButton();
    }

    private void Update()
    {
        if (EscapePressed())
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
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

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearSelectedButton();
    }

    public void ResumeGame()
    {
        isPaused = false;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        SetPlayingCursor();
        ClearSelectedButton();
    }

    private void SetPlayingCursor()
    {
        if (lockCursorWhenPlaying)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private bool EscapePressed()
    {
        if (NewInputSystemEscapePressed())
        {
            return true;
        }

        return OldInputSystemEscapePressed();
    }

    private bool OldInputSystemEscapePressed()
    {
        try
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        catch
        {
            return false;
        }
    }

    private bool NewInputSystemEscapePressed()
    {
        try
        {
            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");

            if (keyboardType == null)
            {
                return false;
            }

            PropertyInfo currentProperty = keyboardType.GetProperty("current", BindingFlags.Public | BindingFlags.Static);

            if (currentProperty == null)
            {
                return false;
            }

            object keyboard = currentProperty.GetValue(null);

            if (keyboard == null)
            {
                return false;
            }

            PropertyInfo escapeKeyProperty = keyboardType.GetProperty("escapeKey", BindingFlags.Public | BindingFlags.Instance);

            if (escapeKeyProperty == null)
            {
                return false;
            }

            object escapeKey = escapeKeyProperty.GetValue(keyboard);

            if (escapeKey == null)
            {
                return false;
            }

            PropertyInfo wasPressedThisFrameProperty = escapeKey.GetType().GetProperty("wasPressedThisFrame", BindingFlags.Public | BindingFlags.Instance);

            if (wasPressedThisFrameProperty == null)
            {
                return false;
            }

            return (bool)wasPressedThisFrameProperty.GetValue(escapeKey);
        }
        catch
        {
            return false;
        }
    }
}