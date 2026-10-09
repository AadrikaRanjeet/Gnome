using UnityEngine;

public enum MonsterState { Hidden, Warning, Out }

// UPDATED. Stays on the "Monster" empty object of each route.
//
// Cycle:
//   Hidden  : everything off
//   Warning : flame blinks (monster still in the cave)
//   Out     : flame solid + DEADLY. Monster walks forward, waits (idle), then walks back.
//             If a gnome gets caught, the monster stops and ATTACKS, then carries on.
//             No gnome = no attack: it just walks out, idles and goes back.
//   (the flame goes off when the monster starts walking back = safe again)
public class Monster : MonoBehaviour
{
    enum OutPhase { WalkOut, Linger, WalkBack }

    [Header("Visuals")]
    [SerializeField] GameObject flameRoot;
    [SerializeField] GameObject monsterRoot;      // keep DISABLED in the scene
    [SerializeField] Transform forwardPoint;      // where the monster stops after walking out
    [SerializeField] Animator animator;
    [SerializeField] bool flipWhenWalkingBack = true;

    [Header("Animator parameter names (exact, case-sensitive). Leave empty to skip.")]
    [SerializeField] string walkParam = "Walk";       // bool
    [SerializeField] string idleParam = "Idle";       // bool
    [SerializeField] string attackParam = "Attack";   // trigger

    [Header("Danger zone")]
    [SerializeField] Transform dangerCenter;      // empty child: red circle follows it (empty = this object)
    [SerializeField] float dangerRadius = 1.2f;

    [Header("Timing (seconds). X = minimum, Y = maximum: a random value in between is picked.")]
    [SerializeField] float startDelay = 0f;
    [SerializeField] Vector2 hiddenTime = new Vector2(2.5f, 4.5f);
    [SerializeField] float warningTime = 1.0f;
    [SerializeField] float warningBlink = 0.15f;
    [SerializeField] float walkOutTime = 0.6f;
    [SerializeField] Vector2 lingerTime = new Vector2(0.8f, 1.5f);
    [SerializeField] float walkBackTime = 0.7f;
    [SerializeField] float attackTime = 0.8f;     // how long the monster freezes to attack: match your attack clip

    [Header("Debug (read only)")]
    [SerializeField] MonsterState currentState;

    public MonsterState State { get; private set; } = MonsterState.Hidden;
    public bool IsOut => State == MonsterState.Out;

    OutPhase phase;
    float timer, phaseDuration, blinkTimer, attackTimer;
    bool attacking;
    Vector3 startPos;
    Vector3 originalScale = Vector3.one;

    Vector3 DangerPos => dangerCenter != null ? dangerCenter.position : transform.position;

    void Start()
    {
        if (monsterRoot != null)
        {
            startPos = monsterRoot.transform.position;
            originalScale = monsterRoot.transform.localScale;
            if (animator == null) animator = monsterRoot.GetComponentInChildren<Animator>(true);
        }
        EnterHidden(Random.Range(hiddenTime.x, hiddenTime.y) + startDelay);
    }

    void Update()
    {
        // Attack freezes everything (movement + phase timer) until the swing is done.
        if (attacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) { attacking = false; PlayPhaseAnim(); }
            return;
        }

        timer -= Time.deltaTime;

        if (State == MonsterState.Warning && flameRoot != null)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkTimer = warningBlink;
                flameRoot.SetActive(!flameRoot.activeSelf);
            }
        }

        if (State == MonsterState.Out) MoveMonster();

        if (timer > 0f) return;

        switch (State)
        {
            case MonsterState.Hidden: EnterWarning(); break;
            case MonsterState.Warning: EnterOut(); break;
            case MonsterState.Out: NextPhase(); break;
        }
    }

    // ---------- states ----------

    void EnterHidden(float duration)
    {
        SetState(MonsterState.Hidden, duration);
        attacking = false;
        if (flameRoot != null) flameRoot.SetActive(false);
        if (monsterRoot != null)
        {
            monsterRoot.SetActive(false);
            monsterRoot.transform.position = startPos;
            monsterRoot.transform.localScale = originalScale;
        }
        if (animator != null && !string.IsNullOrEmpty(attackParam)) animator.ResetTrigger(attackParam);
    }

    void EnterWarning()
    {
        SetState(MonsterState.Warning, warningTime);
        blinkTimer = 0f;
        if (flameRoot != null) flameRoot.SetActive(false);
    }

    void EnterOut()
    {
        SetState(MonsterState.Out, walkOutTime);
        attacking = false;
        if (flameRoot != null) flameRoot.SetActive(true);
        if (monsterRoot != null)
        {
            monsterRoot.transform.position = startPos;
            monsterRoot.transform.localScale = originalScale;
            monsterRoot.SetActive(true);
        }
        if (animator != null && !string.IsNullOrEmpty(attackParam)) animator.ResetTrigger(attackParam);
        StartPhase(OutPhase.WalkOut, walkOutTime);
    }

    void NextPhase()
    {
        switch (phase)
        {
            case OutPhase.WalkOut: StartPhase(OutPhase.Linger, Random.Range(lingerTime.x, lingerTime.y)); break;
            case OutPhase.Linger: StartPhase(OutPhase.WalkBack, walkBackTime); break;
            case OutPhase.WalkBack: EnterHidden(Random.Range(hiddenTime.x, hiddenTime.y)); break;
        }
    }

    void StartPhase(OutPhase p, float duration)
    {
        phase = p;
        phaseDuration = Mathf.Max(0.01f, duration);
        timer = phaseDuration;
        PlayPhaseAnim();

        if (p == OutPhase.WalkBack)
        {
            if (flameRoot != null) flameRoot.SetActive(false);    // danger is over, monster is leaving
            if (flipWhenWalkingBack && monsterRoot != null)
            {
                var s = originalScale;
                monsterRoot.transform.localScale = new Vector3(-s.x, s.y, s.z);
            }
        }
    }

    void SetState(MonsterState s, float duration)
    {
        State = s;
        currentState = s;
        timer = duration;
    }

    // ---------- animation ----------

    void PlayPhaseAnim()
    {
        switch (phase)
        {
            case OutPhase.WalkOut:
            case OutPhase.WalkBack: SetAnim(walk: true, idle: false); break;
            case OutPhase.Linger: SetAnim(walk: false, idle: true); break;
        }
    }

    void SetAnim(bool walk, bool idle)
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(walkParam)) animator.SetBool(walkParam, walk);
        if (!string.IsNullOrEmpty(idleParam)) animator.SetBool(idleParam, idle);
    }

    void BeginAttack()
    {
        attacking = true;
        attackTimer = attackTime;
        SetAnim(walk: false, idle: false);
        if (animator != null && !string.IsNullOrEmpty(attackParam)) animator.SetTrigger(attackParam);
    }

    // ---------- movement ----------

    void MoveMonster()
    {
        if (monsterRoot == null || forwardPoint == null) return;

        float t = 1f - Mathf.Clamp01(timer / phaseDuration);
        t = t * t * (3f - 2f * t);
        Vector3 fwd = forwardPoint.position;
        Vector3 pos;

        switch (phase)
        {
            case OutPhase.WalkOut: pos = Vector3.Lerp(startPos, fwd, t); break;
            case OutPhase.WalkBack: pos = Vector3.Lerp(fwd, startPos, t); break;
            default: pos = fwd; break;
        }
        pos.z = startPos.z;
        monsterRoot.transform.position = pos;
    }

    // ---------- danger ----------

    // Deadly from walk-out until the monster starts walking back, inside the red circle only.
    public bool IsDeadly(Vector2 worldPos)
    {
        if (State != MonsterState.Out || phase == OutPhase.WalkBack) return false;
        return ((Vector2)DangerPos - worldPos).sqrMagnitude <= dangerRadius * dangerRadius;
    }

    // Called by a gnome every frame. If it is caught, the monster reacts with an attack.
    public bool TryKill(Vector2 worldPos)
    {
        if (!IsDeadly(worldPos)) return false;
        if (!attacking) BeginAttack();
        return true;
    }

    void OnDrawGizmos()
    {
        bool deadly = Application.isPlaying && State == MonsterState.Out && phase != OutPhase.WalkBack;
        Gizmos.color = deadly ? new Color(1f, 0.1f, 0.1f, 1f) : new Color(1f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawWireSphere(DangerPos, dangerRadius);

        if (forwardPoint != null && monsterRoot != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 from = Application.isPlaying ? startPos : monsterRoot.transform.position;
            Gizmos.DrawLine(from, forwardPoint.position);
            Gizmos.DrawWireSphere(forwardPoint.position, 0.12f);
        }
    }
}