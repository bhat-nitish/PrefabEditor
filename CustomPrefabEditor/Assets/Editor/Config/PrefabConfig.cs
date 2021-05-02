using System;

namespace Editor.Config
{
    [Serializable]
    public class PrefabConfigItem
    {
        public string text;

        public string color;

        public string image;
    }
    
    [Serializable]
    public class PrefabConfig
    {
        public PrefabConfigItem[] config;
    }
}