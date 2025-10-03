using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class CompanionManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The companion prefab should include CompanionBrain + CompanionWeapon + a projectile under the weapon.")]
    [SerializeField] private CompanionBrain companionPrefab;

    [Tooltip("Start with this many companions (0..MaxSlots).")]
    [Range(0, 5)] [SerializeField] private int startingCompanions = 1;

    [Tooltip("How far the 5 slot anchors are from the player (radius).")]
    [SerializeField] private float slotRadius = 2.5f;

    [Tooltip("Height offset for slot anchors (useful for top-down characters to keep companions at shoulder height).")]
    [SerializeField] private float slotHeight = 0.75f;

    [Header("Runtime Info")]
    [SerializeField] private Transform slotsRoot;

    public const int MaxSlots = 5;

    private readonly List<Transform> _slotAnchors = new();
    private readonly List<CompanionBrain> _companions = new();

    public int CurrentCount => _companions.Count;

    private void Reset()
    {
        // Try to create a slots root automatically
        var go = new GameObject("CompanionSlots");
        go.transform.SetParent(transform, false);
        slotsRoot = go.transform;
    }

    private void Awake()
    {
        if (!slotsRoot)
        {
            var go = new GameObject("CompanionSlots");
            go.transform.SetParent(transform, false);
            slotsRoot = go.transform;
        }

        BuildSlotAnchors();
    }

    private void Start()
    {
        SetCompanionCount(startingCompanions);
    }

    private void Update()
    {
        UpdateSlotAnchorPositions();
    }

    private void BuildSlotAnchors()
    {
        _slotAnchors.Clear();
        for (int i = 0; i < MaxSlots; i++)
        {
            var anchor = new GameObject($"SlotAnchor_{i}").transform;
            anchor.SetParent(slotsRoot, false);
            _slotAnchors.Add(anchor);
        }
        UpdateSlotAnchorPositions(force: true);
    }

    private void UpdateSlotAnchorPositions(bool force = false)
    {
        // Arrange five equidistant points on a circle (static relative to player forward)
        // You can rotate this ring slowly or align to input if desired.
        float step = Mathf.PI * 2f / MaxSlots;
        for (int i = 0; i < MaxSlots; i++)
        {
            float ang = step * i;
            Vector3 local = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * slotRadius;
            local.y = slotHeight;
            _slotAnchors[i].localPosition = local;
            // Optionally rotate with camera: set localPosition in a rotated space if needed.
        }
    }

    /// <summary>Increase or decrease companions to exactly targetCount.</summary>
    public void SetCompanionCount(int targetCount)
    {
        targetCount = Mathf.Clamp(targetCount, 0, MaxSlots);

        // Add
        while (_companions.Count < targetCount)
        {
            int slotIndex = _companions.Count; // fill slots in order
            var brain = SpawnCompanionAtSlot(slotIndex);
            _companions.Add(brain);
        }

        // Remove
        while (_companions.Count > targetCount)
        {
            var last = _companions[_companions.Count - 1];
            if (last) Destroy(last.gameObject);
            _companions.RemoveAt(_companions.Count - 1);
        }
    }

    public void AddOneCompanion()
    {
        if (_companions.Count >= MaxSlots) return;
        int slotIndex = _companions.Count;
        var brain = SpawnCompanionAtSlot(slotIndex);
        _companions.Add(brain);
    }

    private CompanionBrain SpawnCompanionAtSlot(int slotIndex)
    {
        if (!companionPrefab)
        {
            Debug.LogError("CompanionManager: No companionPrefab assigned.");
            return null;
        }

        Transform anchor = _slotAnchors[slotIndex];
        var brain = Instantiate(companionPrefab, anchor.position, Quaternion.LookRotation(transform.forward, Vector3.up));
        brain.Init(owner: transform, slotAnchor: anchor);
        return brain;
    }

    // ===== Capture Hook (future) =====
    // Example call: when you "capture" an enemy, convert it into a companion.
    public void CaptureEnemyAsCompanion(GameObject enemyObject, int desiredSlotIndex = -1)
    {
        if (_companions.Count >= MaxSlots) return;

        int slotIndex = desiredSlotIndex >= 0 ? Mathf.Clamp(desiredSlotIndex, 0, MaxSlots - 1) : _companions.Count;
        Transform anchor = _slotAnchors[slotIndex];

        // Here you’d usually convert the enemy into a companion prefab with its visuals,
        // or destroy enemy & spawn a themed companion. For now, we just spawn your prefab.
        var brain = Instantiate(companionPrefab, enemyObject.transform.position, Quaternion.identity);
        brain.Init(transform, anchor);

        _companions.Add(brain);
        Destroy(enemyObject); // or hide/convert visuals instead
    }

    // ===== Level Broadcast (optional) =====
    // Call this from your game manager when the player levels up, to buff all companions.
    public void GrantXPToAll(float amount)
    {
        foreach (var c in _companions) if (c) c.AddXP(amount);
    }
}
