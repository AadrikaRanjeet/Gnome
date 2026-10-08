using UnityEngine;

// One of these per path (Upper / Middle / Lower).
// Attach to Route_Upper, Route_Middle, Route_Lower.
// The waypoints are the children of "waypointRoot", in hierarchy order (top = first).
public class PathRoute : MonoBehaviour
{
    [SerializeField] Transform waypointRoot;       // the "Waypoints" child object
    [SerializeField] Color gizmoColor = Color.yellow;

    public int Count => waypointRoot != null ? waypointRoot.childCount : 0;

    public Vector3 GetPoint(int i) { return waypointRoot.GetChild(i).position; }

    // Distance from a world point to this path's polyline (used to pick the nearest path on click).
    public float DistanceTo(Vector2 p)
    {
        float best = float.MaxValue;
        for (int i = 0; i < Count - 1; i++)
        {
            Vector2 a = GetPoint(i), b = GetPoint(i + 1);
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            best = Mathf.Min(best, (p - (a + ab * t)).magnitude);
        }
        return best;
    }

    // Draws the path in the Scene view so you can see/adjust it over your background art.
    void OnDrawGizmos()
    {
        if (waypointRoot == null) return;
        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypointRoot.childCount; i++)
        {
            Vector3 p = waypointRoot.GetChild(i).position;
            Gizmos.DrawSphere(p, 0.12f);
            if (i > 0) Gizmos.DrawLine(waypointRoot.GetChild(i - 1).position, p);
        }
    }
}