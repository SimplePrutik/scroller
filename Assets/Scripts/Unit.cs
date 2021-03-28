using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public Text index;
    public Text content;

    public GameObject selected;
    public bool Selected => selected.activeSelf;

    [HideInInspector]
    public int tabindex;

    public void UpdateContent(int i, string con)
    {
        tabindex = i;
        index.text = i.ToString();
        content.text = con;
    }

    public void Select(bool select)
    {
        selected.SetActive(select);
    }


}
