using UnityEngine;
namespace TowerDefence.Placement
{
    //THis script marks a region of the terrain where towers cant be placed
    //attaches empty game object and position/scale it over the area to exclude eg. enemy path, rocks, trees, etc.
    public class NoBuildZones : MonoBehaviour
    {
        public enum ZoneShape { Box, Sphere };

        [Tooltip("box uses the transforms scale as its size, sphere uses radius below")]
        public ZoneShape shape = ZoneShape.Box;

        [Tooltip("Used only when shape = Shpere. World- space radius")]
        public float radius = 5f;

        [Tooltip("Optional extra padding added around the zone, in meters")]
        public float padding = 0f;

        public Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.35f);

        //returns true if the given world-space point falls inside this zone(including padding)

        public bool Contains(Vector3 point)
        {
            switch (shape)
            {
                case ZoneShape.Sphere:
                    return Vector3.Distance(point, transform.position) <= radius + padding;

                case ZoneShape.Box:
                default:
                    //convert to the box's local space so rotation/scale are respected
                    Vector3 local = transform.InverseTransformPoint(point);
                    float padFraction = padding / MaxScaleComponent();
                    Vector3 halfExtents = (Vector3.one * 0.5f) + (Vector3.one * padFraction);
                    return Mathf.Abs(local.x) <= halfExtents.x
                        && Mathf.Abs(local.y) <= halfExtents.y
                        && Mathf.Abs(local.z) <= halfExtents.z;
            }
        }

        float MaxScaleComponent()
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(0.001f, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            if (shape == ZoneShape.Sphere)
            {
                Gizmos.DrawSphere(transform.position, radius + padding);
            }
            else
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                float padFraction = (padding * 2f) / MaxScaleComponent();
                Gizmos.DrawCube(Vector3.zero, Vector3.one + Vector3.one * padFraction);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
