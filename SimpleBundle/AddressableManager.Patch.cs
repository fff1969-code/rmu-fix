#define USE_PARTIAL_LOOP

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace RPGMaker.Codebase.CoreSystem.Helper
{
    public partial class AddressableManager
    {
        // to patch the original object by implementing the load interface with SimpleBundleHelper
        // rename the original one in AdressableManager.cs to something like 'LoadBackup' avoid the compile error
        public class Load
        {
            public static string GetRelPath(string fullpath)
            {
                var rel = fullpath.Replace("\\", "/");
                int start = rel.LastIndexOf("/", rel.LastIndexOf("/") - 1) + 1;

                // must use path to avoid wrong slash
                rel = rel.Substring(start);

                if (System.IO.Path.GetExtension(rel) == ".json")
                {
                    var dir = System.IO.Path.GetDirectoryName(rel).Replace("\\", "/");
                    var name = System.IO.Path.GetFileNameWithoutExtension(rel);
                    dir = dir.Replace("JSON", "SO");
                    rel = dir + "/" + name + ".asset";
                }

                return rel;
            }

            public static T LoadAssetSync<T>(string fullpath) where T : UnityEngine.Object
            {
                // skip the invalid tile folder loading
                if (fullpath.EndsWith(".meta")) return default;

                // get the relative path
                var rel = GetRelPath(fullpath);
                if (rel == "") return default;

                if (SimpleBundleHelper.Query(rel, out SimpleBundleHelper.BundleEntry entry, out AssetBundle bundle))
                {
                    var result = bundle.LoadAsset<T>(entry.data.fullpath);
                    if (result != null)
                        return result;
                }

                Debug.LogError("[Simple] Failed to load: " + fullpath);
                return default;
            }

            public static async Task<bool> CheckResourceExistence(string fullpath)
            {
                // get the relative path
                var rel = GetRelPath(fullpath);
                if (rel == "") return default;

                await Task.Delay(0); // yield one frame to make it async

                return SimpleBundleHelper.Query(rel, out SimpleBundleHelper.BundleEntry entry, out AssetBundle bundle);
            }
        }

#if UNITY_EDITOR
        public partial class RefreshAssetPath
        {
            public class SimpleBundleProcessor : UnityEditor.Build.BuildPlayerProcessor
            {
                public override void PrepareForBuild(UnityEditor.Build.BuildPlayerContext buildPlayerContext)
                {
                    var options = buildPlayerContext.BuildPlayerOptions;

                    // try to added generated files into the 'StreamingAssets' folder
                    var files = SimpleBundleHelper.GetBundleFiles(options.target);
                    foreach (var file in files)
                    {
                        var targetFile = SimpleBundleHelper.PATH_PREFIX + "/" + System.IO.Path.GetFileName(file);
                        buildPlayerContext.AddAdditionalPathToStreamingAssets(file, targetFile);
                    }
                }
            }

            private static void BuildPlayerHandlerSimple(BuildPlayerOptions options)
            {
                // patch the build method with a much simpler version, to speed up the build time
                Debug.Log(string.Format("[Simple] {0}: BuildPlayerHandlerSimple ==== Starts ====", DateTime.Now.ToString("HH:mm:ss.ffffff")));

#if USE_PARTIAL_LOOP
                RefreshAssetPath.UpdateLoopInfo();
#endif

                try
                {
                    AssetDatabase.StartAssetEditing();

                    // only call this when you do want to re-create all assets
                    if (SimpleBundleConfig.GenerateJson)
                    {
                        Debug.Log(string.Format("[Simple] {0}: CreateSO", DateTime.Now.ToString("HH:mm:ss.ffffff")));
                        ScriptableObjectOperator.CreateSO();
                        UnityEditorWrapper.AssetDatabaseWrapper.Refresh2();
                    }

                    Debug.Log(string.Format("[Simple] {0}: BuildSetting", DateTime.Now.ToString("HH:mm:ss.ffffff")));
                    BuildSetting();

                    // whether create or refresh the addressable groups
                    AddressableAssetSettings settings = null;
                    var settingFile = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
                    var needCreate = !File.Exists(settingFile);
                    settings = needCreate ?
                            AddressableAssetSettings.Create("Assets/AddressableAssetsData", "AddressableAssetSettings", true, true) :
                            AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(settingFile);

                    AddressableAssetSettingsDefaultObject.Settings = settings;
                    Path.RefreshAssetPath(settings, needCreate);
                    UnityEditorWrapper.AssetDatabaseWrapper.Refresh();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                Debug.Log(string.Format("[Simple] {0}: GenerateAssetsFromSetting", DateTime.Now.ToString("HH:mm:ss.ffffff")));

                // Generate 'AssetBundle' directly instead of 'Addressable Assets'
                var groups = AddressableAssetSettingsDefaultObject.Settings.groups;
                SimpleBundleHelper.GenerateAssetsFromSetting(groups, options.target,
                    SimpleBundleConfig.Compress
                    ? SimpleBundleHelper.BundleOptions.Compress
                    : SimpleBundleHelper.BundleOptions.Uncompress
                );

                Debug.Log(string.Format("[Simple] {0}: BuildPlayer", DateTime.Now.ToString("HH:mm:ss.ffffff")));

                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);

                Debug.Log(string.Format("[Simple] {0}: BuildPlayerHandlerSimple ==== Ends ====", DateTime.Now.ToString("HH:mm:ss.ffffff")));
            }
        }
#endif
    }
}