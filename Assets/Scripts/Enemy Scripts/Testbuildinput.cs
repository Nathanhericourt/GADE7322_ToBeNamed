using UnityEngine;
using UnityEngine.InputSystem;

// Temp Test Script
// Press 1 to select the Heavy defender, 2 to select the Rapid defender.
public class TestBuildInput : MonoBehaviour
{
    [Tooltip("The Heavy Defender prefab to select when pressing 1")]
    public GameObject defenderPrefab;

    [Tooltip("The Rapid Defender prefab to select when pressing 2")]
    public GameObject secondDefenderPrefab;

    [Tooltip("Drag the GameObject with TowerPlacementManager on it here")]
    public TowerPlacementManager placementManager;

    private void Update()
    {
        if (placementManager == null || Keyboard.current == null)
            return;

        if (defenderPrefab != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            placementManager.SelectTowerToBuild(defenderPrefab);
            Debug.Log("Heavy defender selected - click a placement spot to build it.");
        }

        if (secondDefenderPrefab != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            placementManager.SelectTowerToBuild(secondDefenderPrefab);
            Debug.Log("Rapid defender selected - click a placement spot to build it.");
        }
    }
}
