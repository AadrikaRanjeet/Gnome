using System.Collections.Generic;
using UnityEngine;

// FULL REPLACEMENT. Same as before, plus the "Traffic / Halt" settings.
public class GnomeSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] gnomePrefabs;
    [SerializeField] Transform gnomeParent;
    [SerializeField] PathRoute[] routes;               // 0 = Upper, 1 = Middle, 2 = Lower

    [Header("Stream")]
    [SerializeField] int totalGnomes = 100;
    [SerializeField] float spawnInterval = 0.7f;
    [SerializeField] float gnomeSpeed = 1.5f;          // gap between gnomes = speed x interval
    [SerializeField, Range(0f, 0.4f)] float speedVariance = 0f;
    [SerializeField] float positionJitter = 0.1f;

    [Header("Size")]
    [SerializeField] float targetHeight = 1.0f;        // 0 = off

    [Header("Sorting")]
    [SerializeField] int ySortBase = 5000;             // Canvas_Foreground must be ABOVE ~6000 (use 10000)

    [Header("Traffic / Halt")]
    [SerializeField] float minGap = 0.5f;              // closest a gnome can get to the one ahead. Keep BELOW speed x interval
    [SerializeField, Range(0.5f, 1f)] float crouchScaleY = 0.9f;
    [SerializeField] Color haltTint = new Color(0.72f, 0.86f, 1f, 1f);   // slight blue so halted gnomes are easy to spot

    public PathRoute[] Routes => routes;
    public int TotalGnomes => totalGnomes;
    public int SelectedRoute { get; private set; } = -1;
    public int Spawned { get; private set; }
    public int Saved { get; private set; }
    public int Dead { get; private set; }

    float acc;
    readonly List<int> bag = new List<int>();

    void Update()
    {
        if (SelectedRoute < 0 || Spawned >= totalGnomes) return;

        acc += Time.deltaTime;
        while (acc >= spawnInterval && Spawned < totalGnomes)
        {
            acc -= spawnInterval;
            Spawn();
        }
    }

    int NextPrefabIndex()
    {
        if (bag.Count == 0)
        {
            for (int i = 0; i < gnomePrefabs.Length; i++) bag.Add(i);
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }
        int idx = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return idx;
    }

    void NormalizeSize(GameObject go)
    {
        if (targetHeight <= 0f) return;
        var rs = go.GetComponentsInChildren<SpriteRenderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        if (b.size.y > 0.001f) go.transform.localScale *= targetHeight / b.size.y;
    }

    void Spawn()
    {
        if (gnomePrefabs == null || gnomePrefabs.Length == 0)
        {
            Debug.LogError("GnomeSpawner: no gnome prefabs assigned.");
            enabled = false;
            return;
        }

        var go = Instantiate(gnomePrefabs[NextPrefabIndex()], transform.position, Quaternion.identity, gnomeParent);
        NormalizeSize(go);

        var g = go.GetComponent<GnomeWalker>();
        if (g == null) g = go.AddComponent<GnomeWalker>();
        g.EnableYSort(ySortBase, 100f);
        g.SetTraffic(minGap, crouchScaleY, haltTint);

        float spd = gnomeSpeed * Random.Range(1f - speedVariance, 1f + speedVariance);
        g.Init(routes[SelectedRoute], spd, positionJitter);
        g.Arrived += _ => { Saved++; LogScore(); };
        g.Died += _ => { Dead++; LogScore(); };
        Spawned++;
    }

    void LogScore()
    {
        Debug.Log($"Saved {Saved}  |  Dead {Dead}  |  Spawned {Spawned}");
    }

    public void SelectRoute(int i)
    {
        if (i < 0 || i >= routes.Length || i == SelectedRoute) return;
        SelectedRoute = i;
    }

    public void Stop()
    {
        SelectedRoute = -1;
        acc = 0f;
    }
}