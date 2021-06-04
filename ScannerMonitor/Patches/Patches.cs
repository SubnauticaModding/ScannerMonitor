using System;

#if !UNITY_EDITOR
namespace ScannerMonitor.Patches
{
    using HarmonyLib;
    using UnityEngine;
    using System.Collections.Generic;
    using System.IO;
    using SMLHelper.V2.Utility;
#if SN1
    using ResourceTrackerDatabase = ResourceTracker;
#endif
#if SUBNAUTICA_STABLE
    using Oculus.Newtonsoft.Json;
#else
    using Newtonsoft.Json;
#endif


    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.Start))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_Start_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;
            
            Start(__instance);
            return false;
        }

#if SN1
        private static void Start(MapRoomFunctionality mapRoomFunctionality)
        {
            mapRoomFunctionality.wireFrameWorld.rotation = Quaternion.identity;
            mapRoomFunctionality.ReloadMapWorld();
            if(mapRoomFunctionality.typeToScan != TechType.None)
            {
                double num = mapRoomFunctionality.timeLastScan;
                int num2 = mapRoomFunctionality.numNodesScanned;
                mapRoomFunctionality.StartScanning(mapRoomFunctionality.typeToScan);
                mapRoomFunctionality.timeLastScan = num;
                mapRoomFunctionality.numNodesScanned = num2;
            }
            ResourceTracker.onResourceDiscovered += mapRoomFunctionality.OnResourceDiscovered;
            ResourceTracker.onResourceRemoved += mapRoomFunctionality.OnResourceRemoved;
            mapRoomFunctionality.matInstance = Object.Instantiate(mapRoomFunctionality.mat);
            mapRoomFunctionality.matInstance.SetFloat(ShaderPropertyID._ScanIntensity, 0f);
            mapRoomFunctionality.matInstance.SetVector(ShaderPropertyID._MapCenterWorldPos, mapRoomFunctionality.transform.position);
            MapRoomFunctionality.mapRooms.Add(mapRoomFunctionality);
            mapRoomFunctionality.Subscribe(true);
            mapRoomFunctionality.powerRelay = mapRoomFunctionality.GetComponentInParent<PowerRelay>();
            bool flag;
            if(mapRoomFunctionality.powerRelay)
            {
                flag = !GameModeUtils.RequiresPower() || mapRoomFunctionality.powerRelay.IsPowered();
                mapRoomFunctionality.prevPowerRelayState = flag;
                mapRoomFunctionality.forcePoweredIfNoRelay = false;
            }
            else
            {
                flag = true;
                mapRoomFunctionality.prevPowerRelayState = true;
                mapRoomFunctionality.forcePoweredIfNoRelay = true;
            }
            mapRoomFunctionality.screenRoot.SetActive(flag);
            mapRoomFunctionality.hologramRoot.SetActive(flag);
            if(flag)
            {
                mapRoomFunctionality.ambientSound.Play();
            }
        }
#elif BZ
        private static void Start(MapRoomFunctionality mapRoomFunctionality)
        {
            mapRoomFunctionality.wireFrameWorld.rotation = Quaternion.identity;
            mapRoomFunctionality.ReloadMapWorld();
            if(mapRoomFunctionality.typeToScan != TechType.None)
            {
                double num = mapRoomFunctionality.timeLastScan;
                int num2 = mapRoomFunctionality.numNodesScanned;
                mapRoomFunctionality.StartScanning(mapRoomFunctionality.typeToScan);
                mapRoomFunctionality.timeLastScan = num;
                mapRoomFunctionality.numNodesScanned = num2;
            }

            ResourceTrackerDatabase.onResourceDiscovered += mapRoomFunctionality.OnResourceDiscovered;
            ResourceTrackerDatabase.onResourceRemoved += mapRoomFunctionality.OnResourceRemoved;
            mapRoomFunctionality.matInstance = UnityEngine.Object.Instantiate<Material>(mapRoomFunctionality.mat);
            mapRoomFunctionality.matInstance.SetFloat(ShaderPropertyID._ScanIntensity, 0f);
            mapRoomFunctionality.matInstance.SetFloat(ShaderPropertyID._ScanFrequency, 0f);
            mapRoomFunctionality.matInstance.SetVector(ShaderPropertyID._MapCenterWorldPos,
                mapRoomFunctionality.transform.position);
            MapRoomFunctionality.mapRooms.Add(mapRoomFunctionality);
            mapRoomFunctionality.Subscribe(true);
            mapRoomFunctionality.powered = (!GameModeUtils.RequiresPower() || mapRoomFunctionality.powerConsumer.IsPowered());
            mapRoomFunctionality.screenRoot.SetActive(mapRoomFunctionality.powered);
            mapRoomFunctionality.hologramRoot.SetActive(mapRoomFunctionality.powered);
            if(mapRoomFunctionality.powered)
            {
                mapRoomFunctionality.ambientSound.Play();
            }
        }
#endif
        
#if SN1
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.GetScanRange))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_GetScanRange_Prefix(MapRoomFunctionality __instance, ref float __result)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;
            
            __result = 300f + (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanRange) * 50f);
            return false;
        }
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.GetScanInterval))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_GetScanInterval_Prefix(MapRoomFunctionality __instance, ref float __result)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;

            __result = Mathf.Max(0.1f, 14f - (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanSpeed) * 3f));
            return false;
        }
#elif BZ
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateScanRangeAndInterval))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_UpdateScanRangeAndInterval_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;


            var num = __instance.scanRange;
            __instance.scanRange =  300f + (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanRange) * 50f);
            __instance.scanInterval = Mathf.Max(0.1f, 14f - (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanSpeed) * 3f));
            if(Math.Abs(__instance.scanRange - num) < 0.001f)
                return false;
            __instance.ObtainResourceNodes(__instance.typeToScan);
            __instance.onScanRangeChanged?.Invoke();

            return false;
        }
#endif

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void LoadCache()
        {
            Dictionary<TechType, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>> TrackedResources = new();
            var CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            if(!File.Exists(CacheFilePath))
                return;
            var reader = new StreamReader(CacheFilePath);
            try
            {
                TrackedResources = JsonConvert.DeserializeObject<Dictionary<TechType, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>>>(
                    reader.ReadToEnd(),
                    new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented,
                        Converters = new List<JsonConverter>
                        {
                            new EntryPoint.TechTypeConverter()
                        }
                    }
                );
                reader.Close();
            }
            catch
            {
                reader.Close();
            }

            foreach(var pair in TrackedResources)
            {
                ResourceTrackerDatabase.resources[pair.Key] = pair.Value;
            }
        }


        [HarmonyPatch(typeof(Builder), nameof(Builder.CreateGhost))]
        [HarmonyPrefix]
        public static void Builder_CreateGhost_Prefix()
        {
            if(Builder.prefab is null || Builder.ghostModel is null || CraftData.GetTechType(Builder.prefab) != EntryPoint.ScannerMonitorTechType)
                return;


            if(GameInput.GetButtonDown(GameInput.Button.CyclePrev) || GameInput.GetButtonHeld(GameInput.Button.CyclePrev))
            {
                if (Builder.prefab.transform.localScale.x >= 2.65f) return;
                Builder.prefab.transform.localScale *= 1.01f;
                Object.DestroyImmediate(Builder.ghostModel);
                return;
            }

            if(GameInput.GetButtonDown(GameInput.Button.CycleNext) || GameInput.GetButtonHeld(GameInput.Button.CycleNext))
            {
                if (Builder.prefab.transform.localScale.x <= 0.5f) return;
                Builder.prefab.transform.localScale *= 0.99f;
                Object.DestroyImmediate(Builder.ghostModel);
                return;
            }

            if(GameInput.GetButtonDown(GameInput.Button.Deconstruct))
            {
                Builder.prefab.transform.localScale = Vector3.one;
                Object.DestroyImmediate(Builder.ghostModel);
                return;
            }


            var device = GameInput.GetPrimaryDevice();
            var msg1 = $"Press {GameInput.GetBinding(device, GameInput.Button.CyclePrev, GameInput.BindingSet.Primary)} to Enlarge Monitor";
            var msg2 = $"Press {GameInput.GetBinding(device,GameInput.Button.CycleNext, GameInput.BindingSet.Primary)} to Shrink Monitor";
            var msg3 = $"Press {GameInput.GetBinding(device, GameInput.Button.Deconstruct, GameInput.BindingSet.Primary)} to Reset Monitor Size";
            ErrorMessage._Message errorMessage = ErrorMessage.main.GetExistingMessage(msg1);
            ErrorMessage._Message errorMessage2 = ErrorMessage.main.GetExistingMessage(msg2);
            ErrorMessage._Message errorMessage3 = ErrorMessage.main.GetExistingMessage(msg3);

            ProcessMsg(errorMessage, msg1);
            ProcessMsg(errorMessage2, msg2);
            ProcessMsg(errorMessage3, msg3);
        }

        private static void ProcessMsg(ErrorMessage._Message errorMessage, string msg)
        {
            if (errorMessage != null)
            {
                errorMessage.messageText = msg;
                errorMessage.entry.text = msg;
                if (errorMessage.timeEnd <= Time.time + 1f)
                    errorMessage.timeEnd += Time.deltaTime;
                else
                    errorMessage.timeEnd = Time.time + 1f;
            }
            else
            {
                ErrorMessage.AddMessage(msg);
            }
        }
        
    }
}
#endif