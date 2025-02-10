using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class Random
    {
        public static T Select<T>(IEnumerable<T> list)
        {
            return list.Count() != 0 ? list.ElementAt(UnityEngine.Random.Range(0, list.Count())) : default(T);
        }
    }

    public static class Arrays
    {
        public static T[] Flatten2DArray<T>(T[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            T[] flattened = new T[rows * cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    flattened[i * cols + j] = array[i, j];

            return flattened;
        }
        public static T[] Flatten2DArray<T>(T[][] array)
        {
            int rows = array.Length;
            int cols = array[0].Length;
            T[] flattened = new T[rows * cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    flattened[i * cols + j] = array[i][j];

            return flattened;
        }

        public static T[,] To2DArray<T>(T[] array, int rows, int cols)
        {
            if (rows * cols != array.Length)
                return null;

            T[,] array2D = new T[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    array2D[i, j] = array[i * cols + j];

            return array2D;
        }
    }
}
