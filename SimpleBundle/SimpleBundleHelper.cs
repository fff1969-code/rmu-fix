using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public static class SimpleBundleConfig
    {
        /// true: smaller file size but slower (30 seconds more on 3900x for sample project)
        /// false: bigger file size but faster
        public static bool Compress = true;

        /// Add more if RMU added new groups in future.
        /// Otherwise do not change this known group name list.
        public static List<string> GroupNames = new List<string>
        {
            "animations",
            "battleback",
            "characters",
            "objects",
            "enemies",
            "titles",
            "movies",
            "sounds",
            "others",
        };

        /// Add the "groupname" you want to omit during bundle generation,
        /// For example, if you already generated 'battleback' and won't add new assets,
        /// put it here to avoid re-generation to save the build time.
        public static List<string> OmitGroups = new List<string>
        {
            //"battleback",
            //"characters",
            //"sounds",
        };

        /// true: re-create all json data before build but slower (90 seconds more on 3900x for sample project)
        /// false: skip this step but faster, might create out-of-date build
        /// You should only skip this if you're sure all data is not modified.
        public static bool GenerateJson = true;
    }

    // the helper object which managed all asset bundles
    public class SimpleBundleHelper
    {
        // the prefix in assets folder
        public const string PATH_PREFIX = "rmu";
        
        // the build folder, should inside the Assets because database is generated here
        public const string BUILD_FOLDER = "Assets/RPGMaker/Storage/SimpleBundle";
        
        // the database bundle and file name
        public const string DATA_FILE_NAME = "simplebundle";

        public const string BUNDLE_EXT = ".unity3d";

        private static Dictionary<string, AssetBundle> _bundles;
        private static Dictionary<string, BundleEntry> _entries;

        public class BundleEntry
        {
            public string name;
            public SimpleBundleEntry data;
        }

        public static Dictionary<string, BundleEntry> GetOrCreateEntries()
        {
            if (_entries == null || _bundles == null)
            {
#if UNITY_EDITOR
                // force-reload all asset bundles
                AssetBundle.UnloadAllAssetBundles(true);
#endif

                // try to load the bundle for database
                var simpleBundlePath = SimpleBundleHelper.GetBundlePath(SimpleBundleHelper.DATA_FILE_NAME);
                var simpleBundle = AssetBundle.LoadFromFile(simpleBundlePath);
                if (simpleBundle == null)
                {
                    Debug.LogError("[Simple] Failed to load: " + SimpleBundleHelper.DATA_FILE_NAME);
                    return null;
                }

                // load the database from bundle (the asset itself use same name as bundle)
                var database = simpleBundle.LoadAsset<SimpleBundleData>(SimpleBundleHelper.DATA_FILE_NAME);
                if (database == null)
                {
                    simpleBundle.Unload(true);
                    Debug.LogError("[Simple] Failed to load: " + SimpleBundleHelper.DATA_FILE_NAME);
                    return null;
                }

                // create entries records and loaded all splitted bundles
                var bundles = new Dictionary<string, AssetBundle>(16);
                var entries = new Dictionary<string, BundleEntry>(1024);

                foreach (var group in database.groups)
                {
                    if (group.entries == null || group.entries.Length <= 0)
                        continue;

                    try
                    {
                        var bundleFile = SimpleBundleHelper.GetBundlePath(group.name);
                        Debug.Log("[Simple] Loading:" + bundleFile);

                        var bundle = AssetBundle.LoadFromFile(bundleFile);
                        if (bundle != null)
                        {
                            bundles[group.name] = bundle;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[Simple] Failed to open '" + group.name + "' : " + e.Message);
                    }

                    foreach (var entry in group.entries)
                    {
                        entries[entry.address] = new BundleEntry
                        {
                            name = group.name,
                            data = entry,
                        };
                    }
                }

                _bundles = bundles;
                _entries = entries;
                simpleBundle.Unload(true);
            }

            return _entries;
        }

        public static bool Query(string key, out BundleEntry entry, out AssetBundle bundle)
        {
            entry = null;
            bundle = null;

            if (GetOrCreateEntries()?.TryGetValue(key, out entry) == true)
            {
                var bundleName = entry.name;
                if (_bundles.TryGetValue(bundleName, out bundle))
                {
                    return true;
                }
            }

            return false;
        }

        // it will be loaded from this path
        public static string GetBundlePath(string name)
        {
            // for example on windows, it will be something like this:
            // <PROJECT_DATA>/StreamingAssets/rmu/<FileName>.unity3d
            //
            return string.Format("{0}/{1}/{2}{3}", Application.streamingAssetsPath, PATH_PREFIX, name, BUNDLE_EXT);
        }

        public static SimpleBundleGroup GetGroup(SimpleBundleGroup[] groups, string name)
        {
            return groups?.FirstOrDefault(group => group.name == name);
        }

#if UNITY_EDITOR
        
        // create the bundle path from platform and name
        public static string GetEditorBuildPath(BuildTarget target)
        {
            var player = BuildTargetConverter.TryConvertToRuntimePlatform(target);
            if (player == null)
                throw new InvalidOperationException("Unknown target: " + target.ToString());

            // the generated bundle are saved in this folder to be copied to build folder later
            return string.Format("{0}/{1}/{2}/", BUILD_FOLDER, target, PATH_PREFIX);
        }

        // different option for bundle generation
        public enum BundleOptions
        {
            Compress = BuildAssetBundleOptions.ChunkBasedCompression,
            Uncompress = BuildAssetBundleOptions.UncompressedAssetBundle,
        }

        public static string[] GetBundleFiles(BuildTarget target)
        {
            return Directory.GetFiles(GetEditorBuildPath(target), "*" + BUNDLE_EXT, SearchOption.TopDirectoryOnly);
        }

        public static void GenerateAssetsFromSetting(List<AddressableAssetGroup> groups, BuildTarget target, BundleOptions option)
        {
            // all known asset groups, add more in the config if there are new groups in future
            var bundleGroups = new SimpleBundleGroup[SimpleBundleConfig.GroupNames.Count];
            for (int i = 0; i < bundleGroups.Length; i++)
            {
                bundleGroups[i] = new SimpleBundleGroup { name = SimpleBundleConfig.GroupNames[i] };
            }

            // create the mapping between key <==> fullpath
            var entries = new List<SimpleBundleEntry>(1024);
            foreach (var group in groups)
            {
                // only save known groups
                var bundleGroup = GetGroup(bundleGroups, group.name);
                if (bundleGroup == null)
                    continue;

                entries.Clear();
                foreach (var entry in group.entries)
                {
                    entries.Add(new SimpleBundleEntry
                    {
                        address = entry.address,
                        fullpath = entry.AssetPath,
                        guid = entry.guid,
                    });
                }

                if (entries.Count > 0)
                {
                    bundleGroup.entries = entries.ToArray();

                    // generate the assets bundle
                    if (!SimpleBundleConfig.OmitGroups.Contains(group.name))
                    {
                        // grab all file paths witout the duplicated ones
                        var files = entries.Select(entry => entry.fullpath).Distinct().ToArray();
                        GenerateAssetBundle(group.name, files, target, option);
                    }
                }
            }

            // The database with all file mapping is packed into a single bundle file to be loaded first.
            // This database asset file share the same name as its bunle file for convenient.
            var databaseFileName = string.Format("{0}/{1}.asset", BUILD_FOLDER, DATA_FILE_NAME);
            try
            {
                AssetDatabase.StartAssetEditing();

                // create the simple bundle database and store it in an asset file.
                // must do this after bundle generation to avoid null ref.
                var bundleData = ScriptableObject.CreateInstance<SimpleBundleData>();
                bundleData.groups = bundleGroups;
                bundleData.stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffffff");

                AssetDatabase.CreateAsset(bundleData, databaseFileName);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // save the bundle data into assets
            GenerateAssetBundle(DATA_FILE_NAME, new string[] { databaseFileName }, target, option, true);
        }

        private static void GenerateAssetBundle(string name, string[] files, BuildTarget target, BundleOptions option, bool keepFileName = false)
        {
            // create the bundle path from platform and name
            var bundlePath = GetEditorBuildPath(target) + name + BUNDLE_EXT;

            Debug.Log(string.Format("[Simple] {0}: Building '{1}' with '{2}' assets into {3}...", DateTime.Now.ToString("HH:mm:ss.ffffff"), name, files.Length, bundlePath));

            var buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = System.IO.Path.GetFileName(bundlePath);
            buildMap[0].assetNames = files;

            var output = System.IO.Path.GetDirectoryName(bundlePath);
            if (!Directory.Exists(output)) Directory.CreateDirectory(output);

            var bundleOption = (BuildAssetBundleOptions)option;
            if (!keepFileName)
            {
                // do not need these loading method, always load by full path
                bundleOption = bundleOption | BuildAssetBundleOptions.DisableLoadAssetByFileName
                                            | BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
            }

            BuildPipeline.BuildAssetBundles(output, buildMap, bundleOption, target);

            Debug.Log(string.Format("[Simple] {0}: Generated {1}", DateTime.Now.ToString("HH:mm:ss.ffffff"), bundlePath));
        }

        public static class BuildTargetConverter
        {
            public static RuntimePlatform? TryConvertToRuntimePlatform(BuildTarget buildTarget)
            {
                switch (buildTarget)
                {
                    case BuildTarget.Android:
                        return RuntimePlatform.Android;
                    case BuildTarget.PS4:
                        return RuntimePlatform.PS4;
                    case BuildTarget.PS5:
                        return RuntimePlatform.PS5;
                    case BuildTarget.StandaloneLinux64:
                        return RuntimePlatform.LinuxPlayer;
                    case BuildTarget.LinuxHeadlessSimulation:
                        return RuntimePlatform.LinuxPlayer;
                    case BuildTarget.StandaloneOSX:
                        return RuntimePlatform.OSXPlayer;
                    case BuildTarget.StandaloneWindows:
                        return RuntimePlatform.WindowsPlayer;
                    case BuildTarget.StandaloneWindows64:
                        return RuntimePlatform.WindowsPlayer;
                    case BuildTarget.Switch:
                        return RuntimePlatform.Switch;
                    case BuildTarget.WSAPlayer:
                        return RuntimePlatform.WSAPlayerARM;
                    case BuildTarget.XboxOne:
                        return RuntimePlatform.XboxOne;
                    case BuildTarget.iOS:
                        return RuntimePlatform.IPhonePlayer;
                    case BuildTarget.tvOS:
                        return RuntimePlatform.tvOS;
                    case BuildTarget.WebGL:
                        return RuntimePlatform.WebGLPlayer;
                    case BuildTarget.GameCoreXboxSeries:
                        return RuntimePlatform.GameCoreXboxSeries;
                    case BuildTarget.GameCoreXboxOne:
                        return RuntimePlatform.GameCoreXboxOne;
                    case BuildTarget.EmbeddedLinux:
                        return RuntimePlatform.EmbeddedLinuxArm64;
                    // add more ...
                    default:
                        return null;
                }
            }
        }
#endif
    }
}