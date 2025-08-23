// BattleBootstrap.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleBootstrap : MonoBehaviour
{
    public BattleContext context;

    private void Start()
    {
        if (!context.playerPrefab || !context.enemyPrefab)
        {
            Debug.LogError("BattleBootstrap: context not filled!");
            return;
        }

        // Position prefabs
        Transform ps = GameObject.Find("PlayerSpawn").transform;
        Transform es = GameObject.Find("EnemySpawn").transform;

        context.playerPrefab.transform.SetPositionAndRotation(ps.position, ps.rotation);
        context.enemyPrefab .transform.SetPositionAndRotation(es.position, es.rotation);

        // OPTIONAL: parent them under an “Actors” GameObject for cleanliness
    }

    /* Later, when battle ends, call: */
    public void ExitBattle(bool playerWon)
    {
        SceneManager.LoadScene(context.returnScene);
        context.Clear();
    }
}