using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utils
{
    public static class Random
    {
        public static T Select<T> (IEnumerable<T> list)
        {
            return list.Count() != 0 ? list.ElementAt(UnityEngine.Random.Range(0, list.Count())) : default(T);
        }
    }
}
