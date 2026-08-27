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

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2)
        {
            return;
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}