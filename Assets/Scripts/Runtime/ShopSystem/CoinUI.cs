using TMPro;
using UnityEngine;

public class CoinUI : MonoBehaviour
{
    [Header("Coin UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    private void Start()
    {
        UpdateCoinUI();

        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged += UpdateCoinUI;
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.OnInventoryChanged -= UpdateCoinUI;
        }
    }

    private void UpdateCoinUI()
    {
        if (coinText == null)
        {
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            coinText.text = "0";
            return;
        }

        coinText.text = PlayerInventory.Instance.Coins.ToString();
    }
}