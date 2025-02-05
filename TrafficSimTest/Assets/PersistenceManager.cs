using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class PersistenceManager : SingletonMonobehaviour<PersistenceManager>
{
    public SimulationData lastData;

    public override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);
    }

    public SimulationData SaveSimulationData(string path, Simulation simulation)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Create);

        SimulationData data = new SimulationData(simulation);
        formatter.Serialize(stream, data);
        stream.Close();

        return lastData = data;
    }

    public SimulationData LoadSimulationData(string path)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);

        SimulationData data = formatter.Deserialize(stream) as SimulationData;
        stream.Close();

        return lastData = data;
    }
}
