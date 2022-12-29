#if !UNITY_EDITOR
namespace ScannerMonitor.Patches
{
    using System;
    using HarmonyLib;
    using UnityEngine;
    using System.Collections.Generic;
    using System.IO;
    using SMLHelper.V2.Utility;
    using Newtonsoft.Json;
    using FMOD.Studio;

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

        private static void Start(MapRoomFunctionality mapRoomFunctionality)
        {
            mapRoomFunctionality.wireFrameWorld.rotation = Quaternion.identity;
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
            MapRoomFunctionality.mapRooms.Add(mapRoomFunctionality);
            mapRoomFunctionality.Subscribe(true);
            mapRoomFunctionality.powerConsumer = mapRoomFunctionality.gameObject.EnsureComponent<PowerConsumer>();
            if(mapRoomFunctionality.powerConsumer)
            {
                mapRoomFunctionality.powered = !GameModeUtils.RequiresPower() || mapRoomFunctionality.powerConsumer.IsPowered();
            }
            else
            {
                mapRoomFunctionality.powered = true;
            }
            mapRoomFunctionality.screenRoot.SetActive(mapRoomFunctionality.powered);
            if(mapRoomFunctionality.powered)
            {
                mapRoomFunctionality.ambientSound.Play();
            }
        }        

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

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateScanning))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_UpdateScanning_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;

            DayNightCycle main = DayNightCycle.main;
            if(!main)
            {
                return true;
            }
            double timePassed = main.timePassed;
            if(__instance.timeLastScan + (double)__instance.scanInterval <= timePassed && __instance.powered)
            {
                __instance.timeLastScan = timePassed;
                __instance.UpdateBlips();
                __instance.UpdateCameraBlips();
                float num = __instance.scanRange * __instance.mapScale;
                if(__instance.prevFadeRadius != num)
                {
                    __instance.prevFadeRadius = num;
                }
            }
            if(__instance.scanActive != __instance.prevScanActive || __instance.scanInterval != __instance.prevScanInterval)
            {
                float num2 = 1f / __instance.scanInterval;
                __instance.prevScanActive = __instance.scanActive;
                __instance.prevScanInterval = __instance.scanInterval;
            }
            if(__instance.powered && __instance.timeLastPowerDrain + 1f < Time.time)
            {
                float num3;
                __instance.powerConsumer.ConsumePower(__instance.scanActive ? 0.5f : 0.15f, out num3);
                __instance.timeLastPowerDrain = Time.time;
            }
            return false;
        }

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.Update))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_Update_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
                return true;


            bool flag = __instance.powered;
            __instance.powered = __instance.powerConsumer.IsPowered();
            if(__instance.powered != flag)
            {
                if(__instance.powered)
                {
                    OnPowerUp(__instance);
                }
                else
                {
                   OnPowerDown(__instance);
                }
            }
            if(__instance.containerIsDirty)
            {
                __instance.UpdateScanRangeAndInterval();
                __instance.containerIsDirty = false;
            }
            __instance.UpdateScanning();
            //__instance.scannerCullable.SetActive(__instance.playerDistanceTracker.playerNearby);
            return false;
        }

        private static void OnPowerDown(MapRoomFunctionality instance)
        {
            instance.screenRoot.SetActive(false);
            instance.ambientSound.Stop(STOP_MODE.ALLOWFADEOUT);
        }

        private static void OnPowerUp(MapRoomFunctionality instance)
        {
            instance.screenRoot.SetActive(true);
            instance.timeLastScan = 0.0;
            instance.ambientSound.Play();
        }

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
                            new Main.TechTypeConverter()
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
            if(Builder.prefab is null || Builder.ghostModel is null || CraftData.GetTechType(Builder.prefab) != Main.ScannerMonitorTechType)
                return;


            if(GameInput.GetButtonDown(GameInput.Button.CyclePrev) || GameInput.GetButtonHeld(GameInput.Button.CyclePrev))
            {
                if (Builder.prefab.transform.localScale.x >= 2.65f) return;
                Builder.prefab.transform.localScale *= 1.01f;
                GameObject.DestroyImmediate(Builder.ghostModel);
                return;
            }

            if(GameInput.GetButtonDown(GameInput.Button.CycleNext) || GameInput.GetButtonHeld(GameInput.Button.CycleNext))
            {
                if (Builder.prefab.transform.localScale.x <= 0.5f) return;
                Builder.prefab.transform.localScale *= 0.99f;
                GameObject.DestroyImmediate(Builder.ghostModel);
                return;
            }

            if(GameInput.GetButtonDown(GameInput.Button.Deconstruct))
            {
                Builder.prefab.transform.localScale = Vector3.one;
                GameObject.DestroyImmediate(Builder.ghostModel);
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