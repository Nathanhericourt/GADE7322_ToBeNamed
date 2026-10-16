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

    private Defender firstDefender;
    private Defender secondDefender;

    private void Start()
    {
        firstDefender = GetDefender(defenderPrefab);
        secondDefender = GetDefender(secondDefenderPrefab);

        if (buildButton != null)
            buildButton.onClick.AddListener(() => Build(defenderPrefab, firstDefender));

        if (secondBuildButton != null)
            secondBuildButton.onClick.AddListener(() => Build(secondDefenderPrefab, secondDefender));

        RefreshLabels();
    }

    private void Update()
    {
        UpdateButtonState(buildButton, firstDefender);
        UpdateButtonState(secondBuildButton, secondDefender);
        RefreshLabels();
    }

    private static Defender GetDefender(GameObject prefab)
    {
        return prefab != null ? prefab.GetComponent<Defender>() : null;
    }

    private void RefreshLabels()
    {
        SetCostLabel(costText, firstDefender, secondDefender);
        SetCostLabel(secondCostText, secondDefender, null);
        SetButtonLabel(buildButtonLabel, firstDefender);
        SetButtonLabel(secondBuildButtonLabel, secondDefender);
    }

    private static void SetCostLabel(TMP_Text label, Defender defender, Defender extraDefender)
    {
        if (label == null)
            return;

        if (defender != null && extraDefender != null)
        {
            label.text = defender.DisplayName + ": " + defender.Cost + "  |  " + extraDefender.DisplayName + ": " + extraDefender.Cost;
            return;
        }

        if (defender != null)
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
        if (placementManager == null || prefab == null || defender == null)
            return;

        if (ResourceManager.Instance != null && !ResourceManager.Instance.CanAfford(defender.Cost))
            return;

        placementManager.SelectTowerToBuild(prefab);
    }
}
