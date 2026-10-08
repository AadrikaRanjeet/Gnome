using UnityEngine;
using UnityEngine.EventSystems;

// Attach to an empty "Input" object. Click/tap near a path to send the stream there.
// Keys: 1/2/3 pick a path, Space or S stops the stream (handy for testing).
// Uses legacy Input: Project Settings > Player > Active Input Handling = "Both".
public class PathSelectInput : MonoBehaviour
{
    [SerializeField] GnomeSpawner spawner;
    Camera cam;

    void Awake() { cam = Camera.main; }   // your camera must be tagged "MainCamera"

    void Update()
    {
        if (Input.GetMouseButtonDown(0) &&
            !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
            var routes = spawner.Routes;

            int best = -1;
            float bestDist = float.MaxValue;
            foreach (var col in Physics2D.OverlapPointAll(p))
            {
                var route = col.GetComponentInParent<PathRoute>();
                if (route == null) continue;
                int idx = System.Array.IndexOf(routes, route);
                if (idx < 0) continue;

                // Click areas can overlap near the start: the path whose line is closest wins.
                float d = route.DistanceTo(p);
                if (d < bestDist) { bestDist = d; best = idx; }
            }
            if (best >= 0) spawner.SelectRoute(best);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) spawner.SelectRoute(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) spawner.SelectRoute(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) spawner.SelectRoute(2);
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.S)) spawner.Stop();
    }
}