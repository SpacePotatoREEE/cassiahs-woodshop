using UnityEngine;
using System.Collections;

public class ScaleUpOnSpawn : MonoBehaviour
{
    public float scaleUpDuration = 1f;
    private void Awake()
    {
        transform.localScale = Vector3.zero;
    }
    private void Start()
    {
        StartCoroutine(ScaleRoutine());
    }
    private IEnumerator ScaleRoutine()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale   = Vector3.one;

        while (elapsed < scaleUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaleUpDuration);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}