using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSlotUI : MonoBehaviour
{
    public Sprite        thumb;
    public TextMeshProUGUI nameText;

    public void Set(WeaponDefinition wd)
    {
        if (thumb)     thumb = wd.thumbnail;
        if (nameText)  nameText.text = wd.weaponName;
    }
}