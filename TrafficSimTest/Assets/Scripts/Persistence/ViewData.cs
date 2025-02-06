using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;


[System.Serializable]
public class ViewData
{
    public float[] pos;

    public ViewData(View view)
    {
        this.pos = new float[3] {
            view.transform.position.x,
            view.transform.position.y,
            view.transform.position.z,
        };
    }
}