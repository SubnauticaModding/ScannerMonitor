#if !UNITY_EDITOR
namespace ScannerMonitor
{
    using System;
    using HarmonyLib;
    using System.IO;
    using System.Reflection;
    using QModManager.API.ModLoading;
    using UnityEngine;
    using ScannerMonitor.Game_Items;

    [QModCore]
    public static class EntryPoint
    {
        public static string MOD_FOLDER_LOCATION = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ASSETS_FOLDER_LOCATION = Path.Combine(MOD_FOLDER_LOCATION, "Assets");
        public static string ASSET_BUNDLE_LOCATION = Path.Combine(ASSETS_FOLDER_LOCATION , "scannermonitor");

        public static GameObject SCANNER_MONITOR_DISPLAY_MODEL { get; set; }
        public static ScannerMonitor ScannerMonitor = new ScannerMonitor();

        [QModPatch]
        public static void Entry()
        {
            if (LoadAssets())
            {
                GameObject.DontDestroyOnLoad(SCANNER_MONITOR_DISPLAY_MODEL);
                Harmony.CreateAndPatchAll(typeof(Patches.Patches), "MrPurple6411.ScannerMonitor");
                ScannerMonitor.Patch();
            }
            else
            {
                throw new Exception($"Failed to load assets.");
            }
        }

        public static bool LoadAssets()
        {
            try
            {
                var ASSET_BUNDLE = AssetBundle.LoadFromFile(ASSET_BUNDLE_LOCATION);
                SCANNER_MONITOR_DISPLAY_MODEL = ASSET_BUNDLE.LoadAsset("ScannerMonitorModel") as GameObject;
                return SCANNER_MONITOR_DISPLAY_MODEL != null;
            }
            catch
            {
                return false;
            }
        }
        
    }
}
#endif