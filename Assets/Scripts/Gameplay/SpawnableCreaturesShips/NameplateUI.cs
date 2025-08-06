// NameplateUI.cs
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class NameplateUI : MonoBehaviour
{
    private TextMeshPro _tmp;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _tmp.fontSize = 2;
        _tmp.alignment = TextAlignmentOptions.Center;
    }

    private void LateUpdate()
    {
        if (Camera.main)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }

    public void SetName(string name) => _tmp.text = name;
}