using TMPro;
using UnityEngine;
using UnityEngine.UI;

// NEW. Replaces RoundHUD (the code-drawn placeholder).
// Put it on the SAME object as RoundManager, then drag your own UI objects into the slots.
// It only fills in text / colours / visibility. You control how everything LOOKS in the editor.
public class Roundhud : MonoBehaviour
{
    [SerializeField] RoundManager round;

    [Header("In-game HUD")]
    [SerializeField] TMP_Text timerText;           // "0:42"
    [SerializeField] TMP_Text savedText;           // "SAVED 12 / 40"
    [SerializeField] TMP_Text lostText;            // "LOST 3"
    [SerializeField] TMP_Text toSendText;          // "TO SEND 45"
    [SerializeField] GameObject startHint;         // optional: "Send your first gnome to start the clock"

    [Header("Colours")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color urgentColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] Color reachedColor = new Color(0.45f, 1f, 0.5f);
    [SerializeField] float urgentSeconds = 10f;    // timer turns urgent + pulses below this

    [Header("Result screen")]
    [SerializeField] GameObject resultPanel;       // whole panel, hidden until the round ends
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text detailText;
    [SerializeField] Button retryButton;

    bool resultShown;

    void Reset() { round = GetComponent<RoundManager>(); }

    void Start()
    {
        if (round == null) round = GetComponent<RoundManager>();
        if (resultPanel != null) resultPanel.SetActive(false);
        if (retryButton != null) retryButton.onClick.AddListener(round.Restart);
    }

    void Update()
    {
        if (round == null) return;

        // timer
        if (timerText != null)
        {
            int secs = Mathf.CeilToInt(round.TimeRemaining);
            timerText.text = (secs / 60) + ":" + (secs % 60).ToString("00");

            bool urgent = round.ClockRunning && round.IsPlaying && secs <= urgentSeconds;
            timerText.color = urgent
                ? Color.Lerp(normalColor, urgentColor, 0.5f + 0.5f * Mathf.PingPong(Time.unscaledTime * 3f, 1f))
                : normalColor;
        }

        // score
        if (savedText != null)
        {
            savedText.text = "SAVED  " + round.Saved + " / " + round.TargetScore;
            savedText.color = round.Saved >= round.TargetScore ? reachedColor : normalColor;
        }
        if (lostText != null)
        {
            lostText.text = "LOST  " + round.Dead;
            lostText.color = round.Dead > 0 ? urgentColor : normalColor;
        }
        if (toSendText != null) toSendText.text = "TO SEND  " + round.ToSend;

        if (startHint != null) startHint.SetActive(round.IsPlaying && !round.ClockRunning);

        // result screen (shown once)
        if (!round.IsPlaying && !resultShown)
        {
            resultShown = true;
            if (resultPanel != null) resultPanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = round.Won ? "LEVEL COMPLETE" : (round.TimedOut ? "TIME'S UP" : "TOO MANY LOST");
                titleText.color = round.Won ? reachedColor : urgentColor;
            }
            if (detailText != null)
            {
                string d = "Saved  " + round.Saved + "   (target " + round.TargetScore + ")\n" +
                           "Lost  " + round.Dead;
                if (round.Unresolved > 0) d += "\nDidn't make it  " + round.Unresolved;
                detailText.text = d;
            }
        }
    }
}