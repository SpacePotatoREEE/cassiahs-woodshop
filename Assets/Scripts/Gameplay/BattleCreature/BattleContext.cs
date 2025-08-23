// BattleContext.cs  (ScriptableObject makes debugging easy)
using UnityEngine;

[CreateAssetMenu(menuName = "Battles/Context")]
public class BattleContext : ScriptableObject
{
    [HideInInspector] public GameObject playerPrefab;
    [HideInInspector] public GameObject enemyPrefab;
    [HideInInspector] public string     returnScene;   // overworld scene name

    public void Clear()
    {
        playerPrefab = null;
        enemyPrefab  = null;
        returnScene  = null;
    }
}