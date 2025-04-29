using System.IO;
using Core;
using UnityEngine;

namespace Persistence
{
    public class PersistenceManager : SingletonMonobehaviour<PersistenceManager>
    {
        public SimulationData lastData;

        public override void Awake()
        {
            if (Instance != null)
                Destroy(Instance.gameObject);

            base.Awake();
            DontDestroyOnLoad(this);

            PlayerPrefs.DeleteAll();
        }

        public SimulationData SaveSimulationData(string path, Simulation simulation)
        {
            FileStream stream = new FileStream(path, FileMode.Create);

            SimulationData data = new SimulationData(simulation);
            byte[] json = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));

            stream.Write(json, 0, json.Length);
            stream.Close();

            return lastData = data;
        }

        public SimulationData LoadSimulationData(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open);

            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            stream.Close();

            string json = System.Text.Encoding.UTF8.GetString(bytes);
            SimulationData data = JsonUtility.FromJson<SimulationData>(json);

            return lastData = data;
        }
    }
}
