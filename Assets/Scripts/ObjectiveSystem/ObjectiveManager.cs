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

    [Header("Objective UI")]
    [SerializeField] private TMP_Text objectiveTitleText;
    [SerializeField] private TMP_Text objectiveProgressText;
    [SerializeField] private TMP_Text objectiveRewardText;

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

    private ObjectiveStep currentObjectiveStep = ObjectiveStep.DefeatThreeEnemies;

    private bool showingCompleteMessage;
    private float completeMessageTimer;

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

        RefreshObjectiveUI();
    }

    private void Update()
    {
        FindMissingReferences();

        if (showingCompleteMessage)
        {
            completeMessageTimer -= Time.deltaTime;

            ShowObjectiveCompletedMessage();

            if (completeMessageTimer <= 0)
            {
                showingCompleteMessage = false;
                MoveToNextObjective();
                RefreshObjectiveUI();
            }

            return;
        }

        CheckCurrentObjective();

        if (showingCompleteMessage)
        {
            return;
        }

        RefreshObjectiveUI();
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

    #region Objective Checks
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
    #endregion

    #region Objective Progression
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
    #endregion

    #region Rewards
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
    #endregion

    #region UI
    private void StartObjectiveCompletedMessage()
    {
        showingCompleteMessage = true;
        completeMessageTimer = objectiveCompleteDelay;

        ShowObjectiveCompletedMessage();
    }

    private void ShowObjectiveCompletedMessage()
    {
        SetText(objectiveTitleText, "Objective Completed");
        SetText(objectiveProgressText, "");
        SetText(objectiveRewardText, "");
    }

    private void RefreshObjectiveUI()
    {
        if (objectiveTitleText == null && objectiveProgressText == null && objectiveRewardText == null)
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
                ShowMaterialObjective(
                    "Collect Armor",
                    materialInventory != null ? materialInventory.shatteredArmor : 0,
                    armorTarget,
                    armorReward
                );
                break;

            case ObjectiveStep.CollectSomeSticks:
                ShowMaterialObjective(
                    "Collect Sticks",
                    materialInventory != null ? materialInventory.arrowSticks : 0,
                    sticksTarget,
                    sticksReward
                );
                break;

            case ObjectiveStep.CollectSomeCloth:
                ShowMaterialObjective(
                    "Collect Cloth",
                    materialInventory != null ? materialInventory.tatteredCloth : 0,
                    clothTarget,
                    clothReward
                );
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

    private void ShowKillObjective(string title, int target, int reward)
    {
        int shownKills = currentKillCount;

        if (shownKills > target)
        {
            shownKills = target;
        }

        SetText(objectiveTitleText, title);
        SetText(objectiveProgressText, "Progress: " + shownKills + " / " + target);
        SetText(objectiveRewardText, "Reward: +" + reward + " coins");
    }

    private void ShowMaterialObjective(string title, int currentAmount, int targetAmount, int reward)
    {
        if (currentAmount > targetAmount)
        {
            currentAmount = targetAmount;
        }

        SetText(objectiveTitleText, title);
        SetText(objectiveProgressText, "Progress: " + currentAmount + " / " + targetAmount);
        SetText(objectiveRewardText, "Reward: +" + reward + " coins");
    }

    private void SetText(TMP_Text textObject, string value)
    {
        if (textObject != null)
        {
            textObject.text = value;
        }
    }
    #endregion
}