#if !UNITY_EDITOR
namespace ScannerMonitor.Game_Items
{
    using System.Collections.Generic;
    using System.IO;
    using SMLHelper.V2.Assets;
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    using SMLHelper.V2.Utility;
    using UnityEngine;
    using System.Reflection;
#if SN1
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

    public class ScannerMonitor : Buildable
    {
        public override TechGroup GroupForPDA => TechGroup.InteriorModules;
        public override TechCategory CategoryForPDA => TechCategory.InteriorModule;

        public override TechType RequiredForUnlock => TechType.ScannerRoomBlueprint;

        private const string CLASS_ID = "ScannerMonitor";
        private const string NICE_NAME = "Scanner Tracking Screen";
        private const string DESCRIPTION = "Small Scale Scanner Module.";

        private static readonly string modFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string assetsFolderPath = Path.Combine(modFolderPath, "Assets");
        private static readonly string assetBundlePath = Path.Combine(assetsFolderPath , "scannermonitor");
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
                        new Ingredient(TechType.AdvancedWiringKit, 2)
                    }
                };
            RecipeData data = CraftDataHandler.GetTechData(TechType.BaseMapRoom);
            data.Ingredients.Add(new Ingredient(tech, 2));

            return data;

        }
        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(assetsFolderPath, "ScannerMonitor.png"));
        }
    }
}
#endif