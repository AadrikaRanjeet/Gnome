using UnityEngine;

// UPDATED: same as before, plus a "Monster" slot for the cave at the end of this path.
public class PathRoute : MonoBehaviour
{
    [SerializeField] Transform waypointRoot;       // the "Waypoints" child object
    [SerializeField] Monster monster;              // NEW: the Monster child of this route
    [SerializeField] Color gizmoColor = Color.yellow;

    public Monster Monster => monster;
    public int Count => waypointRoot != null ? waypointRoot.childCount : 0;

    public Vector3 GetPoint(int i) { return waypointRoot.GetChild(i).position; }

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