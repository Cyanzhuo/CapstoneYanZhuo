using System;
using System.Reflection;
using UnityEngine;

public class VendingMachineInteract : MonoBehaviour
{
    [Header("Shop UI")]
    [SerializeField] private GameObject shopUI;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";

    private bool playerInRange = false;
    private bool shopOpen = false;

    private void Start()
    {
        if (shopUI != null)
        {
            shopUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (EPressed())
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

    public void OpenShop()
    {
        shopOpen = true;

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
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = true;
            Debug.Log("Press E to open shop.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            playerInRange = false;

            if (shopOpen)
            {
                CloseShop();
            }
        }
    }

    private bool EPressed()
    {
        if (EPressedNewInputSystem())
        {
            return true;
        }

        try
        {
            return Input.GetKeyDown(KeyCode.E);
        }
        catch
        {
            return false;
        }
    }

    private bool EPressedNewInputSystem()
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

            PropertyInfo eKeyProperty = keyboardType.GetProperty(
                "eKey",
                BindingFlags.Public | BindingFlags.Instance
            );

            object eKey = eKeyProperty.GetValue(currentKeyboard);

            if (eKey == null)
            {
                return false;
            }

            PropertyInfo wasPressedProperty = eKey.GetType().GetProperty(
                "wasPressedThisFrame",
                BindingFlags.Public | BindingFlags.Instance
            );

            if (wasPressedProperty == null)
            {
                return false;
            }

            return (bool)wasPressedProperty.GetValue(eKey);
        }
        catch
        {
            return false;
        }
    }
}