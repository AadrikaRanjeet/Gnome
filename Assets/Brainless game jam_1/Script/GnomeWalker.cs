using System;
using UnityEngine;

// Attach to the Gnome prefab (root object that has the SpriteRenderer / Animator).
// Walks from where it spawned, through every waypoint of its route, then reports "Arrived".
public class GnomeWalker : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;  // optional, auto-found in children
    [SerializeField] bool artFacesRight = true;      // untick if your gnome art faces LEFT by default

    public event Action<GnomeWalker> Arrived;

    PathRoute route;
    int next;
    float speed;
    Vector2 offset;   // tiny per-gnome offset so the stream doesn't stack into one gnome

    public void Init(PathRoute r, float worldSpeed, float jitter)
    {
        route = r;
        speed = worldSpeed;
        next = 0;
        offset = UnityEngine.Random.insideUnitCircle * jitter;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
    }

    void Update()
    {
        if (route == null) return;

        Vector3 pos = transform.position;
        Vector3 wp = route.GetPoint(next);
        Vector3 target = new Vector3(wp.x + offset.x, wp.y + offset.y, pos.z); // keep our own z
        Vector3 delta = target - pos;
        float step = speed * Time.deltaTime;

        if (delta.magnitude <= step)
        {
            transform.position = target;
            next++;
            if (next >= route.Count)
            {
                Arrived?.Invoke(this);
                Destroy(gameObject);
            }
            return;
        }

        transform.position = pos + delta.normalized * step;

        if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.01f)
            spriteRenderer.flipX = artFacesRight ? delta.x < 0f : delta.x > 0f;
    }
}