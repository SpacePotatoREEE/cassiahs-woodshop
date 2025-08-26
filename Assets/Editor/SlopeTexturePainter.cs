// Assets/Editor/SlopeTexturePainter.cs
// Unity 6 (URP compatible) – paints a terrain’s splatmap based on slope angle.
// Hard threshold by default; set Blend Width to 0 for a razor edge.

using UnityEngine;
using UnityEditor;

public class SlopeTexturePainter : EditorWindow
{
    [Header("Target")]
    public Terrain targetTerrain;

    [Header("Layers (exactly these two will be written)")]
    public TerrainLayer grassLayer;
    public TerrainLayer cliffLayer;

    [Header("Angle Rule")]
    [Tooltip("Degrees from the horizontal plane where it flips from grass to cliff.\n" +
             "Example: 35 means slopes steeper than 35° become cliff.")]
    [Range(0f, 90f)] public float thresholdDegrees = 35f;

    [Tooltip("Optional soft blend around the threshold (degrees). 0 = hard cut.")]
    [Range(0f, 30f)] public float blendWidthDegrees = 0f;

    [Header("Sampling")]
    [Tooltip("Downsample factor for speed vs. precision. 1 = full res splatmap.")]
    [Min(1)] public int downsample = 1;

    [MenuItem("Tools/Terrain/Slope Texture Painter")]
    private static void ShowWindow()
    {
        var win = GetWindow<SlopeTexturePainter>("Slope Texture Painter");
        win.minSize = new Vector2(360, 230);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Paint Terrain By Slope", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        targetTerrain    = (Terrain)EditorGUILayout.ObjectField("Terrain", targetTerrain, typeof(Terrain), true);
        grassLayer       = (TerrainLayer)EditorGUILayout.ObjectField("Grass Layer", grassLayer, typeof(TerrainLayer), false);
        cliffLayer       = (TerrainLayer)EditorGUILayout.ObjectField("Cliff Layer", cliffLayer, typeof(TerrainLayer), false);

        EditorGUILayout.Space(6);
        thresholdDegrees   = EditorGUILayout.Slider("Threshold (°)", thresholdDegrees, 0f, 90f);
        blendWidthDegrees  = EditorGUILayout.Slider("Blend Width (°)", blendWidthDegrees, 0f, 30f);
        downsample         = EditorGUILayout.IntSlider("Downsample", downsample, 1, 8);

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(!InputsValid()))
        {
            if (GUILayout.Button("Apply To Terrain", GUILayout.Height(32)))
            {
                Apply();
            }
        }

        if (!InputsValid())
        {
            EditorGUILayout.HelpBox("Assign a Terrain and both TerrainLayers.\n" +
                                    "Make sure those layers are also added to the Terrain’s Layers list (Terrain component > Paint Terrain > Layers).",
                                    MessageType.Info);
        }
    }

    private bool InputsValid()
    {
        if (!targetTerrain || !grassLayer || !cliffLayer) return false;
        var td = targetTerrain.terrainData;
        if (td == null) return false;

        // Ensure both layers exist on the terrain
        var layers = td.terrainLayers;
        bool hasGrass = false, hasCliff = false;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == grassLayer) hasGrass = true;
            if (layers[i] == cliffLayer) hasCliff = true;
        }
        return hasGrass && hasCliff;
    }

    private void Apply()
    {
        var terrain = targetTerrain;
        var td = terrain.terrainData;
        if (td == null) return;

        // Find layer indices for the two layers we care about
        int grassIdx = -1, cliffIdx = -1;
        var layers = td.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == grassLayer) grassIdx = i;
            if (layers[i] == cliffLayer) cliffIdx = i;
        }
        if (grassIdx < 0 || cliffIdx < 0)
        {
            EditorUtility.DisplayDialog("Missing Layers",
                "Both selected TerrainLayers must be assigned on the Terrain (Paint Terrain > Layers).",
                "OK");
            return;
        }

        // Read/prepare alphamap
        int aw = td.alphamapWidth;
        int ah = td.alphamapHeight;
        int layersCount = td.alphamapLayers;

        // We will write at reduced resolution if downsample > 1, then upscale via SetAlphamaps (Unity will handle it).
        int step = Mathf.Max(1, downsample);

        // Pull existing alphamaps so we preserve other layers by zeroing them and normalizing at the end,
        // OR simply overwrite only our two layers and set others to 0 so there’s no checkerboard.
        float[,,] alphas = td.GetAlphamaps(0, 0, aw, ah);

        float halfBlend = Mathf.Max(0f, blendWidthDegrees * 0.5f);

        // Process
        for (int y = 0; y < ah; y += step)
        {
            float v = (float)y / (ah - 1);
            for (int x = 0; x < aw; x += step)
            {
                float u = (float)x / (aw - 1);

                // Get surface normal at this UV
                Vector3 n = td.GetInterpolatedNormal(u, v).normalized;
                // Angle from horizontal plane: 0° = flat ground, 90° = vertical cliff
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(n, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;

                float grassW, cliffW;

                if (halfBlend <= 0.0001f)
                {
                    // Hard threshold
                    bool isCliff = angle >= thresholdDegrees;
                    grassW = isCliff ? 0f : 1f;
                    cliffW = isCliff ? 1f : 0f;
                }
                else
                {
                    // Soft blend across [threshold - halfBlend, threshold + halfBlend]
                    float t = Mathf.InverseLerp(thresholdDegrees - halfBlend, thresholdDegrees + halfBlend, angle);
                    cliffW = Mathf.Clamp01(t);
                    grassW = 1f - cliffW;
                }

                // Write weights for all layers: only our two layers get weight, others zeroed to avoid checkerboard
                for (int li = 0; li < layersCount; li++)
                {
                    float w = 0f;
                    if (li == grassIdx) w = grassW;
                    else if (li == cliffIdx) w = cliffW;

                    // Fill this pixel and optionally the block if downsample > 1
                    for (int oy = 0; oy < step && (y + oy) < ah; oy++)
                    {
                        for (int ox = 0; ox < step && (x + ox) < aw; ox++)
                        {
                            alphas[y + oy, x + ox, li] = w;
                        }
                    }
                }
            }
        }

        // Apply back
        Undo.RegisterCompleteObjectUndo(td, "Paint Terrain By Slope");
        td.SetAlphamaps(0, 0, alphas);

        // Done
        EditorUtility.DisplayDialog("Slope Texture Painter", "Finished painting by slope.", "Nice");
        EditorUtility.SetDirty(td);
    }
}
