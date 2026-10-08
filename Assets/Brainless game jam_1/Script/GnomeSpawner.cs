using UnityEngine;

// Attach to the "GnomeSpawn" empty object (positioned BEHIND the foreground tree).
// Owns "which path is selected" and spawns gnomes at its own position while a path is selected.
public class GnomeSpawner : MonoBehaviour
{
    [SerializeField] GnomeWalker gnomePrefab;
    [SerializeField] Transform gnomeParent;            // the "Gnomes" container object
    [SerializeField] PathRoute[] routes;               // 0 = Upper, 1 = Middle, 2 = Lower

    [Header("Stream")]
    [SerializeField] int totalGnomes = 100;
    [SerializeField] float spawnInterval = 0.5f;       // seconds between gnomes
    [SerializeField] float gnomeSpeed = 2.5f;          // world units / second
    [SerializeField, Range(0f, 0.4f)] float speedVariance = 0.1f;
    [SerializeField] float positionJitter = 0.08f;

    public PathRoute[] Routes => routes;
    public int SelectedRoute { get; private set; } = -1;   // -1 = stopped
    public int Spawned { get; private set; }
    public int Arrived { get; private set; }

    float acc;

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

    void Spawn()
    {
        var g = Instantiate(gnomePrefab, transform.position, Quaternion.identity, gnomeParent);
        float spd = gnomeSpeed * Random.Range(1f - speedVariance, 1f + speedVariance);
        g.Init(routes[SelectedRoute], spd, positionJitter);
        g.Arrived += OnGnomeArrived;
        Spawned++;
    }

    void OnGnomeArrived(GnomeWalker g)
    {
        Arrived++;
        Debug.Log($"Arrived {Arrived} / spawned {Spawned}");   // placeholder until we add saved/dead
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