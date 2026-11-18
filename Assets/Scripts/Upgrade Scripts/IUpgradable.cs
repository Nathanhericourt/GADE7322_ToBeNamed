//Anything the player can upgrade implements this interface so one upgrade panle can be used for all upgrades.
public interface IUpgradable
{
    string DisplayName { get; }
    int Level { get; }
    int MaxLevel { get; }
    bool IsMaxLevel { get; }
    int NextUpgradeCost { get; }

    //Health text lines to display in the upgrade panel.
    string GetUpgradeSummary();

    //Spends the resources and levels up
    bool TryUpgrade();
}