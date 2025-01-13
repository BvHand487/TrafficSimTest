using System.Collections.Generic;
using UnityEngine;

public class SimulationsManager
{
    public Simulation simulation;
    public List<Simulation> instances;
    
    public SimulationsManager()
    {
        simulation = CreateSimulation();
        instances = new List<Simulation>();
    }

    public Simulation CreateSimulation()
    {
        GameObject obj = GameObject.Instantiate(GameManager.Instance.simulationPrefab, Vector3.zero, Quaternion.identity);
        obj.SetActive(true);
        obj.name = GameManager.Instance.simulationPrefab.name;
        return obj.GetComponent<Simulation>();
    }

    public void Initialize(List<Junction> js, List<Road> rs, List<Building> bs, float physicalSize)
    {
        var ground = GameObject.Instantiate(GameManager.Instance.groundPrefab, Vector3.zero, Quaternion.identity, simulation.transform);
        var scale = ground.transform.localScale;
        scale.Scale(new Vector3(physicalSize / GameManager.Instance.tileSize, 1f, physicalSize / GameManager.Instance.tileSize));
        ground.transform.localScale = scale;

        this.simulation.Initialize(js, rs, bs, physicalSize);
    }

    public void MakeCopies(int times, float spacing=15f)
    {
        DestroyCopies();

        float offsetPerInstance = simulation.physicalSize + spacing;

        for (int i = 0; i < times; ++i)
        {
            Simulation copy = simulation.Duplicate();
            copy.transform.position += Vector3.right * offsetPerInstance * (i + 1);
            instances.Add(copy);
        }
    }

    public void DestroyCopies()
    {
        foreach (Simulation simulationInstance in instances)
        {
            simulationInstance.Destroy();
        }

        instances.Clear();
    }
}
