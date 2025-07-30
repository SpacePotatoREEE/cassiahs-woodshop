// BakeVertexLightmapToVertexColors.cs
// Place this file in an **Editor** folder.
// -----------------------------------------------------------------------------
//  ▸ After a normal light‑map bake choose:
//        *Tools ▸ Vertex Color ▸ Bake Scene (all static meshes)*
//    ‑or‑ *Tools ▸ Vertex Color ▸ Bake Selected Object(s)*
//  ▸ The tool duplicates every eligible mesh, sampling the baked light‑map
//    and writing the colour into the new mesh's vertex‑colour array.
//  ▸ The duplicates are stored in **Assets/BakedVertexMeshes/** with the name
//    "<Original>_VC" so they are permanent project assets.
//  ▸ NEW!  Need a clean re‑bake?  Use
//        *Tools ▸ Vertex Color ▸ Delete Baked Mesh Copies*
//    to remove every *_VC* mesh asset and (when possible) relink scene objects
//    back to the original meshes.
// -----------------------------------------------------------------------------
// Tested in Unity 6.0.0 (URP) — no additional packages required.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class BakeVertexLightmapToVertexColors
{
    private const string k_OutputFolder = "Assets/BakedVertexMeshes";

    // ─────────────────────────────────────────────────────────────────────────────
    //  MENU ENTRIES
    // ─────────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Vertex Color/Bake Scene (all static meshes)")]
    private static void BakeAllStaticMeshes()
    {
        MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
        int baked = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            if (!IsContributingGI(renderer))
                continue;                                // skip non‑static meshes

            if (renderer.lightmapIndex < 0)
                continue;                                // no light‑map assigned

            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            if (Bake(renderer, filter))
                baked++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[VertexColorBake] Finished. Baked {baked} meshes into vertex colours.");
    }

    [MenuItem("Tools/Vertex Color/Bake Selected Object(s)")]
    private static void BakeSelected()
    {
        GameObject[] selection = Selection.gameObjects;
        if (selection.Length == 0)
        {
            Debug.LogWarning("[VertexColorBake] Nothing selected.");
            return;
        }

        int baked = 0;
        foreach (GameObject go in selection)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            var filter   = go.GetComponent<MeshFilter>();
            if (renderer == null || filter == null || filter.sharedMesh == null)
                continue;

            if (renderer.lightmapIndex < 0)
            {
                Debug.LogWarning($"[VertexColorBake] {go.name} has no light‑map; skipped.");
                continue;
            }

            if (Bake(renderer, filter))
                baked++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[VertexColorBake] Finished. Baked {baked} selected meshes.");
    }

    [MenuItem("Tools/Vertex Color/Delete Baked Mesh Copies")]
    private static void DeleteBakedVertexMeshes()
    {
        // 1. Delete mesh assets inside the output folder
        int deleted = 0;
        if (Directory.Exists(k_OutputFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { k_OutputFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.DeleteAsset(path))
                    deleted++;
            }

            // Remove the folder if it is now empty
            if (Directory.Exists(k_OutputFolder) &&
                Directory.GetFiles(k_OutputFolder).Length == 0 &&
                Directory.GetDirectories(k_OutputFolder).Length == 0)
            {
                AssetDatabase.DeleteAsset(k_OutputFolder);
            }
        }

        // 2. Restore scene MeshFilters that were pointing at *_VC* meshes
        int restored = 0;
        MeshFilter[] filters = Object.FindObjectsOfType<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            Mesh m = filter.sharedMesh;
            if (m == null || !m.name.EndsWith("_VC"))
                continue;

            string originalName = m.name.Substring(0, m.name.Length - 3); // remove _VC
            string[] originalGuids = AssetDatabase.FindAssets(originalName + " t:Mesh");
            foreach (string oguid in originalGuids)
            {
                string opath = AssetDatabase.GUIDToAssetPath(oguid);
                if (opath.StartsWith(k_OutputFolder))
                    continue;                           // skip other baked versions

                Mesh originalMesh = AssetDatabase.LoadAssetAtPath<Mesh>(opath);
                if (originalMesh != null)
                {
                    filter.sharedMesh = originalMesh;
                    restored++;
                    break;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VertexColorBake] Deleted {deleted} baked meshes and restored {restored} mesh references.");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  INTERNAL
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool Bake(MeshRenderer renderer, MeshFilter filter)
    {
        Mesh sourceMesh = filter.sharedMesh;
        if (sourceMesh == null)
            return false;

        Vector2[] uv2 = sourceMesh.uv2;
        if (uv2 == null || uv2.Length == 0)
        {
            Debug.LogWarning($"[VertexColorBake] Mesh '{sourceMesh.name}' has no UV2 channel for light‑mapping; skipped.");
            return false;
        }

        // Fetch the correct light‑map texture
        int lmIndex = renderer.lightmapIndex;
        LightmapData lmData = LightmapSettings.lightmaps[lmIndex];
        Texture2D lmTex = lmData.lightmapColor;          // we only read colour (non‑directional)
        if (lmTex == null)
        {
            Debug.LogError("[VertexColorBake] Missing light‑map texture; aborted.");
            return false;
        }

        if (!EnsureReadable(lmTex))
        {
            Debug.LogError("[VertexColorBake] Light‑map not readable and could not make it readable; aborted.");
            return false;
        }

        // Pre‑calc scale/offset once (x,y = scale; z,w = offset)
        Vector4 scaleOffset = renderer.lightmapScaleOffset;
        int vCount = sourceMesh.vertexCount;
        Color[] vColors = new Color[vCount];

        for (int i = 0; i < vCount; i++)
        {
            Vector2 uv = uv2[i];
            Vector2 uvLM = new Vector2(
                uv.x * scaleOffset.x + scaleOffset.z,
                uv.y * scaleOffset.y + scaleOffset.w);

            Color c = lmTex.GetPixelBilinear(uvLM.x, uvLM.y);
            vColors[i] = c;
        }

        // Duplicate mesh so we do not overwrite the original asset
        Mesh bakedMesh = Object.Instantiate(sourceMesh);
        bakedMesh.name = sourceMesh.name + "_VC";
        bakedMesh.colors = vColors;

        // Ensure output folder exists
        if (!Directory.Exists(k_OutputFolder))
            Directory.CreateDirectory(k_OutputFolder);

        string path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(k_OutputFolder, bakedMesh.name + ".asset"));
        AssetDatabase.CreateAsset(bakedMesh, path);
        filter.sharedMesh = bakedMesh;

        Debug.Log($"[VertexColorBake] '{bakedMesh.name}' written to {path}");
        return true;
    }

    private static bool IsContributingGI(MeshRenderer renderer)
    {
        return GameObjectUtility.AreStaticEditorFlagsSet(renderer.gameObject, StaticEditorFlags.ContributeGI);
    }

    /// <summary>
    /// Makes sure the texture can be CPU‑sampled with GetPixelBilinear(). Returns true if readable.
    /// </summary>
    private static bool EnsureReadable(Texture2D tex)
    {
        if (tex.isReadable)
            return true;

        string assetPath = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed; // avoid artifacts
        importer.SaveAndReimport();
        return tex.isReadable;
    }
}
