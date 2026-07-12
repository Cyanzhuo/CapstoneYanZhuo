#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using TMPro;
using UnityEngine;

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

    [Header("Gameplay References")]
    [SerializeField] private KillCounter killCounter;
    [SerializeField] private MaterialInventory materialInventory;
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Current Objective HUD")]
    [SerializeField] private GameObject objectiveHudRoot;
    [SerializeField] private GameObject objectiveBackgroundObject;
    [SerializeField] private TMP_Text objectiveTitleText;
    [SerializeField] private TMP_Text objectiveProgressText;
    [SerializeField] private TMP_Text objectiveRewardText;

    [Header("Quest Panel")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private TMP_Text currentQuestText;
    [SerializeField] private TMP_Text nextQuestText;
    [SerializeField] private TMP_Text thirdQuestText;

    [Header("View Buttons")]
    [SerializeField] private GameObject viewButtonObject;
    [SerializeField] private GameObject unviewButtonObject;
    [SerializeField] private TMP_Text eyeStateText;

    [Header("Starting Settings")]
    [SerializeField] private bool startWithObjectiveHudVisible = false;

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

    [Header("Objective Complete")]
    [SerializeField] private float objectiveCompleteDelay = 2f;

    private ObjectiveStep currentObjectiveStep =
        ObjectiveStep.DefeatThreeEnemies;

    private bool objectiveHudVisible;
    private bool questPanelOpen;
    private bool showingCompleteMessage;

    private float completeMessageTimer;
    private float previousTimeScale = 1f;

    private int currentKillCount;

    private void OnEnable()
    {
        KillCounter.OnKillRegistered += UpdateKillCount;
    }

    private void OnDisable()
    {
        KillCounter.OnKillRegistered -= UpdateKillCount;

        if (questPanelOpen)
        {
            Time.timeScale = previousTimeScale;
            questPanelOpen = false;
        }
    }

    private void Start()
    {
        FindGameplayReferences();

        if (killCounter != null)
        {
            currentKillCount = killCounter.KillCount;
        }

        objectiveHudVisible = startWithObjectiveHudVisible;
        questPanelOpen = false;

        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }

        ApplyObjectiveHudVisibility();
        RefreshObjectiveUI();
        RefreshQuestPanelUI();
        RefreshViewButtonUI();
    }

    private void Update()
    {
        FindGameplayReferences();

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

            if (completeMessageTimer <= 0f)
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
        if (Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame)
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
        if (Keyboard.current != null &&
            Keyboard.current.vKey.wasPressedThisFrame)
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

    private void FindGameplayReferences()
    {
        if (killCounter == null)
        {
            killCounter = FindFirstObjectByType<KillCounter>();
        }

        if (killCounter != null &&
            killCounter.KillCount > currentKillCount)
        {
            currentKillCount = killCounter.KillCount;
        }

        if (materialInventory == null)
        {
            materialInventory =
                FindFirstObjectByType<MaterialInventory>();
        }

        if (playerInventory == null)
        {
            playerInventory = PlayerInventory.Instance;
        }

        if (playerInventory == null)
        {
            playerInventory =
                FindFirstObjectByType<PlayerInventory>();
        }
    }

    private void UpdateKillCount(int newKillCount)
    {
        currentKillCount = newKillCount;
    }

    private void CheckCurrentObjective()
    {
        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                if (currentKillCount >= firstKillTarget)
                {
                    CompleteCurrentObjective(firstKillReward);
                    Debug.Log("Objective Complete: Defeat 3 enemies.");
                }
                break;

            case ObjectiveStep.DefeatSixEnemies:
                if (currentKillCount >= secondKillTarget)
                {
                    CompleteCurrentObjective(secondKillReward);
                    Debug.Log("Objective Complete: Defeat 6 enemies.");
                }
                break;

            case ObjectiveStep.CollectSomeArmor:
                if (materialInventory != null &&
                    materialInventory.shatteredArmor >= armorTarget)
                {
                    CompleteCurrentObjective(armorReward);
                    Debug.Log("Objective Complete: Collect Armor.");
                }
                break;

            case ObjectiveStep.CollectSomeSticks:
                if (materialInventory != null &&
                    materialInventory.arrowSticks >= sticksTarget)
                {
                    CompleteCurrentObjective(sticksReward);
                    Debug.Log("Objective Complete: Collect Sticks.");
                }
                break;

            case ObjectiveStep.CollectSomeCloth:
                if (materialInventory != null &&
                    materialInventory.tatteredCloth >= clothTarget)
                {
                    CompleteCurrentObjective(clothReward);
                    Debug.Log("Objective Complete: Collect Cloth.");
                }
                break;

            case ObjectiveStep.CraftAxe:
                if (materialInventory != null &&
                    materialInventory.axeUnlocked)
                {
                    CompleteCurrentObjective(craftAxeReward);
                    Debug.Log("Objective Complete: Craft the Axe.");
                }
                break;

            case ObjectiveStep.Complete:
                break;
        }
    }

    private void CompleteCurrentObjective(int coinReward)
    {
        GiveCoinReward(coinReward);

        showingCompleteMessage = true;
        completeMessageTimer = objectiveCompleteDelay;

        ShowObjectiveCompletedMessage();
    }

    private void MoveToNextObjective()
    {
        currentObjectiveStep =
            GetObjectiveStepAtOffset(currentObjectiveStep, 1);
    }

    private ObjectiveStep GetObjectiveStepAtOffset(
        ObjectiveStep startingStep,
        int offset
    )
    {
        int requestedStep = (int)startingStep + offset;
        int completeStep = (int)ObjectiveStep.Complete;

        if (requestedStep >= completeStep)
        {
            return ObjectiveStep.Complete;
        }

        return (ObjectiveStep)requestedStep;
    }

    private void GiveCoinReward(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

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
        if (questPanelOpen)
        {
            return;
        }

        questPanelOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }

        RefreshQuestPanelUI();
        RefreshViewButtonUI();
    }

    public void CloseQuestPanel()
    {
        if (!questPanelOpen)
        {
            return;
        }

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

        if (objectiveHudVisible)
        {
            RefreshObjectiveUI();
        }

        RefreshViewButtonUI();
    }

    public void ShowObjectiveHud()
    {
        objectiveHudVisible = true;

        ApplyObjectiveHudVisibility();
        RefreshObjectiveUI();
        RefreshViewButtonUI();
    }

    public void HideObjectiveHud()
    {
        objectiveHudVisible = false;

        ApplyObjectiveHudVisibility();
        RefreshViewButtonUI();
    }

    private void ApplyObjectiveHudVisibility()
    {
        SetObjectActive(
            objectiveHudRoot,
            objectiveHudVisible
        );

        SetObjectActive(
            objectiveBackgroundObject,
            objectiveHudVisible
        );

        if (objectiveTitleText != null)
        {
            SetObjectActive(
                objectiveTitleText.gameObject,
                objectiveHudVisible
            );
        }

        if (objectiveProgressText != null)
        {
            SetObjectActive(
                objectiveProgressText.gameObject,
                objectiveHudVisible
            );
        }

        if (objectiveRewardText != null)
        {
            SetObjectActive(
                objectiveRewardText.gameObject,
                objectiveHudVisible
            );
        }
    }

    private void SetObjectActive(
        GameObject targetObject,
        bool activeState
    )
    {
        if (targetObject == null)
        {
            return;
        }

        if (targetObject.activeSelf != activeState)
        {
            targetObject.SetActive(activeState);
        }
    }

    private void ShowObjectiveCompletedMessage()
    {
        if (!objectiveHudVisible)
        {
            return;
        }

        SetText(
            objectiveTitleText,
            "Objective Complete"
        );

        SetText(
            objectiveProgressText,
            ""
        );

        SetText(
            objectiveRewardText,
            ""
        );
    }

    private void RefreshObjectiveUI()
    {
        if (!objectiveHudVisible)
        {
            return;
        }

        switch (currentObjectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                ShowObjective(
                    "Defeat 3 Enemies",
                    GetKillProgress(firstKillTarget),
                    firstKillReward
                );
                break;

            case ObjectiveStep.DefeatSixEnemies:
                ShowObjective(
                    "Defeat 6 Enemies",
                    GetKillProgress(secondKillTarget),
                    secondKillReward
                );
                break;

            case ObjectiveStep.CollectSomeArmor:
                ShowObjective(
                    "Collect Armor",
                    GetMaterialProgress(
                        GetArmorAmount(),
                        armorTarget
                    ),
                    armorReward
                );
                break;

            case ObjectiveStep.CollectSomeSticks:
                ShowObjective(
                    "Collect Sticks",
                    GetMaterialProgress(
                        GetSticksAmount(),
                        sticksTarget
                    ),
                    sticksReward
                );
                break;

            case ObjectiveStep.CollectSomeCloth:
                ShowObjective(
                    "Collect Cloth",
                    GetMaterialProgress(
                        GetClothAmount(),
                        clothTarget
                    ),
                    clothReward
                );
                break;

            case ObjectiveStep.CraftAxe:
                ShowObjective(
                    "Craft the Axe",
                    "Use Vending Machine",
                    craftAxeReward
                );
                break;

            case ObjectiveStep.Complete:
                SetText(
                    objectiveTitleText,
                    "Completed"
                );

                SetText(
                    objectiveProgressText,
                    ""
                );

                SetText(
                    objectiveRewardText,
                    ""
                );
                break;
        }
    }

    private void ShowObjective(
        string title,
        string progress,
        int reward
    )
    {
        SetText(
            objectiveTitleText,
            title
        );

        SetText(
            objectiveProgressText,
            progress
        );

        SetText(
            objectiveRewardText,
            "Reward: +" + reward + " coins"
        );
    }

    private void RefreshQuestPanelUI()
    {
        ObjectiveStep firstSlotStep =
            GetObjectiveStepAtOffset(currentObjectiveStep, 0);

        ObjectiveStep secondSlotStep =
            GetObjectiveStepAtOffset(currentObjectiveStep, 1);

        ObjectiveStep thirdSlotStep =
            GetObjectiveStepAtOffset(currentObjectiveStep, 2);

        SetQuestSlot(
            currentQuestText,
            firstSlotStep,
            true
        );

        SetQuestSlot(
            nextQuestText,
            secondSlotStep,
            false
        );

        SetQuestSlot(
            thirdQuestText,
            thirdSlotStep,
            false
        );
    }

    private void SetQuestSlot(
        TMP_Text questText,
        ObjectiveStep objectiveStep,
        bool isCurrentSlot
    )
    {
        if (questText == null)
        {
            return;
        }

        if (objectiveStep == ObjectiveStep.Complete)
        {
            if (isCurrentSlot &&
                currentObjectiveStep == ObjectiveStep.Complete)
            {
                questText.gameObject.SetActive(true);
                questText.text = "Completed";
            }
            else
            {
                questText.gameObject.SetActive(false);
            }

            return;
        }

        questText.gameObject.SetActive(true);
        questText.text = GetQuestPanelText(objectiveStep);
    }

    private string GetQuestPanelText(
        ObjectiveStep objectiveStep
    )
    {
        switch (objectiveStep)
        {
            case ObjectiveStep.DefeatThreeEnemies:
                return
                    "Defeat 3 Enemies\n" +
                    GetKillProgress(firstKillTarget) +
                    "\n" +
                    "Reward: +" +
                    firstKillReward +
                    " Coins";

            case ObjectiveStep.DefeatSixEnemies:
                return
                    "Defeat 6 Enemies\n" +
                    GetKillProgress(secondKillTarget) +
                    "\n" +
                    "Reward: +" +
                    secondKillReward +
                    " Coins";

            case ObjectiveStep.CollectSomeArmor:
                return
                    "Collect Armor\n" +
                    GetMaterialProgress(
                        GetArmorAmount(),
                        armorTarget
                    ) +
                    "\n" +
                    "Reward: +" +
                    armorReward +
                    " Coins";

            case ObjectiveStep.CollectSomeSticks:
                return
                    "Collect Sticks\n" +
                    GetMaterialProgress(
                        GetSticksAmount(),
                        sticksTarget
                    ) +
                    "\n" +
                    "Reward: +" +
                    sticksReward +
                    " Coins";

            case ObjectiveStep.CollectSomeCloth:
                return
                    "Collect Cloth\n" +
                    GetMaterialProgress(
                        GetClothAmount(),
                        clothTarget
                    ) +
                    "\n" +
                    "Reward: +" +
                    clothReward +
                    " Coins";

            case ObjectiveStep.CraftAxe:
                return
                    "Craft the Axe\n" +
                    "Use Vending Machine\n" +
                    "Reward: +" +
                    craftAxeReward +
                    " Coins";

            case ObjectiveStep.Complete:
                return "Completed";
        }

        return "";
    }

    private string GetKillProgress(int target)
    {
        return "Progress: " +
               ClampToTarget(currentKillCount, target) +
               " / " +
               target;
    }

    private string GetMaterialProgress(
        int currentAmount,
        int targetAmount
    )
    {
        return "Progress: " +
               ClampToTarget(currentAmount, targetAmount) +
               " / " +
               targetAmount;
    }

    private int GetArmorAmount()
    {
        if (materialInventory == null)
        {
            return 0;
        }

        return materialInventory.shatteredArmor;
    }

    private int GetSticksAmount()
    {
        if (materialInventory == null)
        {
            return 0;
        }

        return materialInventory.arrowSticks;
    }

    private int GetClothAmount()
    {
        if (materialInventory == null)
        {
            return 0;
        }

        return materialInventory.tatteredCloth;
    }

    private int ClampToTarget(
        int amount,
        int target
    )
    {
        return Mathf.Clamp(amount, 0, target);
    }

    private void RefreshViewButtonUI()
    {
        if (eyeStateText != null)
        {
            eyeStateText.text =
                objectiveHudVisible
                    ? "Objective HUD: ON"
                    : "Objective HUD: OFF";
        }

        if (viewButtonObject != null)
        {
            viewButtonObject.SetActive(
                !objectiveHudVisible
            );
        }

        if (unviewButtonObject != null)
        {
            unviewButtonObject.SetActive(
                objectiveHudVisible
            );
        }
    }

    private void SetText(
        TMP_Text targetText,
        string value
    )
    {
        if (targetText != null)
        {
            targetText.text = value;
        }
    }
}