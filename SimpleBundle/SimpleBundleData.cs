using System;
using UnityEngine;

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    [Serializable]
    public class SimpleBundleData : ScriptableObject
    {
        public string stamp;
        public SimpleBundleGroup[] groups;
    }

    [Serializable]
    public class SimpleBundleEntry
    {
        public string address;
        public string fullpath;
        public string guid;
    }

    [Serializable]
    public class SimpleBundleGroup
    {
        public string name;
        public SimpleBundleEntry[] entries;
    }
}