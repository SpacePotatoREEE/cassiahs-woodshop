using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Opens with M, Shift-click plots routes, click pins info in bottom-right.
public class GalaxyMapController : MonoBehaviour
{
    /* ---------- Inspector ---------- */
    [Header("Assets & Prefabs")]
    [SerializeField] GalaxyDatabase galaxyDatabase;
    [SerializeField] NodeButtonUI   nodePrefab;
    [SerializeField] Image          linePrefab;

    [Header("UI References")]
    [SerializeField] RectTransform  mapContainer;   // holds dots + lines
    [SerializeField] RectTransform  mapViewport;    // full map panel
    [SerializeField] CanvasGroup    canvasGroup;    // root CanvasGroup
    [SerializeField] TMP_Text       tooltipLabel;
    [SerializeField] CanvasGroup    legendGroup;

    [Header("Info Panel")]
    [SerializeField] RectTransform  infoPanel;      // child already in scene
    [SerializeField] Sprite         defaultSprite;
    [SerializeField] Color          selectOutline  = Color.yellow;
    [SerializeField] float          outlineWidth   = 3f;

    [Header("Map Settings")]
    [SerializeField] float mapScale  = 30f;
    [SerializeField] float nodeSize  = 12f;
    [SerializeField] int   revealRad = 2;

    /* ---------- Runtime ---------- */
    readonly Dictionary<StarSystemData, NodeButtonUI> nodes = new();
    readonly List<(StarSystemData,StarSystemData,Image)> links = new();

    List<StarSystemData> activeRoute = null;
    StarSystemData  pinnedSystem;
    NodeButtonUI    pinnedNode;
    
    [Header("Line Colours")]
    [SerializeField] Color normalLineColor      = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] Color highlightedLineColor = Color.cyan;

    /* info-panel sub-refs */
    Image   infoIcon, factionDot;
    TMP_Text nameLabel, descLabel;

    bool visible;
    public bool IsVisible => visible;

    /* ---------- Unity ---------- */
    void Awake()
    {
        if (!galaxyDatabase || !nodePrefab || !linePrefab || !infoPanel)
        { Debug.LogError("GalaxyMap: missing refs", this); enabled=false; return; }

        CacheInfoPanelRefs();
        BuildMap();
        HideInstant();
    }

    void Start()
    {
        // GameManager is definitely initialised now
        if (GameManager.Instance)
            foreach (var s in galaxyDatabase.allSystems)
                if (s.discoveredAtStart)
                    GameManager.Instance.AddDiscoveredSystem(s);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Toggle();
    }

    /* ---------- Build ---------- */
    void BuildMap()
    {
        foreach (var s in galaxyDatabase.allSystems)
        {
            var btn = Instantiate(nodePrefab, mapContainer);
            btn.Init(s, this, nodeSize);
            
            ((RectTransform)btn.transform).anchoredPosition = s.mapPosition * mapScale;
            nodes[s] = btn;
            
            btn.ShowCurrentMarker( GameManager.Instance &&
                                   GameManager.Instance.CurrentSystem == s );
        }

        foreach (var a in galaxyDatabase.allSystems)
            foreach (var b in a.neighborSystems)
                if (b && a.GetInstanceID() < b.GetInstanceID())
                {
                    var img = Instantiate(linePrefab, mapContainer);
                    links.Add((a,b,img));
                    PositionLine(a,b,img);
                    img.transform.SetAsLastSibling();
                }
    }

    void CacheInfoPanelRefs()
    {
        infoIcon   = infoPanel.Find("IconImage").GetComponent<Image>();
        factionDot = infoPanel.Find("FactionDot").GetComponent<Image>();
        nameLabel = new GameObject("NameLabel", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        descLabel  = infoPanel.Find("FactionLabel").GetComponent<TMP_Text>();

        infoPanel.gameObject.SetActive(false);
    }

    /* ---------- Pointer handlers ---------- */
    public void ShowHover(StarSystemData s)
    {
        if (pinnedSystem != null) return;   // ignore if pinned
        FillPanel(s); infoPanel.gameObject.SetActive(true);
    }
    public void HideHover()
    {
        if (pinnedSystem != null) return;
        infoPanel.gameObject.SetActive(false);
    }

    public void OnNodeClicked(StarSystemData s, NodeButtonUI btn)
    {
        if (Input.GetKey(KeyCode.LeftShift)|Input.GetKey(KeyCode.RightShift))
        {
            if (activeRoute!=null && activeRoute[^1]==s) ClearRoute();
            else PlotRouteTo(s);
            return;
        }

        if (pinnedSystem == s) { Unpin(); return; }
        Pin(s, btn);
    }

    void Pin(StarSystemData s, NodeButtonUI btn)
    {
        Unpin();
        pinnedSystem = s; pinnedNode = btn;
        btn.SetOutline(selectOutline, outlineWidth);
        FillPanel(s);
        infoPanel.gameObject.SetActive(true);
    }
    void Unpin()
    {
        if (pinnedNode) pinnedNode.ClearOutline();
        pinnedSystem = null; pinnedNode = null;
        infoPanel.gameObject.SetActive(false);
    }

    /* ---------- Info content ---------- */
    void FillPanel(StarSystemData s)
    {
        infoIcon.sprite = s.previewSprite ? s.previewSprite : defaultSprite;
        factionDot.color= s.ownerFaction.ToColor();
        nameLabel.text  = s.displayName;
        descLabel.text  = s.description;
    }

    /* ---------- Lines ---------- */
    void PositionLine(StarSystemData a, StarSystemData b, Image img)
    {
        Vector2 pa = a.mapPosition*mapScale, pb = b.mapPosition*mapScale;
        Vector2 d  = pb-pa;
        var rt = img.rectTransform;
        rt.sizeDelta        = new Vector2(d.magnitude,2);
        rt.anchoredPosition = pa;
        rt.localRotation    = Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
    }

    /* ---------- Routes ---------- */
    void PlotRouteTo(StarSystemData target)
    {
        var cur = GameManager.Instance?.CurrentSystem; if (cur==null) return;
        var prev = BFS(cur);
        if (!prev.ContainsKey(target)) return;
        activeRoute = ReconstructPath(prev,target);
        ColourLines();
    }
    void ClearRoute(){ activeRoute=null; ColourLines(); }

    void ColourLines()
    {
        foreach (var (a,b,img) in links)
            img.color = (activeRoute!=null && IsOnRoute(a,b)) ? highlightedLineColor : normalLineColor;
    }
    bool IsOnRoute(StarSystemData a, StarSystemData b)
    {
        for (int i=0;i<activeRoute.Count-1;i++)
            if ((activeRoute[i]==a && activeRoute[i+1]==b) ||
                (activeRoute[i]==b && activeRoute[i+1]==a)) return true;
        return false;
    }

    /* ---------- Visibility ---------- */
    void Toggle(){ if(visible) Close(); else Open(); }
    void Open()
    {
        visible=true; canvasGroup.alpha=1; canvasGroup.blocksRaycasts=true;
        legendGroup.alpha=1; legendGroup.blocksRaycasts=true;
        RefreshVisibility();
        GameManager.Instance?.PauseGame();
    }
    void Close()
    {
        visible=false; canvasGroup.alpha=0; canvasGroup.blocksRaycasts=false;
        legendGroup.alpha=0; legendGroup.blocksRaycasts=false;
        GameManager.Instance?.ResumeGame();
    }
    
    public void ClearSelection()
    {
        Unpin();
        //ClearRoute();          // hides highlighted lines
    }
    
    void HideInstant(){ canvasGroup.alpha=0; canvasGroup.blocksRaycasts=false; }

    void RefreshVisibility()
    {
        /* current system & what the player can see right now */
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.CurrentSystem == null) return;

        var cur = gm.CurrentSystem;
        var vis = BFS_Distance(cur, revealRad);

        /* 1) anything within reveal radius is now discovered forever */
        foreach (var sys in vis.Keys)
            gm.AddDiscoveredSystem(sys);

        /* 2) show nodes that are either visible *or* already discovered */
        foreach (var kv in nodes)
            kv.Value.gameObject.SetActive(
                gm.IsSystemDiscovered(kv.Key) || vis.ContainsKey(kv.Key));

        /* 3) show links only when BOTH ends are visible or discovered */
        foreach (var (a, b, img) in links)
            img.gameObject.SetActive(
                (gm.IsSystemDiscovered(a) || vis.ContainsKey(a)) &&
                (gm.IsSystemDiscovered(b) || vis.ContainsKey(b)));

        /* 4) green “you-are-here” pip */
        foreach (var kv in nodes)
            kv.Value.ShowCurrentMarker(cur == kv.Key);
    }
    
    /* ══════════  Trim the plotted route after a completed jump  ══════════ */
    public void RemoveFirstHopFromActiveRoute()
    {
        if (activeRoute == null || activeRoute.Count == 0)
            return;

        // drop the system the player just left
        activeRoute.RemoveAt(0);

        // refresh line colours so the highlight starts at the new current node
        ColourLines();

        // if we had pinned a node that is no longer in the route, unpin it
        if (pinnedSystem != null && !activeRoute.Contains(pinnedSystem))
            Unpin();
    }

    /* ---------- Helpers ---------- */
    Dictionary<StarSystemData,StarSystemData> BFS(StarSystemData start)
    {
        var p=new Dictionary<StarSystemData,StarSystemData>{{start,null}};
        var q=new Queue<StarSystemData>(); q.Enqueue(start);
        while(q.Count>0)
        { var s=q.Dequeue();
          foreach(var n in s.neighborSystems) if(n && !p.ContainsKey(n)){p[n]=s;q.Enqueue(n);} }
        return p;
    }
    List<StarSystemData> ReconstructPath(Dictionary<StarSystemData,StarSystemData> prev, StarSystemData tgt)
    {
        var l=new List<StarSystemData>(); for(var v=tgt;v!=null;v=prev[v]) l.Add(v); l.Reverse(); return l;
    }
    Dictionary<StarSystemData,int> BFS_Distance(StarSystemData s,int max)
    {
        var d=new Dictionary<StarSystemData,int>{{s,0}};
        var q=new Queue<StarSystemData>(); q.Enqueue(s);
        while(q.Count>0)
        { var v=q.Dequeue(); int dep=d[v]; if(dep>=max) continue;
          foreach(var n in v.neighborSystems) if(n && !d.ContainsKey(n)){d[n]=dep+1;q.Enqueue(n);} }
        return d;
    }
}
