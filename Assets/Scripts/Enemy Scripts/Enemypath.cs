using System.Collections.Generic;
using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    [Tooltip("Drag the waypoint Transforms here in the order enemies should walk through them, ending near the tower.")]
    public List<Transform> waypoints = new List<Transform>();

    public int WaypointCount
    {
        get { return waypoints.Count; }
    }

    public Vector3 GetWaypointPosition(int index)
    {
        if (index >= 0 && index < waypoints.Count && waypoints[index] != null)
        {
            return waypoints[index].position;
        }

        return transform.position;
    }

    // Marks the spawn point with a small sphere in the Scene view
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0 || waypoints[0] == null)
        {
            return;
        }
 
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(waypoints[0].position, 0.5f);
    }
}