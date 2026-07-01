using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MyMenu : MonoBehaviour
{
    private PlayerInputActions controls;
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

    void Awake()
    {
        controls = new PlayerInputActions();
    }

    public void OnPause(InputValue value)
    {
        ToggleMenu();
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
}