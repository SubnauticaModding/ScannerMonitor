#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
namespace ScannerMonitor
{
    using System;
    using HarmonyLib;
    using System.IO;
    using Game_Items;
    using Nautilus.Utility;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using BepInEx;
    using BepInEx.Logging;

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(Nautilus.PluginInfo.PLUGIN_GUID, Nautilus.PluginInfo.PLUGIN_VERSION)]
    [BepInIncompatibility("com.ahk1221.smlhelper")]
    public class Main: BaseUnityPlugin
    {
        internal static new ManualLogSource Logger { get; private set; }
        
        public void Awake()
        {
            Logger = base.Logger;
            ScannerMonitor.CreateAndRegister();
            Harmony.CreateAndPatchAll(typeof(Patches.Patches), MyPluginInfo.PLUGIN_GUID);
            SaveUtils.RegisterOnSaveEvent(SaveCache);
        }

        public static void SaveCache()
        {
            Dictionary<TechType, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>> TrackedResources = ResourceTrackerDatabase.resources;

            Dictionary<string, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>> stringifiedTrackedResources = new Dictionary<string, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>>();

            foreach (var kvp in TrackedResources)
            {
                stringifiedTrackedResources[kvp.Key.AsString()] = kvp.Value;
            }

            var CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            var writer = new StreamWriter(CacheFilePath);
            try
            {
                writer.Write(
                    JsonConvert.SerializeObject(
                        stringifiedTrackedResources, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            Culture = CultureInfo.InvariantCulture,
                            NullValueHandling = NullValueHandling.Ignore

                        })
                );
                writer.Flush();
                writer.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine($"{e}");
                writer.Close();
            }
        }
    }
}
#endif