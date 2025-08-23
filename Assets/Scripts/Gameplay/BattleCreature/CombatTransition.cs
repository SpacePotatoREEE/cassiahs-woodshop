using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatTransition : MonoBehaviour
{
    public static CombatTransition Instance { get; private set; }

    [Header("Fade Canvas (full-screen Image + CanvasGroup)")]
    public CanvasGroup fadeCanvas;
    public float       fadeDuration = 0.6f;

    [Header("Zoom-FOV curve (ghost effect)")]
    public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0, 60, 0.6f, 30);

    private Camera mainCam;

    /* ───────────────────────────────────────── */

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        mainCam = Camera.main;
    }

    public void Begin(string battleScene, BattleContext ctx)
    {
        StartCoroutine(Sequence(battleScene, ctx));
    }

    /* ───────────────────────────────────────── */

    private IEnumerator Sequence(string battleScene, BattleContext ctx)
    {
        fadeCanvas.gameObject.SetActive(true);

        /* Pre-zoom & fade */
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float n = t / fadeDuration;

            if (mainCam) mainCam.fieldOfView = fovCurve.Evaluate(n);
            fadeCanvas.alpha = n * 0.3f;      // bright flash

            yield return null;
        }
        fadeCanvas.alpha = 1f;

        /* Load battle scene */
        yield return SceneManager.LoadSceneAsync(battleScene, LoadSceneMode.Single);

        mainCam = Camera.main;                // scene has new camera

        /* Optional Timeline play */
        var dir = FindObjectOfType<UnityEngine.Playables.PlayableDirector>();
        if (dir) dir.Play();

        /* Fade back in */
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
        fadeCanvas.gameObject.SetActive(false);
    }
}
