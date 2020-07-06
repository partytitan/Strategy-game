using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour
{
    public SaveLoadMenu menu;

    public string MapName
    {
        get => _mapName;
        set
        {
            _mapName = value;
            transform.GetChild(0).GetComponent<Text>().text = value;
        }
    }

    private string _mapName;

    public void Select()
    {
        menu.SelectItem(_mapName);
    }
}
