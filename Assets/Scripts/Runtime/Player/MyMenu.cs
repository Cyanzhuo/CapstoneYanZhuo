using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MyMenu : MonoBehaviour
{
    private PlayerInputActions controls;
    [Header("Menu UI")]
    [SerializeField] private GameObject menuPanel;
    private bool isMenuOpen = false;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopUI;

    [Header("Interaction")]
    [SerializeField] private string shopTag = "Shop";
    private bool shopInRange = false;
    private bool shopOpen = false;

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

        if (shopUI != null)
        {
            shopUI.SetActive(false);
        }
    }

    void Awake()
    {
        controls = new PlayerInputActions();
    }

    public void OnPause(InputValue value)
    {
        ToggleMenu();
    }

    public void OnInteract(InputValue value)
    {
        if (shopInRange)
        {
            if (shopOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
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
    
    public void OpenShop()
    {
        shopOpen = true;
        Time.timeScale = 0f;

        if (shopUI != null)
        {
            shopUI.SetActive(true);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Shop opened.");
    }

    public void CloseShop()
    {
        shopOpen = false;
        Time.timeScale = 1f;

        if (shopUI != null)
        {
            shopUI.SetActive(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Shop closed.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(shopTag) || other.transform.root.CompareTag(shopTag))
        {
            shopInRange = true;
            Debug.Log("Press E to open shop.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(shopTag) || other.transform.root.CompareTag(shopTag))
        {
            shopInRange = false;

            if (shopOpen)
            {
                CloseShop();
            }
        }
    }
}