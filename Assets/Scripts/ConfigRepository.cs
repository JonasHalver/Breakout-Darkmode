using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfigRepository : MonoBehaviour
{
    [Serializable]
    public struct ConfigStruct
    {
        public string Name;
        public ConfigBase Config;
    }

    [SerializeField] List<ConfigStruct> _configs = new();
    public static ConfigRepository Instance;

    static Dictionary<string, ConfigBase> s_configDictionary = new();
    void Awake()
    {
        Instance = this;
        foreach (var configStruct in _configs)
        {
            if (s_configDictionary.ContainsKey(configStruct.Name))
            {
                continue;
            }
            s_configDictionary.Add(configStruct.Name, configStruct.Config);
        }
    }
    public static bool TryGetConfig<T>(string name, out T config)
    {
        if (s_configDictionary.TryGetValue(name, out var value) && value is T castedValue)
        {
            config = castedValue;
            return true;
        }
        config = default;
        return false;
    }
}
