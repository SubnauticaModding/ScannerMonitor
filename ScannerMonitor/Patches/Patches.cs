#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
namespace ScannerMonitor.Patches
{
    using System;
    using HarmonyLib;
    using UnityEngine;
    using System.Collections.Generic;
    using System.IO;
    using Nautilus.Utility;
    using Newtonsoft.Json;
    using System.Collections;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using System.Linq;

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.Start))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_Start_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor"))
            {
                return true;
            }

            Start(__instance);
            return false;
        }

        private static Material mapRoomMaterial;
        private static GameObject blipPrefab;
        private static GameObject cameraBlipPrefabObject;
        private static MapRoomCameraBlip cameraBlipPrefab;
        private static GameObject roomBlipPrefabObject;

        private static void Start(MapRoomFunctionality mapRoomFunctionality)
        {
            Main.Logger.LogDebug("MapRoomFunctionality.Start");
            if(mapRoomMaterial == null || blipPrefab == null || cameraBlipPrefabObject == null || cameraBlipPrefab == null || roomBlipPrefabObject == null)
            {
                GameObject FunctionalityPrefab = AddressablesUtility.LoadAsync<GameObject>("Submarine/Build/MapRoomFunctionality.prefab").WaitForCompletion();
                MapRoomFunctionality functionality = FunctionalityPrefab.GetComponentInChildren<MapRoomFunctionality>(true);
                if(functionality != null)
                {
                    mapRoomMaterial = Material.Instantiate(functionality.miniWorld.hologramMaterial);
                    blipPrefab = EditorModifications.Instantiate(functionality.blipPrefab, default, default, false);
                    cameraBlipPrefabObject = EditorModifications.Instantiate(functionality.cameraBlipPrefab.gameObject, default, default, false);
                    roomBlipPrefabObject = EditorModifications.Instantiate(functionality.roomBlip.gameObject, default, default, false);
                    cameraBlipPrefab = cameraBlipPrefabObject.GetComponent<MapRoomCameraBlip>();
                    Main.Logger.LogMessage($"required objects: {mapRoomMaterial != null} : {blipPrefab != null} : {cameraBlipPrefabObject != null} : {cameraBlipPrefab != null}");
                }
                else
                {
                    Main.Logger.LogError($"Failed to find MapRoomFunctionality!");
                }
            }

            if(mapRoomMaterial != null && blipPrefab != null && cameraBlipPrefabObject != null && cameraBlipPrefab != null || roomBlipPrefabObject != null)
            {
                mapRoomFunctionality.miniWorld.hologramMaterial = mapRoomMaterial;
                if(mapRoomFunctionality.miniWorld.materialInstance != null)
                {
                    var mat = mapRoomFunctionality.miniWorld.materialInstance;
                    mat.shader = mapRoomMaterial.shader;
                    mat.globalIlluminationFlags = mapRoomMaterial.globalIlluminationFlags;
                    mat.CopyFields(mapRoomMaterial);
                    mat.CopyPropertiesFromMaterial(mapRoomMaterial);
                }
                mapRoomFunctionality.blipPrefab = blipPrefab;
                mapRoomFunctionality.cameraBlipPrefab = cameraBlipPrefab;
                GameObject roomBlipObject = EditorModifications.Instantiate(roomBlipPrefabObject, mapRoomFunctionality.cameraBlipRoot.transform, default, default, true);
                roomBlipObject.transform.localScale = Vector3.one * (500f / mapRoomFunctionality.miniWorld.mapWorldRadius);
                mapRoomFunctionality.roomBlip = roomBlipObject.GetComponent<MapRoomCameraBlip>();
            }
            else
            {
                Main.Logger.LogError($"Failed to find all required objects! {mapRoomMaterial != null} : {blipPrefab != null} : {cameraBlipPrefabObject != null} : {cameraBlipPrefab != null}");
            }

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
            mapRoomFunctionality.powered = !GameModeUtils.RequiresPower() || mapRoomFunctionality.powerConsumer.IsPowered();
            mapRoomFunctionality.screenRoot.SetActive(mapRoomFunctionality.powered);
            mapRoomFunctionality.worlddisplay.SetActive(mapRoomFunctionality.powered);
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
            __instance.scanRange = 300f + (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanRange) * 50f);
            __instance.scanInterval = Mathf.Max(0.1f, 14f - (__instance.storageContainer.container.GetCount(TechType.MapRoomUpgradeScanSpeed) * 3.5f));
            if(__instance.scanRange != num)
            {
                __instance.ObtainResourceNodes(__instance.typeToScan);
                __instance.onScanRangeChanged?.Invoke();
            }

            if(__instance.roomBlip && __instance.roomBlip.gameObject != null && !__instance.roomBlip.gameObject.activeSelf)
            {
                __instance.roomBlip.gameObject.SetActive(true);
            }

            return false;
        }

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateScanning))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_UpdateScanning_Prefix(MapRoomFunctionality __instance)
        {
            if(!__instance.gameObject.name.Contains("ScannerMonitor") || DayNightCycle.main == null)
                return true;

            Material materialInstance = __instance.miniWorld.materialInstance;
            double timePassed = DayNightCycle.main.timePassed;
            if(__instance.timeLastScan + (double)__instance.scanInterval <= timePassed && __instance.powered)
            {
                __instance.timeLastScan = timePassed;
                if(__instance.scanInterval < 1f)
                    __instance.numNodesScanned = __instance.resourceNodes.Count;
                __instance.UpdateBlips();
                __instance.UpdateCameraBlips();
                float num = __instance.scanRange * __instance.mapScale;
                if(__instance.prevFadeRadius != num)
                {
                    materialInstance.SetFloat(ShaderPropertyID._FadeRadius, num);
                    __instance.prevFadeRadius = num;
                }
            }
            if(__instance.scanActive != __instance.prevScanActive || __instance.scanInterval != __instance.prevScanInterval)
            {
                float num2 = 1f / Mathf.Max(1f, __instance.scanInterval);
                materialInstance.SetFloat(ShaderPropertyID._ScanIntensity, __instance.scanActive ? __instance.scanIntensity : 0f);
                materialInstance.SetFloat(ShaderPropertyID._ScanFrequency, __instance.scanActive ? num2 : 0f);
                __instance.prevScanActive = __instance.scanActive;
                __instance.prevScanInterval = __instance.scanInterval;
            }
            if(__instance.typeToScan != TechType.None && __instance.powered && __instance.timeLastPowerDrain + 1f < Time.time)
            {
                __instance.powerConsumer.ConsumePower(0.05f, out _);
                __instance.timeLastPowerDrain = Time.time;
            }
            return false;
        }

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.mapScale), MethodType.Getter)]
        [HarmonyPostfix]
        private static void MapScalePostfix(MapRoomFunctionality __instance, ref float __result)
        {
            if(!__instance.gameObject.name.StartsWith("ScannerMonitor"))
                return;

            __result = __instance.hologramRadius / __instance.miniWorld.mapWorldRadius;
        }

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateBlips))]
        [HarmonyPostfix]
        public static void UpdateBlipsPostfix(MapRoomFunctionality __instance)
        {
            if(__instance.scanActive && __instance.gameObject.name.StartsWith("ScannerMonitor"))
            {
                for(int i = 0; i < __instance.mapBlips.Count; i++)
                {
                    __instance.mapBlips[i].transform.localScale = Vector3.one * (500f / __instance.miniWorld.mapWorldRadius);
                }
            }
        }

        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateCameraBlips))]
        [HarmonyPostfix]
        public static void UpdateCameraBlipsPostfix(MapRoomFunctionality __instance)
        {
            if(__instance.gameObject.name.StartsWith("ScannerMonitor"))
            {
                for(int i = 0; i < __instance.cameraBlips.Count; i++)
                {
                    __instance.cameraBlips[i].transform.localScale = Vector3.one * (500f / __instance.miniWorld.mapWorldRadius);
                }
            }
        }

        [HarmonyPatch(typeof(MiniWorld), nameof(MiniWorld.RebuildHologram))]
        [HarmonyPrefix]
        private static bool Prefix(MiniWorld __instance)
        {
            if(__instance.gameObject?.name != "MapRoot")
                return true;

            return false;
        }

        [HarmonyPatch(typeof(MiniWorld), nameof(MiniWorld.RebuildHologram))]
        [HarmonyPostfix]
        public static IEnumerator Postfix(IEnumerator result, MiniWorld __instance)
        {
            if(__instance.gameObject?.name != "MapRoot")
            { 
                yield return result;
                yield break;
            }
            Vector3 position = __instance.gameObject.transform.position;
            bool ranOnce = false;
            Vector3 newPosition = default;

            bool isPickupable = __instance.gameObject.GetComponentInParent<Pickupable>() != null;
            while(!(__instance == null))
            {
                newPosition = __instance.gameObject.transform.position;
                if(ranOnce && newPosition == position)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                Main.Logger.LogDebug($"Rebuilding Map for Scanner Monitor at {newPosition.x}, {newPosition.y}, {newPosition.z}");

                position = newPosition;
                if(!__instance.gameObject.activeInHierarchy || (isPickupable && __instance.gameObject.GetComponentInParent<Player>() == null))
                {
                    __instance.ClearAllChunks();
                }
                else if(__instance.gameObject.activeInHierarchy)
                {
                    Int3 block = LargeWorldStreamer.main.GetBlock(__instance.gameObject.transform.position);
                    Int3 u = block - __instance.mapWorldRadius;
                    Int3 u2 = block + __instance.mapWorldRadius;
                    block >>= 2;
                    Int3 mins = (u >> 2) / 32;
                    Int3 maxs = (u2 >> 2) / 32;
                    bool chunkAdded = false;
                    Int3.RangeEnumerator iter = Int3.Range(mins, maxs);
                    List<AsyncOperationHandle<Mesh>> requests = new List<AsyncOperationHandle<Mesh>>();
                    HashSet<string> loadingChunks = new HashSet<string>();
                    while(iter.MoveNext())
                    {
                        Int3 chunkId = iter.Current;
                        __instance.requestChunks.Add(chunkId);
                        if(!__instance.GetChunkExists(chunkId))
                        {
                            string chunkPath = __instance.GetChunkFilename(chunkId);
                            if(!AddressablesUtility.Exists<Mesh>(chunkPath) || loadingChunks.Any(r => r == chunkPath))
                            {
                                continue;
                            }
                            AsyncOperationHandle<Mesh> request = AddressablesUtility.LoadAsync<Mesh>(chunkPath);
                            request.Completed += (AsyncOperationHandle<Mesh> obj) =>
                            {
                                if(__instance == null)
                                {
                                    AddressablesUtility.QueueRelease<Mesh>(ref request);
                                    return;
                                }
                                if(request.Status == AsyncOperationStatus.Failed || __instance.GetChunkExists(chunkId))
                                {
                                    AddressablesUtility.QueueRelease<Mesh>(ref request);
                                    loadingChunks.Remove(chunkPath);
                                    return;
                                }
                                __instance.GetOrMakeChunk(chunkId, request, chunkPath);
                                chunkAdded = true;
                            };
                            requests.Add(request);
                        }
                    }

                    while(requests.Any(r => !r.IsDone))
                        yield return null;

                    __instance.ClearUnusedChunks(__instance.requestChunks);
                    __instance.requestChunks.Clear();
                    if(chunkAdded)
                    {
                        __instance.UpdatePosition();
                    }
                    Main.Logger.LogDebug($"Rebuilding Map for Scanner Monitor Complete at {newPosition.x}, {newPosition.y}, {newPosition.z}");
                }
                ranOnce = true;
                yield return new WaitForSeconds(1f);
            }
            yield break;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        [HarmonyPostfix]
        public static void LoadCache()
        {
            var CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            if(!File.Exists(CacheFilePath))
                return;
            var reader = new StreamReader(CacheFilePath);
            try
            {
                Dictionary<string, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>> stringifiedTrackedResources = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, ResourceTrackerDatabase.ResourceInfo>>>(
                    reader.ReadToEnd(),
                    new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented,
                    }
                );
                reader.Close();

                foreach(var pair in stringifiedTrackedResources)
                {
                    if(Enum.TryParse(pair.Key, out TechType techType))
                        ResourceTrackerDatabase.resources[techType] = pair.Value;
                }
            }
            catch(Exception ex)
            {
                Main.Logger.LogError(ex);
                reader.Close();
            }
        }
    }
}
#endif