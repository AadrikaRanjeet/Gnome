using UnityEngine;
using UnityEngine.SceneManagement;

// NEW. Put on an empty object called "RoundManager".
// Runs the clock and decides when the round ends:
//   - time runs out, OR
//   - every gnome has been resolved (saved or dead)
// Score = gnomes saved. Win = saved >= targetScore.
// Gnomes that haven't reached a cave when the round ends (still walking, or never sent) do NOT count.
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [SerializeField] GnomeSpawner spawner;
    [SerializeField] float timeLimit = 60f;
    [SerializeField] int targetScore = 40;
    [Tooltip("ON: the clock starts when you send the first gnome (you get time to watch the caves first).\nOFF: the clock starts as soon as you press Play.")]
    [SerializeField] bool startClockOnFirstGnome = true;

    public float TimeRemaining { get; private set; }
    public float TimeLimit => timeLimit;
    public bool ClockRunning { get; private set; }
    public bool IsPlaying { get; private set; } = true;
    public bool Won { get; private set; }
    public bool TimedOut { get; private set; }

    public int Saved => spawner.Saved;
    public int Dead => spawner.Dead;
    public int Spawned => spawner.Spawned;
    public int TotalGnomes => spawner.TotalGnomes;
    public int ToSend => spawner.TotalGnomes - spawner.Spawned;
    public int Unresolved => spawner.TotalGnomes - spawner.Saved - spawner.Dead;
    public int TargetScore => targetScore;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        TimeRemaining = timeLimit;
        ClockRunning = !startClockOnFirstGnome;
    }

    void Update()
    {
        if (!IsPlaying) return;

        if (!ClockRunning && spawner.Spawned > 0) ClockRunning = true;

        if (ClockRunning)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                TimedOut = true;
                EndRound();
                return;
            }
        }

        if (spawner.Saved + spawner.Dead >= spawner.TotalGnomes) EndRound();
    }

    void EndRound()
    {
        IsPlaying = false;
        Won = spawner.Saved >= targetScore;
        spawner.Stop();
        Time.timeScale = 0f;     // freezes gnomes, monsters and animations
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}