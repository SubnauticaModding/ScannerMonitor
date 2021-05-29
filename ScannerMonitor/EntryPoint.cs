#if !UNITY_EDITOR
namespace ScannerMonitor
{
    using System;
    using HarmonyLib;
    using System.IO;
    using System.Reflection;
    using QModManager.API.ModLoading;
    using UnityEngine;
    using Game_Items;

    [QModCore]
    public static class EntryPoint
    {
        public static TechType ScannerMonitorTechType { get; private set; }
        [QModPatch]
        public static void Entry()
        {
            var scannerMonitor = new ScannerMonitor(); 
            scannerMonitor.Patch();
            ScannerMonitorTechType = scannerMonitor.TechType;
            Harmony.CreateAndPatchAll(typeof(Patches.Patches), "MrPurple6411.ScannerMonitor");
        }
    }
}
#endif