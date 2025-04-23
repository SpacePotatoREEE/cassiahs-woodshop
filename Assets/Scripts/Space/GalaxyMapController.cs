using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// • Opens/closes with M (pauses game)  
/// • Shows star‑systems ≤ revealRadius jumps from the player  
/// • Shift‑click plots fastest route and highlights the link Images
/// </summary>
public class GalaxyMapController : MonoBehaviour
{
    /* ───────────  Inspector  ─────────── */
    [Header("Assets & Prefabs")]
    [SerializeField] private GalaxyDatabase galaxyDatabase;
    [SerializeField] private NodeButtonUI   nodeButtonPrefab;       // coloured dots
    [SerializeField] private Image          uiLinePrefab;           // thin Image (Prefab_UI_NodeLine)

    [Header("UI References")]
    [SerializeField] private RectTransform mapContainer;
    [SerializeField] private CanvasGroup   rootCanvasGroup;
    [SerializeField] private TMP_Text      tooltipLabel;

    [Header("Map Settings")]
    [SerializeField] private float mapScale     = 30f;  // Unity‑units per mapPosition unit
    [SerializeField] private float nodeSize     = 12f;  // pixel diameter of dots
    [SerializeField] private int   revealRadius = 2;    // jumps player can “see”

    [Header("Line Colours")]
    [SerializeField] private Color normalLineColor      = new(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color highlightedLineColor = Color.cyan;

    /* ───────────  Runtime  ─────────── */
    private readonly Dictionary<StarSystemData, NodeButtonUI> nodeLookup = new();
    private readonly List<LinkInfo> linkInfos = new();

    private bool visible;
    private List<StarSystemData> activeRoute = null;

    /* ───────────  Internals  ─────────── */
    private class LinkInfo
    {
        public StarSystemData A;
        public StarSystemData B;
        public Image          img;
    }

    /* ═══════════  LIFECYCLE  ═══════════ */
    private void Awake()
    {
        if (!galaxyDatabase) { Debug.LogError("GalaxyDatabase missing.", this); enabled = false; return; }
        if (!nodeButtonPrefab || !uiLinePrefab) { Debug.LogError("Prefabs not assigned.", this); enabled = false; return; }

        BuildNodesAndLinks();
        HideInstant();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) ToggleVisible();
    }

    /* ═══════════  BUILD  ═══════════ */
    private void BuildNodesAndLinks()
    {
        // ── nodes ──────────────────────────────────────────
        foreach (StarSystemData sys in galaxyDatabase.allSystems)
        {
            NodeButtonUI btn = Instantiate(nodeButtonPrefab, mapContainer);
            btn.Init(sys, this, nodeSize);
            nodeLookup[sys] = btn;

            (btn.transform as RectTransform).anchoredPosition = sys.mapPosition * mapScale;
        }

        // ── links ─────────────────────────────────────────
        foreach (StarSystemData sys in galaxyDatabase.allSystems)
        {
            foreach (StarSystemData neigh in sys.neighborSystems)
            {
                if (!neigh) continue;
                if (sys.GetInstanceID() > neigh.GetInstanceID()) continue;   // avoid duplicate pair

                Image img = Instantiate(uiLinePrefab, mapContainer);
                img.color = normalLineColor;

                LinkInfo li = new() { A = sys, B = neigh, img = img };
                linkInfos.Add(li);

                PositionUiLine(li);
            }
        }
    }

    /* ═══════════  LINE POSITION  ═══════════ */
    private void PositionUiLine(LinkInfo li)
    {
        Vector2 a = li.A.mapPosition * mapScale;
        Vector2 b = li.B.mapPosition * mapScale;

        Vector2 dir   = b - a;
        float   len   = dir.magnitude;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        RectTransform rt = li.img.rectTransform;
        rt.sizeDelta        = new Vector2(len, 2f);              // 2‑px thickness
        rt.anchoredPosition = a;                                 // left end on node A
        rt.localRotation    = Quaternion.Euler(0, 0, angle);
    }

    /* ═══════════  NODE CLICK  ═══════════ */
    public void OnNodeClicked(StarSystemData sys)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (!shift) { CentreOnSystem(sys); return; }

        if (activeRoute != null && activeRoute[^1] == sys) ClearRoute();
        else                                               PlotRouteTo(sys);
    }

    /* ═══════════  ROUTE PLOTTING  ═══════════ */
    private void PlotRouteTo(StarSystemData target)
    {
        StarSystemData start = GameManager.Instance?.CurrentSystem;
        if (start == null) return;

        Dictionary<StarSystemData, StarSystemData> prev = BFS_GetPredecessors(start);
        if (!prev.ContainsKey(target))
        {
            Debug.LogWarning($"No path from {start.displayName} to {target.displayName}");
            return;
        }

        // reconstruct
        List<StarSystemData> path = new();
        for (StarSystemData p = target; p != null; p = prev[p]) path.Add(p);
        path.Reverse();

        activeRoute = path;
        HighlightRouteLines();
    }

    private Dictionary<StarSystemData, StarSystemData> BFS_GetPredecessors(StarSystemData start)
    {
        var pred = new Dictionary<StarSystemData, StarSystemData> { [start] = null };
        Queue<StarSystemData> q = new(); q.Enqueue(start);

        while (q.Count > 0)
        {
            StarSystemData s = q.Dequeue();
            foreach (StarSystemData n in s.neighborSystems)
            {
                if (!n || pred.ContainsKey(n)) continue;
                pred[n] = s; q.Enqueue(n);
            }
        }
        return pred;
    }

    private void ClearRoute()
    {
        activeRoute = null;
        foreach (var li in linkInfos) li.img.color = normalLineColor;
    }

    private void HighlightRouteLines()
    {
        foreach (var li in linkInfos) li.img.color = normalLineColor;
        if (activeRoute == null || activeRoute.Count < 2) return;

        for (int i = 0; i < activeRoute.Count - 1; i++)
        {
            StarSystemData a = activeRoute[i], b = activeRoute[i + 1];

            foreach (var li in linkInfos)
            {
                if ((li.A == a && li.B == b) || (li.A == b && li.B == a))
                    li.img.color = highlightedLineColor;
            }
        }
    }

    /* ═══════════  VIEW HELPERS  ═══════════ */
    private void CentreOnSystem(StarSystemData sys)
    {
        if (nodeLookup.TryGetValue(sys, out var btn))
            mapContainer.anchoredPosition = -(btn.transform as RectTransform).anchoredPosition;
    }

    /* ═══════════  PANEL & VISIBILITY  ═══════════ */
    private void ToggleVisible() { if (visible) Close(); else Open(); }

    private void Open()
    {
        visible = true;
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.blocksRaycasts = rootCanvasGroup.interactable = true;
        RefreshVisibility();
        GameManager.Instance?.PauseGame();
    }
    private void Close()
    {
        visible = false;
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.blocksRaycasts = rootCanvasGroup.interactable = false;
        GameManager.Instance?.ResumeGame();
    }
    private void HideInstant()
    {
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.blocksRaycasts = rootCanvasGroup.interactable = false;
        visible = false;
    }

    private void RefreshVisibility()
    {
        StarSystemData current = GameManager.Instance?.CurrentSystem;
        if (current == null) return;

        Dictionary<StarSystemData, int> vis = BFS_Distances(current, revealRadius);

        foreach (var kvp in nodeLookup)
            kvp.Value.gameObject.SetActive(vis.ContainsKey(kvp.Key));

        foreach (var li in linkInfos)
        {
            bool show = vis.ContainsKey(li.A) && vis.ContainsKey(li.B);
            li.img.gameObject.SetActive(show);
            if (show) PositionUiLine(li);
        }
    }
    
    public void RemoveFirstHopFromActiveRoute()
    {
        var fi = typeof(GalaxyMapController).GetField("activeRoute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = fi?.GetValue(this) as List<StarSystemData>;
        if (list is { Count: > 0 })
        {
            list.RemoveAt(0);          // drop the completed system
            HighlightRouteLines();     // refresh colours
        }
    }

    private Dictionary<StarSystemData, int> BFS_Distances(StarSystemData start, int maxDepth)
    {
        var d = new Dictionary<StarSystemData, int> { [start] = 0 };
        Queue<StarSystemData> q = new(); q.Enqueue(start);

        while (q.Count > 0)
        {
            StarSystemData s = q.Dequeue();
            int dep = d[s]; if (dep >= maxDepth) continue;

            foreach (StarSystemData n in s.neighborSystems)
            {
                if (!n || d.ContainsKey(n)) continue;
                d[n] = dep + 1; q.Enqueue(n);
            }
        }
        return d;
    }

    /* ═══════════  TOOLTIP  ═══════════ */
    public void ShowTooltip(string txt) { if (tooltipLabel) tooltipLabel.text = txt; }
    public void HideTooltip()           { if (tooltipLabel) tooltipLabel.text = "";  }
}
