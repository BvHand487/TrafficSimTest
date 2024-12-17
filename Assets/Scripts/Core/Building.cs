using System.Collections.Generic;
using UnityEngine;

public class Building
{
    public enum Type
    {
        None,
        Home,
        Work
    }

    public Type type { get; private set; }
    public GameObject obj;
    public List<Road> adjacentRoads = new List<Road>();

    public Building(GameObject obj, Type type)
    {
        this.obj = obj;

        var mat = obj.GetComponent<Renderer>().material;
        switch (type)
        {
             case Type.Work:
                mat.color = Color.HSVToRGB(0.5f, 0.5f, 0.8f);
                break;
             case Type.Home:
                mat.color = Color.HSVToRGB(0.3f, 0.5f, 0.7f);
                break;

            default:
            mat.color = Color.black;
            break;
        }
    }
}