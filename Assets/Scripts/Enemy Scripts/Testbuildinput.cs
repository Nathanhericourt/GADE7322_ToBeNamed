using UnityEngine;
using UnityEngine.InputSystem; // add this

// Temp Test Script
// Because theres no UI button yet to select which defender to build
// lets you press "1" to select the Defender prefab for placement,
// so the click-to-build flow can be tested right now.
// Delete this once the UI button calls TowerPlacementManager.SelectTowerToBuild() instead.
public class TestBuildInput : MonoBehaviour
{
    [Tooltip("The Defender prefab to select when pressing 1")]
    public GameObject defenderPrefab;

    [Tooltip("Drag the GameObject with TowerPlacementManager on it here")]
    public TowerPlacementManager placementManager;

    private void Update()
    {
        // New Input System equivalent of Input.GetKeyDown(KeyCode.Alpha1)
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            placementManager.SelectTowerToBuild(defenderPrefab);
            Debug.Log("Defender selected - click a placement spot to build it.");
        }
    }
}