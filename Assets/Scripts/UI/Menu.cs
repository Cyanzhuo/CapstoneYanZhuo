using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class Menu : MonoBehaviour
{
    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;

    private bool isMenuOpen = false;

    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        ClearSelectedButton();
    }

    private void Update()
    {
        if (EscapePressed())
        {
            ToggleMenu();
        }
    }

    private void ToggleMenu()
    {
        if (isMenuOpen)
        {
            ResumeGame();
        }
        else
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        isMenuOpen = true;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            menuPanel.transform.SetAsLastSibling();
        }

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        ClearSelectedButton();
    }

    public void ResumeGame()
    {
        Debug.Log("Resume button clicked");

        isMenuOpen = false;

        ClearSelectedButton();

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Menu Panel is not assigned in Menu Manager.");
        }

        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
        if (EscapePressedNewInputSystem())
        {
            return true;
        }

        try
        {
            return Input.GetKeyDown(KeyCode.Escape);
        }
        catch
        {
            return false;
        }
    }

    private bool EscapePressedNewInputSystem()
    {
        try
        {
            Type keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");

            if (keyboardType == null)
            {
                return false;
            }

            PropertyInfo currentProperty = keyboardType.GetProperty(
                "current",
                BindingFlags.Public | BindingFlags.Static
            );

            object currentKeyboard = currentProperty.GetValue(null);

            if (currentKeyboard == null)
            {
                return false;
            }

            PropertyInfo escapeKeyProperty = keyboardType.GetProperty(
                "escapeKey",
                BindingFlags.Public | BindingFlags.Instance
            );

            object escapeKey = escapeKeyProperty.GetValue(currentKeyboard);

            if (escapeKey == null)
            {
                return false;
            }

            PropertyInfo wasPressedProperty = escapeKey.GetType().GetProperty(
                "wasPressedThisFrame",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (wasPressedProperty == null)
            {
                return false;
            }

            return (bool)wasPressedProperty.GetValue(escapeKey);
        }
        catch
        {
            return false;
        }
    }
}