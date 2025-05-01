/*
 * BendMaterialList
 * ----------------
 * A simple ScriptableObject that stores all materials which need the
 * _PlayerPos parameter driven by TopDownPlayerController (or any other
 * script).  Create as many of these assets as you like – for example,
 * one per scene or one shared across scenes.
 *
 *  • Right-click → Create → Bending → Bend Material List
 *  • Drag every material that uses the bend shader into the list.
 */

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BendMaterialList",
    menuName = "Bending/Bend Material List",
    order = 0)]
public class BendMaterialList : ScriptableObject
{
    [Tooltip("All materials that need the _PlayerPos bending vector.")]
    public List<Material> materials = new();
}