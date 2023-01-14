#if !UNITY_EDITOR
namespace ScannerMonitor.Game_Items
{
    using System.Collections.Generic;
    using System.IO;
    using SMLHelper.Assets;
    using SMLHelper.Crafting;
    using SMLHelper.Handlers;
    using SMLHelper.Utility;
    using UnityEngine;
    using System.Reflection;
#if SN1
    using RecipeData = SMLHelper.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

    public class ScannerMonitor : Buildable
    {
        public override TechGroup GroupForPDA => TechGroup.InteriorModules;
        public override TechCategory CategoryForPDA => TechCategory.InteriorModule;

        public override TechType RequiredForUnlock => TechType.BaseMapRoom;

        private const string CLASS_ID = "ScannerMonitor";
        private const string NICE_NAME = "Scanner Tracking Screen";
        private const string DESCRIPTION = "Small Scale Scanner Module.";

        private const string ASSET_BUNDLE_NAME =
#if SN1
            "scannermonitor";
#elif BZ
            "scannermonitorbz";
#endif

        private static readonly string modFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string assetsFolderPath = Path.Combine(modFolderPath, "Assets");
        private static readonly string assetBundlePath = Path.Combine(assetsFolderPath , ASSET_BUNDLE_NAME);
        private static GameObject processedPrefab;
        
        private static readonly AssetBundle AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
        public ScannerMonitor() : base(CLASS_ID, NICE_NAME, DESCRIPTION) { }

        public override GameObject GetGameObject()
        {
            if (processedPrefab is not null) return Object.Instantiate(processedPrefab);
            
            processedPrefab = AssetBundle.LoadAsset<GameObject>("ScannerMonitorModel");
            Shader MarmosetUBER = Shader.Find("MarmosetUBER");
            foreach (Renderer renderer in processedPrefab.GetComponentsInChildren<Renderer>())
                renderer.material.shader = MarmosetUBER;
            foreach(var uid in processedPrefab.GetComponentsInChildren<UniqueIdentifier>())
                uid.classId = ClassID;
            processedPrefab.GetComponent<Constructable>().techType = this.TechType;
            processedPrefab.GetComponent<TechTag>().type = this.TechType;
            
            return Object.Instantiate(processedPrefab);
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType tech = TechTypeHandler.TryGetModdedTechType("Kit_BaseMapRoom", out TechType techType) ? techType : TechType.AdvancedWiringKit;
            if (tech != TechType.AdvancedWiringKit)
                return new RecipeData()
                {
                    craftAmount = 1,
                    Ingredients = new List<Ingredient>()
                    {
                        new Ingredient(tech, 1),
                        new Ingredient(TechType.AdvancedWiringKit, 1)
                    }
                };
            RecipeData data = CraftDataHandler.GetTechData(TechType.BaseMapRoom);
            data.Ingredients.Add(new Ingredient(tech, 1));

            return data;

        }
        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(assetsFolderPath, "ScannerMonitor.png"));
        }
    }
}
#endif