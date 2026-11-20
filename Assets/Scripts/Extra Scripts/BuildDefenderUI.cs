using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildDefenderUI : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private TowerPlacementManager placementManager;

    [Header("Heavy Defender")]
    [SerializeField] private GameObject defenderPrefab;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buildButton;
    [SerializeField] private TMP_Text buildButtonLabel;

    [Header("Rapid Defender")]
    [SerializeField] private GameObject secondDefenderPrefab;
    [SerializeField] private TMP_Text secondCostText;
    [SerializeField] private Button secondBuildButton;
    [SerializeField] private TMP_Text secondBuildButtonLabel;

    [Header("Freeze Defender")]
    [SerializeField] private GameObject thirdDefenderPrefab;
    [SerializeField] private TMP_Text thirdCostText;
    [SerializeField] private Button thirdBuildButton;
    [SerializeField] private TMP_Text thirdBuildButtonLabel;

    [Header("Fourth Defender")]
    [SerializeField] private GameObject fourthDefenderPrefab;
    [SerializeField] private TMP_Text fourthCostText;
    [SerializeField] private Button fourthBuildButton;
    [SerializeField] private TMP_Text fourthBuildButtonLabel;

    private Defender firstDefender;
    private Defender secondDefender;
    private Defender thirdDefender;
    private Defender fourthDefender;

    private void Start()
    {
        firstDefender = GetDefender(defenderPrefab);
        secondDefender = GetDefender(secondDefenderPrefab);
        thirdDefender = GetDefender(thirdDefenderPrefab);
        fourthDefender = GetDefender(fourthDefenderPrefab);

        if (buildButton != null)
            buildButton.onClick.AddListener(() => Build(defenderPrefab, firstDefender));

        if (secondBuildButton != null)
            secondBuildButton.onClick.AddListener(() => Build(secondDefenderPrefab, secondDefender));

        if (thirdBuildButton != null)
            thirdBuildButton.onClick.AddListener(() => Build(thirdDefenderPrefab, thirdDefender));
        
        if (fourthBuildButton != null)
            fourthBuildButton.onClick.AddListener(() => Build(fourthDefenderPrefab, fourthDefender));

        RefreshLabels();
    }

    private void Update()
    {
        UpdateButtonState(buildButton, firstDefender);
        UpdateButtonState(secondBuildButton, secondDefender);
        UpdateButtonState(thirdBuildButton, thirdDefender);
        UpdateButtonState(fourthBuildButton, fourthDefender);
        RefreshLabels();
    }

    private static Defender GetDefender(GameObject prefab)
    {
        return prefab != null ? prefab.GetComponent<Defender>() : null;
    }

    private void RefreshLabels()
    {
        SetCombinedCostLabel(costText, firstDefender, secondDefender, thirdDefender, fourthDefender);
        SetCostLabel(secondCostText, secondDefender);
        SetCostLabel(thirdCostText, thirdDefender);
        SetCostLabel(fourthCostText, fourthDefender);
        SetButtonLabel(buildButtonLabel, firstDefender);
        SetButtonLabel(secondBuildButtonLabel, secondDefender);
        SetButtonLabel(thirdBuildButtonLabel, thirdDefender);
        SetButtonLabel(fourthBuildButtonLabel, fourthDefender);
    }

    private static void SetCombinedCostLabel(TMP_Text label, params Defender[] defenders)
    {
        if (label == null)
            return;

        string text = "";
        foreach (Defender defender in defenders)
        {
            if (defender == null)
                continue;

            if (text.Length > 0)
                text += "  |  ";

            text += defender.DisplayName + ": " + defender.Cost;
        }

        if (text.Length > 0)
            label.text = text;
    }

    private static void SetCostLabel(TMP_Text label, Defender defender)
    {
        if (label == null || defender == null)
            return;

        label.text = "Cost: " + defender.Cost;
    }

    private static void SetButtonLabel(TMP_Text label, Defender defender)
    {
        if (label == null || defender == null)
            return;

        label.text = "BUILD " + defender.DisplayName.ToUpperInvariant();
    }

    private static void UpdateButtonState(Button button, Defender defender)
    {
        if (button == null || defender == null || ResourceManager.Instance == null)
            return;

        button.interactable = ResourceManager.Instance.CanAfford(defender.Cost);
    }

    private void Build(GameObject prefab, Defender defender)
    {
        // Buttons do nothing while the game is paused
        if (Time.timeScale == 0f)
            return;
            
        if (placementManager == null || prefab == null || defender == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(defender.Cost))
            return;

        placementManager.SelectTowerToBuild(prefab);
    }
}
