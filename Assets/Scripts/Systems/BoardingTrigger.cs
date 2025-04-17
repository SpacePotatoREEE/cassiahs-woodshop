using UnityEngine;

[RequireComponent(typeof(EnemySpaceShip))]
public class BoardingTrigger : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private GameObject boardingPanelPrefab;
    [SerializeField] private GameObject boardPrompt;

    private GameObject panelInstance;
    private EnemySpaceShip ship;
    private bool inRange;
    private bool panelInit;
    private bool worldPaused;

    private int layerShip;
    private int layerHuman;

    private void Awake()
    {
        ship       = GetComponent<EnemySpaceShip>();
        layerShip  = LayerMask.NameToLayer("PlayerShip");
        layerHuman = LayerMask.NameToLayer("PlayerHuman");

        if (boardPrompt) boardPrompt.SetActive(false);
    }

    /* ───────── trigger events ───────── */
    private void OnTriggerEnter(Collider other)
    {
        if (!ship.isDisabled) return;
        if (!IsPlayer(other.gameObject.layer)) return;

        inRange = true;
        if (boardPrompt) boardPrompt.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other.gameObject.layer)) return;

        inRange = false;
        if (boardPrompt) boardPrompt.SetActive(false);
        HideBoardingPanel();
    }

    private void Update()
    {
        if (inRange && Input.GetKeyDown(KeyCode.L))
            ShowBoardingPanel();
    }

    /* ───────── show / hide panel ───────── */
    private void ShowBoardingPanel()
    {
        if (boardPrompt) boardPrompt.SetActive(false);

        // Spawn the UI prefab the first time
        if (panelInstance == null)
        {
            if (!boardingPanelPrefab)
            {
                Debug.LogWarning("BoardingTrigger: prefab missing.");
                return;
            }

            panelInstance = Instantiate(boardingPanelPrefab);
            BoardingPanelUI ui = panelInstance.GetComponent<BoardingPanelUI>();

            // pass THIS trigger’s HideBoardingPanel so the Leave button
            // can close the panel and resume the game
            if (ui) ui.Init(ship, HideBoardingPanel);

            panelInit = true;
        }
        else if (!panelInit)
        {
            // in case Instance existed but never initialised
            BoardingPanelUI ui = panelInstance.GetComponent<BoardingPanelUI>();
            if (ui) ui.Init(ship, HideBoardingPanel);
            panelInit = true;
        }

        panelInstance.SetActive(true);
        GameManager.Instance.PauseGame();   // <── use GM
    }

    private void HideBoardingPanel()
    {
        if (panelInstance) panelInstance.SetActive(false);
        GameManager.Instance.ResumeGame();  // <── use GM
    }


    /* ───────── layer helper ───────── */
    private bool IsPlayer(int layer) => layer == layerShip || layer == layerHuman;
}
