using System;
using System.Collections.Generic;
using UnityEngine;

// FULL REPLACEMENT. Adds: click-to-halt (crouch + freeze), traffic queue (no passing through the gnome ahead),
// click picking, plus everything from before (walking, monster kill check, height sorting).
public class GnomeWalker : MonoBehaviour
{
    // every living gnome (used for picking and for the traffic queue)
    public static readonly List<GnomeWalker> All = new List<GnomeWalker>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { All.Clear(); }

    [SerializeField] SpriteRenderer spriteRenderer;  // optional, auto-found in children
    [SerializeField] bool artFacesRight = true;
    [SerializeField] GameObject deathFx;             // optional poof prefab

    public event Action<GnomeWalker> Arrived;        // reached the cave = saved
    public event Action<GnomeWalker> Died;           // caught by the monster

    public bool Halted { get; private set; }
    public float Traveled { get; private set; }      // distance walked along the path (used for queue order)

    PathRoute route;
    int next;
    float speed;
    Vector2 offset;
    bool done;

    // set by the spawner
    float minGap = 0.5f;             // closest a gnome may get to the gnome ahead of it
    float crouchY = 0.9f;            // Y scale while halted
    Color haltTint = Color.white;    // colour multiplier while halted
    Vector3 baseScale = Vector3.one;
    Animator anim;

    SpriteRenderer[] rends;
    Color[] origColors;

    // front/back sorting
    int[] origOrders;
    int minOrig;
    bool ySort;
    int ySortBase;
    float ySortScale;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void EnsureRenderers()
    {
        if (rends != null) return;
        rends = GetComponentsInChildren<SpriteRenderer>();
        origColors = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++) origColors[i] = rends[i].color;
    }

    public void SetTraffic(float gap, float crouchScaleY, Color tint)
    {
        minGap = gap;
        crouchY = crouchScaleY;
        haltTint = tint;
    }

    public void Init(PathRoute r, float worldSpeed, float jitter)
    {
        route = r;
        speed = worldSpeed;
        next = 0;
        offset = UnityEngine.Random.insideUnitCircle * jitter;
        EnsureRenderers();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        baseScale = transform.localScale;     // the spawner has already normalised the size

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
    }

    // ---------- sorting ----------

    public void EnableYSort(int sortBase, float scale)
    {
        ySort = true;
        ySortBase = sortBase;
        ySortScale = scale;
        EnsureRenderers();
        origOrders = new int[rends.Length];
        minOrig = int.MaxValue;
        for (int i = 0; i < rends.Length; i++)
        {
            origOrders[i] = rends[i].sortingOrder;
            if (origOrders[i] < minOrig) minOrig = origOrders[i];
        }
        ApplySort();
    }

    void ApplySort()
    {
        if (!ySort || rends == null) return;
        int order = ySortBase - Mathf.RoundToInt(transform.position.y * ySortScale);
        for (int i = 0; i < rends.Length; i++)
            rends[i].sortingOrder = order + (origOrders[i] - minOrig);
    }

    void LateUpdate() { ApplySort(); }

    // ---------- halt ----------

    public void ToggleHalt() { SetHalted(!Halted); }

    public void SetHalted(bool halted)
    {
        Halted = halted;
        transform.localScale = halted ? new Vector3(baseScale.x, baseScale.y * crouchY, baseScale.z) : baseScale;
        EnsureRenderers();
        for (int i = 0; i < rends.Length; i++)
            rends[i].color = halted ? origColors[i] * haltTint : origColors[i];
    }

    Bounds GetBounds()
    {
        EnsureRenderers();
        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in rends)
        {
            if (!r.enabled) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // The gnome under a world point (nearest centre wins if several overlap). 'pad' makes the target bigger/easier to hit.
    public static GnomeWalker PickAt(Vector2 p, float pad)
    {
        GnomeWalker best = null;
        float bestDist = float.MaxValue;
        foreach (var g in All)
        {
            if (g == null || g.done) continue;
            Bounds b = g.GetBounds();
            Vector2 c = b.center;
            Vector2 ext = (Vector2)b.extents + new Vector2(pad, pad);
            if (Mathf.Abs(p.x - c.x) > ext.x || Mathf.Abs(p.y - c.y) > ext.y) continue;
            float d = (p - c).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = g; }
        }
        return best;
    }

    // ---------- traffic ----------

    GnomeWalker FindAhead()
    {
        GnomeWalker best = null;
        float bestT = float.MaxValue;
        foreach (var g in All)
        {
            if (g == this || g.done || g.route != route) continue;
            if (g.Traveled > Traveled && g.Traveled < bestT) { best = g; bestT = g.Traveled; }
        }
        return best;
    }

    void SetMoving(bool moving)
    {
        if (anim != null) anim.speed = moving ? 1f : 0f;   // freeze the walk cycle while stopped
    }

    // ---------- main loop ----------

    void Update()
    {
        if (route == null || done) return;

        float step = Halted ? 0f : speed * Time.deltaTime;

        // Queue: never get closer than minGap to the gnome ahead (a halted gnome blocks everyone behind it).
        if (step > 0f)
        {
            var ahead = FindAhead();
            if (ahead != null)
            {
                float room = (ahead.Traveled - minGap) - Traveled;
                step = Mathf.Clamp(step, 0f, Mathf.Max(0f, room));
            }
        }

        SetMoving(step > 0.0001f);
        if (step > 0f) Move(step);

        // Danger check runs even while halted: a crouching gnome inside the zone still gets caught.
        if (!done && route.Monster != null && route.Monster.TryKill(transform.position))
            Finish(false);
    }

    void Move(float step)
    {
        Vector3 pos = transform.position;
        Vector3 wp = route.GetPoint(next);
        Vector3 target = new Vector3(wp.x + offset.x, wp.y + offset.y, pos.z);
        Vector3 delta = target - pos;
        float dist = delta.magnitude;

        if (dist <= step)
        {
            transform.position = target;
            Traveled += dist;
            next++;
            if (next >= route.Count) Finish(true);
            return;
        }

        transform.position = pos + delta / dist * step;
        Traveled += step;

        if (spriteRenderer != null && Mathf.Abs(delta.x) > 0.01f)
            spriteRenderer.flipX = artFacesRight ? delta.x < 0f : delta.x > 0f;
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