using System.Collections.Generic;
using UnityEngine;

// UPDATED: spawns a MIX of different gnome prefabs instead of just one.
// Uses a "shuffle bag": every gnome prefab is used once (in random order) before any repeats,
// so with 20 prefabs and 100 gnomes each one shows up about 5 times.
public class GnomeSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] gnomePrefabs;        // drag ALL your gnome prefabs here
    [SerializeField] Transform gnomeParent;            // the "Gnomes" container object
    [SerializeField] PathRoute[] routes;               // 0 = Upper, 1 = Middle, 2 = Lower

    [Header("Stream")]
    [SerializeField] int totalGnomes = 100;            // set any number: 5, 30, 100...
    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] float gnomeSpeed = 2.5f;
    [SerializeField, Range(0f, 0.4f)] float speedVariance = 0.1f;
    [SerializeField] float positionJitter = 0.08f;

    [Header("Sorting")]
    [SerializeField] bool overrideSortingOrder = true; // untick if a gnome has several layered sprite parts
    [SerializeField] int gnomeSortingOrder = 5;        // above background (0), below foreground (10)

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
            for (int i = bag.Count - 1; i > 0; i--)            // shuffle
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }
        int idx = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        return idx;
    }

    void Spawn()
    {
        if (gnomePrefabs == null || gnomePrefabs.Length == 0)
        {
            Debug.LogError("GnomeSpawner: no gnome prefabs assigned.");
            enabled = false;
            return;
        }

        var prefab = gnomePrefabs[NextPrefabIndex()];
        var go = Instantiate(prefab, transform.position, Quaternion.identity, gnomeParent);

        if (overrideSortingOrder)
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder = gnomeSortingOrder;

        // Prefabs don't need GnomeWalker added by hand: we add it here if it's missing.
        var g = go.GetComponent<GnomeWalker>();
        if (g == null) g = go.AddComponent<GnomeWalker>();

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