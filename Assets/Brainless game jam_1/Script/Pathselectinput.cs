using UnityEngine;
using UnityEngine.EventSystems;

// FULL REPLACEMENT. On the "Input" object.
//   Click a GNOME            -> halt / resume that gnome (gnomes win over paths)
//   Click a path (no gnome)  -> send the stream down that path
//   Keys 1 / 2 / 3           -> pick a path
//   Space                    -> halt / resume the gnome under the mouse; if none, stop the stream
//   S                        -> always stop the stream
public class PathSelectInput : MonoBehaviour
{
    [SerializeField] GnomeSpawner spawner;
    [SerializeField] float gnomeClickPadding = 0.15f;   // makes gnomes easier to hit (world units)
    Camera cam;

    void Awake() { cam = Camera.main; }

    void Update()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0) &&
            !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            var gnome = GnomeWalker.PickAt(mouseWorld, gnomeClickPadding);
            if (gnome != null) gnome.ToggleHalt();
            else SelectPathAt(mouseWorld);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var gnome = GnomeWalker.PickAt(mouseWorld, gnomeClickPadding);
            if (gnome != null) gnome.ToggleHalt();
            else spawner.Stop();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) spawner.SelectRoute(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) spawner.SelectRoute(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) spawner.SelectRoute(2);
        if (Input.GetKeyDown(KeyCode.S)) spawner.Stop();
    }

    void SelectPathAt(Vector2 p)
    {
        var routes = spawner.Routes;
        int best = -1;
        float bestDist = float.MaxValue;
        foreach (var col in Physics2D.OverlapPointAll(p))
        {
            var route = col.GetComponentInParent<PathRoute>();
            if (route == null) continue;
            int idx = System.Array.IndexOf(routes, route);
            if (idx < 0) continue;

            float d = route.DistanceTo(p);   // overlapping click areas: the closest path wins
            if (d < bestDist) { bestDist = d; best = idx; }
        }
        if (best >= 0) spawner.SelectRoute(best);
    }
}