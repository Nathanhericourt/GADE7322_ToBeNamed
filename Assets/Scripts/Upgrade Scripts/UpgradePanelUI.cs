using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

//One panel for every upgrade. Click a placed defender or the tower to show its upgrade info,
//or place a new defender and the panel shows it straight away.
//Put this script on an always-active object (NOT on the panel itself, because the panel gets hidden).
public class UpgradePanelUI : MonoBehaviour
{
    [Header("Panel Parts")]
    [Tooltip("The background box. This is what gets shown / hidden")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeButtonLabel;

    [Header("Icons")]
    [SerializeField] private Sprite heavyIcon;
    [SerializeField] private Sprite rapidIcon;
    [SerializeField] private Sprite freezeIcon;
    [SerializeField] private Sprite luckyIcon;
    [SerializeField] private Sprite towerIcon;

    [Header("Text")]
    [Tooltip("Shown between the current and next value. If it shows as a square, your font has no arrow - type > instead")]
    [SerializeField] private string arrow = "→";
    [Tooltip("{0} becomes the price. If the dot shows as a square, use a dash instead")]
    [SerializeField] private string upgradeLabelFormat = "UPGRADE • {0}";
    [SerializeField] private string maxLevelLabel = "MAX LEVEL";
    [SerializeField] private Color canAffordColor = Color.white;
    [SerializeField] private Color cannotAffordColor = new Color(1f, 0.4f, 0.4f);

    private IUpgradable selected;
    private MonoBehaviour selectedBehaviour; // same object as 'selected', used to check if it was destroyed
    private TowerPlacementManager placementManager;
    private Camera cam;
    private int ignoreClickFrame = -1;

    private void OnEnable()
    {
        TowerPlacementManager.OnDefenderPlaced += HandleDefenderPlaced;
    }

    private void OnDisable()
    {
        TowerPlacementManager.OnDefenderPlaced -= HandleDefenderPlaced;
    }

    private void Start()
    {
        cam = Camera.main;
        placementManager = FindAnyObjectByType<TowerPlacementManager>();

        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        Deselect();
    }

    private void Update()
    {
        //Nothing can be clicked while paused
        if (Time.timeScale == 0f)
            return;

        HandleClicks();

        //The selected defender may have been destroyed by enemies
        if (selected != null && selectedBehaviour == null)
            Deselect();

        if (selected != null)
            Refresh();
    }

    //Selecting
    private void HandleClicks()
    {
        if (Mouse.current == null || cam == null)
            return;

        bool placing = placementManager != null && placementManager.IsPlacing;

        //Right click closes the panel (while placing, right click cancels the build instead)
        if (Mouse.current.rightButton.wasPressedThisFrame && !placing && Time.frameCount != ignoreClickFrame)
        {
            Deselect();
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        //Left click while placing builds the defender, it shouldn't also select things
        if (placing || Time.frameCount == ignoreClickFrame)
            return;

        //Clicks on the UI (like the upgrade button) don't select anything
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;

            //Enemies walking in front don't block the click
            if (col.GetComponentInParent<Enemy>() != null)
                continue;

            //A defender or the tower
            IUpgradable upgradable = col.GetComponentInParent<IUpgradable>();
            if (upgradable != null)
            {
                Select(upgradable);
                return;
            }

            //The little green placement cubes don't block the click either
            if (col.GetComponentInParent<PlacementSpot>() != null)
                continue;

            //Hit the terrain or something else, so stop looking
            break;
        }

        //Clicked empty ground
        Deselect();
    }

    private void HandleDefenderPlaced(Defender defender)
    {
        if (defender == null)
            return;

        ignoreClickFrame = Time.frameCount;
        Select(defender);
    }

    private void Select(IUpgradable target)
    {
        selected = target;
        selectedBehaviour = target as MonoBehaviour;

        panelRoot.SetActive(true);
        Refresh();
    }

    private void Deselect()
    {
        selected = null;
        selectedBehaviour = null;
        panelRoot.SetActive(false);
    }

    //Upgrading
    private void OnUpgradeClicked()
    {
        if (selected == null || Time.timeScale == 0f)
            return;

        selected.TryUpgrade(); // spends the resources and levels it up
        Refresh();
    }

    //Showing the info
    private void Refresh()
    {
        if (selected == null)
            return;

        bool isTower = selected is Tower;

        titleText.text = isTower ? "SELECTED TOWER" : "SELECTED DEFENDER";
        nameText.text = isTower ? selected.DisplayName : selected.DisplayName + " Defender";

        Sprite icon = GetIcon(selected);
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        if (selected.IsMaxLevel)
            levelText.text = "LEVEL " + selected.Level + " (MAX)";
        else
            levelText.text = "LEVEL " + selected.Level + " " + arrow + " " + (selected.Level + 1);

        statsText.text = FormatStats(selected.GetUpgradeSummary());

        //Upgrade button
        if (selected.IsMaxLevel)
        {
            upgradeButton.interactable = false;
            upgradeButtonLabel.text = maxLevelLabel;
            upgradeButtonLabel.color = canAffordColor;
        }
        else
        {
            int cost = selected.NextUpgradeCost;
            bool canAfford = ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(cost);

            upgradeButton.interactable = canAfford;
            upgradeButtonLabel.text = string.Format(upgradeLabelFormat, cost);
            upgradeButtonLabel.color = canAfford ? canAffordColor : cannotAffordColor;
        }
    }

    private Sprite GetIcon(IUpgradable target)
    {
        if (target is HeavyDefender) return heavyIcon;
        if (target is RapidDefender) return rapidIcon;
        if (target is FreezeDefender) return freezeIcon;
        if (target is LuckyDefender) return luckyIcon;
        if (target is Tower) return towerIcon;
        return null;
    }

    //Turns "Damage 10 > 12" into "DAMAGE: 10 → 12"
    private string FormatStats(string summary)
    {
        string[] lines = summary.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string left = lines[i];
            string next = null;

            int arrowIndex = left.IndexOf(" > ");
            if (arrowIndex >= 0)
            {
                next = left.Substring(arrowIndex + 3);
                left = left.Substring(0, arrowIndex);
            }

            int lastSpace = left.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                lines[i] = left.ToUpperInvariant();
                continue;
            }

            string label = left.Substring(0, lastSpace).ToUpperInvariant();
            string current = left.Substring(lastSpace + 1);

            lines[i] = next == null
                ? label + ": " + current
                : label + ": " + current + " " + arrow + " " + next;
        }

        return string.Join("\n", lines);
    }
}