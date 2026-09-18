using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildDefenderUI : MonoBehaviour
{
    [Header("Defender")]
    [SerializeField] private GameObject defenderPrefab;

    [Header("UI")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buildButton;

    [Header("Placement")]
    [SerializeField] private TowerPlacementManager placementManager;

    private Defender defender;

    private void Start()
    {
        if (defenderPrefab != null)
            defender = defenderPrefab.GetComponent<Defender>();

        UpdateCostText();

        if (buildButton != null)
            buildButton.onClick.AddListener(BuildDefender);
    }

    private void Update()
    {
        UpdateButtonState();
    }

    private void UpdateCostText()
    {
        if (costText == null || defender == null)
            return;

        costText.text = "Cost: " + defender.Cost;
    }

    private void UpdateButtonState()
    {
        if (buildButton == null || defender == null)
            return;

        if (ResourceManager.Instance == null)
            return;

        buildButton.interactable =
            ResourceManager.Instance.CanAfford(defender.Cost);
    }

    private void BuildDefender()
    {
        if (placementManager == null || defenderPrefab == null)
            return;

        if (ResourceManager.Instance != null &&
            !ResourceManager.Instance.CanAfford(defender.Cost))
        {
            return;
        }

        placementManager.SelectTowerToBuild(defenderPrefab);
    }
}