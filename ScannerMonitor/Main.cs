#if !UNITY_EDITOR
namespace ScannerMonitor
{
    using System;
    using HarmonyLib;
    using System.IO;
    using Game_Items;
    using SMLHelper.V2.Utility;
    using System.Collections.Generic;
    using SMLHelper.V2.Handlers;
    using System.Globalization;
    using Newtonsoft.Json;
    using BepInEx;

    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main: BaseUnityPlugin
    {
        #region[Declarations]
        public const string
            MODNAME = "ScannerMonitor",
            AUTHOR = "MrPurple6411",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0.0";
        #endregion
        public static TechType ScannerMonitorTechType { get; private set; }
        
        public void Awake()
        {
            var scannerMonitor = new ScannerMonitor();
            scannerMonitor.Patch();
            ScannerMonitorTechType = scannerMonitor.TechType;
            Harmony.CreateAndPatchAll(typeof(Patches.Patches), GUID);
            IngameMenuHandler.RegisterOnSaveEvent(SaveCache);

        }

        public static void SaveCache()
        {
            var TrackedResources = ResourceTrackerDatabase.resources;


            var CacheFilePath = Path.Combine(SaveUtils.GetCurrentSaveDataDir(), "FoundScanTypes.json");
            var writer = new StreamWriter(CacheFilePath);
            try
            {
                writer.Write(
                    JsonConvert.SerializeObject(
                        TrackedResources, new JsonSerializerSettings()
                        {
                            Formatting = Formatting.Indented,
                            Converters = new List<JsonConverter>
                            {
                                new TechTypeConverter()
                            },
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

        public class TechTypeConverter: JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, ((TechType)value).AsString());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var v = (string)serializer.Deserialize(reader, typeof(string));
                return TechTypeExtensions.FromString(v, out var techType, true)
                    ? techType
                    : TechTypeHandler.TryGetModdedTechType(v, out techType)
                        ? (object)techType
                        : throw new Exception($"Failed to parse {v} into a TechType");
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(TechType);
            }
        }
    }
}
#endif