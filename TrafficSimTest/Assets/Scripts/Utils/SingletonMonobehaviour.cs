using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SingletonMonobehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
                Debug.Log("SingletonMonobehaviour instance not set.");

            return _instance;
        }
    }

    public virtual void Awake()
    {
        _instance = this as T;
    }
}