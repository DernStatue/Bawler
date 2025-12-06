using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance;

    public Dictionary<string, int> resources = new Dictionary<string, int>()
    {
        { "ore", 50 },
        { "iron", 0 },
        { "steel", 0 },
        { "gear", 0 },
        { "money", 100 }
    };

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public bool HasResource(string type, int amount)
    {
        return resources.ContainsKey(type) && resources[type] >= amount;
    }

    public void AddResource(string type, int amount)
    {
        if (resources.ContainsKey(type))
            resources[type] += amount;
    }

    public void RemoveResource(string type, int amount)
    {
        if (resources.ContainsKey(type))
            resources[type] -= amount;
    }
}