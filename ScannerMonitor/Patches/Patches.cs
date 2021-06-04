#if !UNITY_EDITOR
namespace ScannerMonitor.Patches
{
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.Start))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_OnPostRebuildGeometry_Prefix(MapRoomFunctionality __instance)
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
            mapRoomFunctionality.matInstance = UnityEngine.Object.Instantiate<Material>(mapRoomFunctionality.mat);
            mapRoomFunctionality.matInstance.SetFloat(ShaderPropertyID._ScanIntensity, 0f);
            mapRoomFunctionality.matInstance.SetVector(ShaderPropertyID._MapCenterWorldPos, mapRoomFunctionality.transform.position);
            MapRoomFunctionality.mapRooms.Add(mapRoomFunctionality);
            mapRoomFunctionality.Subscribe(true);
            mapRoomFunctionality.powerRelay = mapRoomFunctionality.GetComponentInParent<PowerRelay>();
            bool flag;
            if(mapRoomFunctionality.powerRelay)
            {
                flag = (!GameModeUtils.RequiresPower() || mapRoomFunctionality.powerRelay.IsPowered());
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