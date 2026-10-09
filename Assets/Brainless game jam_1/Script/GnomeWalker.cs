using System;
using UnityEngine;

// UPDATED: now also dies if it is inside an active (Out) danger zone.
public class GnomeWalker : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;  // optional, auto-found in children
    [SerializeField] bool artFacesRight = true;
    [SerializeField] GameObject deathFx;             // optional: any poof/puff prefab

    public event Action<GnomeWalker> Arrived;        // reached the cave = saved
    public event Action<GnomeWalker> Died;           // hit the fire

    PathRoute route;
    int next;
    float speed;
    Vector2 offset;
    bool done;

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
        if (route == null || done) return;

        Vector3 pos = transform.position;
        Vector3 wp = route.GetPoint(next);
        Vector3 target = new Vector3(wp.x + offset.x, wp.y + offset.y, pos.z);
        Vector3 delta = target - pos;
        float step = speed * Time.deltaTime;

        if (delta.magnitude <= step)
        {
            transform.position = target;
            next++;
            if (next >= route.Count) { Finish(true); return; }
        }
        else
        {
            transform.position = pos + delta.normalized * step;
            if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.01f)
                spriteRenderer.flipX = artFacesRight ? delta.x < 0f : delta.x > 0f;
        }

        // Danger check: inside an active zone = dead
        if (route.Monster != null && route.Monster.TryKill(transform.position))
            Finish(false);
    }

    void Finish(bool saved)
    {
        done = true;
        if (saved) Arrived?.Invoke(this);
        else
        {
            if (deathFx != null) Instantiate(deathFx, transform.position, Quaternion.identity);
            Died?.Invoke(this);
        }
        Destroy(gameObject);
    }
}