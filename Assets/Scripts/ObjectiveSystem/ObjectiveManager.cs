#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using UnityEngine;
using TMPro;

public class ObjectiveManager : MonoBehaviour
{
    private enum ObjectiveStep
    {
        DefeatThreeEnemies,
        DefeatSixEnemies,
        CollectSomeArmor,
        CollectSomeSticks,
        CollectSomeCloth,
        CraftAxe,
        Complete
    }

    [Header("References")]
    [SerializeField] private KillCounter killCounter;
    [SerializeField] private MaterialInventory materialInventory;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Current Objective HUD")]
    [SerializeField] private GameObject objectiveHudRoot;
    [SerializeField] private TMP_Text objectiveTitleText;
    [SerializeField] private TMP_Text objectiveProgressText;
    [SerializeField] private TMP_Text objectiveRewardText;

    [Header("Quest Screen UI")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text questListText;
    [SerializeField] private TMP_Text eyeStateText;

    [Header("Eye Button UI")]
    [SerializeField] private GameObject viewButtonObject;
    [SerializeField] private GameObject unviewButtonObject;

    [Header("Kill Objectives")]
    [SerializeField] private int firstKillTarget = 3;
    [SerializeField] private int secondKillTarget = 6;
    [SerializeField] private int firstKillReward = 3;
    [SerializeField] private int secondKillReward = 5;

    [Header("Material Objectives")]
    [SerializeField] private int armorTarget = 2;
    [SerializeField] private int sticksTarget = 2;
    [SerializeField] private int clothTarget = 1;

    [SerializeField] private int armorReward = 2;
    [SerializeField] private int sticksReward = 2;
    [SerializeField] private int clothReward = 2;

    [Header("Axe Objective")]
    [SerializeField] private int craftAxeReward = 8;

    [Header("Objective Complete Delay")]
    [SerializeField] private float objectiveCompleteDelay = 2f;

    [Header("Settings")]
    [SerializeField] private bool objectiveHudVisible = true;

    private ObjectiveStep currentObjectiveStep = ObjectiveStep.DefeatThreeEnemies;

    private bool showingCompleteMessage;
    private float completeMessageTimer;

    private bool questPanelOpen;
    private float previousTimeScale = 1f;

    private int currentKillCount;

    private void OnEnable()
    {
        KillCounter.OnKillRegistered += UpdateKillCount;
    }

    private void OnDisable()
    {
        KillCounter.OnKillRegistered -= UpdateKillCount;
    }

    private void Start()
    {
        FindMissingReferences();

        if (killCounter != null)
        {
            currentKillCount = killCounter.KillCount;
        }

        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        ApplyObjectiveHudVisibility();
        RefreshObjectiveUI();
        RefreshQuestPanelUI();
        RefreshEyeStateUI();
    }

    private void Update()
    {
        FindMissingReferences();

        if (QuestKeyPressedThisFrame())
        {
            ToggleQuestPanel();
            return;
        }

        if (ViewKeyPressedThisFrame())
        {
            ToggleObjectiveHud();
            return;
        }

        if (showingCompleteMessage)
        {
            completeMessageTimer -= Time.unscaledDeltaTime;

            ShowObjectiveCompletedMessage();

            if (completeMessageTimer <= 0)
            {
                showingCompleteMessage = false;
                MoveToNextObjective();
                RefreshObjectiveUI();
                RefreshQuestPanelUI();
            }

            return;
        }

        CheckCurrentObjective();

        if (showingCompleteMessage)
        {
            return;
        }

        RefreshObjectiveUI();

        if (questPanelOpen)
        {
            RefreshQuestPanelUI();
        }
    }

    private bool QuestKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Q))
        {
            return true;
        }
#endif

        return false;
    }

    private bool ViewKeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.V))
        {
            return true;
        }
#endif

        return false;
    }

    private void UpdateKillCount(int newKillCount)
    {
        currentKillCount = newKillCount;
    }

    private void FindMissingReferences()
    {
        if (killCounter == null)
        {
            killCounter = FindFirstObjectByType<KillCounter>();
        }

        if (killCounter != null)
        {
            if (killCounter.KillCount > currentKillCount)
            {
                currentKillCount = killCounter.KillCount;
            }
        }

        if (materialInventory == null)
        {
            materialInventory = FindFirstObjectByType<MaterialInventory>();
        }

        if (playerInventory == null)
        {
            playerInventory = PlayerInventory.Instance;
        }
    }

    private void CheckCurrentObjective()
    {
        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                CheckDefeatThreeEnemies();
                break;

            case ObjectiveStep.DefeatSixEnemies:
                CheckDefeatSixEnemies();
                break;

            case ObjectiveStep.CollectSomeArmor:
                CheckCollectSomeArmor();
                break;

            case ObjectiveStep.CollectSomeSticks:
                CheckCollectSomeSticks();
                break;

            case ObjectiveStep.CollectSomeCloth:
                CheckCollectSomeCloth();
                break;

            case ObjectiveStep.CraftAxe:
                CheckCraftAxe();
                break;

            case ObjectiveStep.Complete:
                break;
        }
    }

    private void CheckDefeatThreeEnemies()
    {
        if (currentKillCount >= firstKillTarget)
        {
            CompleteCurrentObjective(firstKillReward);
            Debug.Log("Objective Complete: Defeat 3 enemies.");
        }
    }

    private void CheckDefeatSixEnemies()
    {
        if (currentKillCount >= secondKillTarget)
        {
            CompleteCurrentObjective(secondKillReward);
            Debug.Log("Objective Complete: Defeat 6 enemies.");
        }
    }

    private void CheckCollectSomeArmor()
    {
        if (materialInventory == null) return;

        if (materialInventory.shatteredArmor >= armorTarget)
        {
            CompleteCurrentObjective(armorReward);
            Debug.Log("Objective Complete: Collect Armor.");
        }
    }

    private void CheckCollectSomeSticks()
    {
        if (materialInventory == null) return;

        if (materialInventory.arrowSticks >= sticksTarget)
        {
            CompleteCurrentObjective(sticksReward);
            Debug.Log("Objective Complete: Collect Sticks.");
        }
    }

    private void CheckCollectSomeCloth()
    {
        if (materialInventory == null) return;

        if (materialInventory.tatteredCloth >= clothTarget)
        {
            CompleteCurrentObjective(clothReward);
            Debug.Log("Objective Complete: Collect Cloth.");
        }
    }

    private void CheckCraftAxe()
    {
        if (materialInventory == null) return;

        if (materialInventory.axeUnlocked)
        {
            CompleteCurrentObjective(craftAxeReward);
            Debug.Log("Objective Complete: Craft the Axe.");
        }
    }

    private void CompleteCurrentObjective(int coinReward)
    {
        GiveCoinReward(coinReward);
        StartObjectiveCompletedMessage();
    }

    private void MoveToNextObjective()
    {
        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                currentObjectiveStep = ObjectiveStep.DefeatSixEnemies;
                break;

            case ObjectiveStep.DefeatSixEnemies:
                currentObjectiveStep = ObjectiveStep.CollectSomeArmor;
                break;

            case ObjectiveStep.CollectSomeArmor:
                currentObjectiveStep = ObjectiveStep.CollectSomeSticks;
                break;

            case ObjectiveStep.CollectSomeSticks:
                currentObjectiveStep = ObjectiveStep.CollectSomeCloth;
                break;

            case ObjectiveStep.CollectSomeCloth:
                currentObjectiveStep = ObjectiveStep.CraftAxe;
                break;

            case ObjectiveStep.CraftAxe:
                currentObjectiveStep = ObjectiveStep.Complete;
                break;

            case ObjectiveStep.Complete:
                break;
        }
    }

    private void GiveCoinReward(int amount)
    {
        if (amount <= 0) return;

        if (playerInventory == null)
        {
            playerInventory = PlayerInventory.Instance;
        }

        if (playerInventory != null)
        {
            playerInventory.AddCoins(amount);
        }
    }

    public void ToggleQuestPanel()
    {
        if (questPanelOpen)
        {
            CloseQuestPanel();
        }
        else
        {
            OpenQuestPanel();
        }
    }

    public void OpenQuestPanel()
    {
        if (questPanelOpen) return;

        questPanelOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }

        RefreshQuestPanelUI();
        RefreshEyeStateUI();
    }

    public void CloseQuestPanel()
    {
        if (!questPanelOpen) return;

        questPanelOpen = false;

        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        Time.timeScale = previousTimeScale;
    }

    public void ToggleObjectiveHud()
    {
        objectiveHudVisible = !objectiveHudVisible;

        ApplyObjectiveHudVisibility();
        RefreshObjectiveUI();
        RefreshEyeStateUI();
    }

    public void ShowObjectiveHud()
    {
        objectiveHudVisible = true;

        ApplyObjectiveHudVisibility();
        RefreshObjectiveUI();
        RefreshEyeStateUI();
    }

    public void HideObjectiveHud()
    {
        objectiveHudVisible = false;

        ApplyObjectiveHudVisibility();
        RefreshEyeStateUI();
    }

    private void StartObjectiveCompletedMessage()
    {
        showingCompleteMessage = true;
        completeMessageTimer = objectiveCompleteDelay;

        ShowObjectiveCompletedMessage();
        RefreshQuestPanelUI();
    }

    private void ShowObjectiveCompletedMessage()
    {
        if (!objectiveHudVisible)
        {
            ApplyObjectiveHudVisibility();
            return;
        }

        SetText(objectiveTitleText, "Objective Completed");
        SetText(objectiveProgressText, "");
        SetText(objectiveRewardText, "");
    }

    private void RefreshObjectiveUI()
    {
        ApplyObjectiveHudVisibility();

        if (!objectiveHudVisible)
        {
            return;
        }

        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                ShowKillObjective("Defeat enemies", firstKillTarget, firstKillReward);
                break;

            case ObjectiveStep.DefeatSixEnemies:
                ShowKillObjective("Defeat 6 enemies", secondKillTarget, secondKillReward);
                break;

            case ObjectiveStep.CollectSomeArmor:
                ShowMaterialObjective("Collect Armor", GetArmorAmount(), armorTarget, armorReward);
                break;

            case ObjectiveStep.CollectSomeSticks:
                ShowMaterialObjective("Collect Sticks", GetSticksAmount(), sticksTarget, sticksReward);
                break;

            case ObjectiveStep.CollectSomeCloth:
                ShowMaterialObjective("Collect Cloth", GetClothAmount(), clothTarget, clothReward);
                break;

            case ObjectiveStep.CraftAxe:
                SetText(objectiveTitleText, "Craft the Axe");
                SetText(objectiveProgressText, "");
                SetText(objectiveRewardText, "Reward: +" + craftAxeReward + " coins");
                break;

            case ObjectiveStep.Complete:
                SetText(objectiveTitleText, "Objectives Complete");
                SetText(objectiveProgressText, "All current tasks done");
                SetText(objectiveRewardText, "");
                break;
        }
    }

    private void RefreshQuestPanelUI()
    {
        if (questListText == null)
        {
            return;
        }

        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                questListText.text =
                    "Defeat 3 Enemies\n" +
                    GetKillProgress(firstKillTarget) + "\n" +
                    "Reward: +" + firstKillReward + " Coins";
                break;

            case ObjectiveStep.DefeatSixEnemies:
                questListText.text =
                    "Defeat 6 Enemies\n" +
                    GetKillProgress(secondKillTarget) + "\n" +
                    "Reward: +" + secondKillReward + " Coins";
                break;

            case ObjectiveStep.CollectSomeArmor:
                questListText.text =
                    "Collect Armor\n" +
                    "Progress: " + ClampToTarget(GetArmorAmount(), armorTarget) + " / " + armorTarget + "\n" +
                    "Reward: +" + armorReward + " Coins";
                break;

            case ObjectiveStep.CollectSomeSticks:
                questListText.text =
                    "Collect Sticks\n" +
                    "Progress: " + ClampToTarget(GetSticksAmount(), sticksTarget) + " / " + sticksTarget + "\n" +
                    "Reward: +" + sticksReward + " Coins";
                break;

            case ObjectiveStep.CollectSomeCloth:
                questListText.text =
                    "Collect Cloth\n" +
                    "Progress: " + ClampToTarget(GetClothAmount(), clothTarget) + " / " + clothTarget + "\n" +
                    "Reward: +" + clothReward + " Coins";
                break;

            case ObjectiveStep.CraftAxe:
                questListText.text =
                    "Craft the Axe\n" +
                    "Go to the Vending Machine\n" +
                    "Reward: +" + craftAxeReward + " Coins";
                break;

            case ObjectiveStep.Complete:
                questListText.text =
                    "All Objectives Completed";
                break;
        }
    }

    private void ShowKillObjective(string title, int target, int reward)
    {
        SetText(objectiveTitleText, title);
        SetText(objectiveProgressText, GetKillProgress(target));
        SetText(objectiveRewardText, "Reward: +" + reward + " coins");
    }

    private void ShowMaterialObjective(string title, int currentAmount, int targetAmount, int reward)
    {
        SetText(objectiveTitleText, title);
        SetText(objectiveProgressText, "Progress: " + ClampToTarget(currentAmount, targetAmount) + " / " + targetAmount);
        SetText(objectiveRewardText, "Reward: +" + reward + " coins");
    }

    private string GetKillProgress(int target)
    {
        return "Progress: " + ClampToTarget(currentKillCount, target) + " / " + target;
    }

    private int GetArmorAmount()
    {
        if (materialInventory == null) return 0;

        return materialInventory.shatteredArmor;
    }

    private int GetSticksAmount()
    {
        if (materialInventory == null) return 0;

        return materialInventory.arrowSticks;
    }

    private int GetClothAmount()
    {
        if (materialInventory == null) return 0;

        return materialInventory.tatteredCloth;
    }

    private int ClampToTarget(int amount, int target)
    {
        if (amount > target)
        {
            return target;
        }

        return amount;
    }

    private void ApplyObjectiveHudVisibility()
    {
        if (objectiveHudRoot != null)
        {
            objectiveHudRoot.SetActive(objectiveHudVisible);
            return;
        }

        if (objectiveTitleText != null)
        {
            objectiveTitleText.gameObject.SetActive(objectiveHudVisible);
        }

        if (objectiveProgressText != null)
        {
            objectiveProgressText.gameObject.SetActive(objectiveHudVisible);
        }

        if (objectiveRewardText != null)
        {
            objectiveRewardText.gameObject.SetActive(objectiveHudVisible);
        }
    }

    private void RefreshEyeStateUI()
    {
        if (eyeStateText != null)
        {
            if (objectiveHudVisible)
            {
                eyeStateText.text = "Objective HUD: ON";
            }
            else
            {
                eyeStateText.text = "Objective HUD: OFF";
            }
        }

        if (viewButtonObject != null)
        {
            viewButtonObject.SetActive(!objectiveHudVisible);
        }

        if (unviewButtonObject != null)
        {
            unviewButtonObject.SetActive(objectiveHudVisible);
        }
    }

    private void SetText(TMP_Text textObject, string value)
    {
        if (textObject != null)
        {
            textObject.text = value;
        }
    }
}