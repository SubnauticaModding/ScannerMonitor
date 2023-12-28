#if !UNITY_EDITOR && (SUBNAUTICA || BELOWZERO)
namespace ScannerMonitor.Game_Items
{
    using System.Collections.Generic;
    using System.IO;
    using Nautilus.Assets;
    using Nautilus.Crafting;
    using Nautilus.Handlers;
    using Nautilus.Utility;
    using UnityEngine;
    using System.Reflection;
#if SUBNAUTICA
    using Sprite = Atlas.Sprite;
    using UWE;
    using static CraftData;
    using Nautilus.Assets.Gadgets;
#endif

    public static class ScannerMonitor
    {
        public static TechType TechType { get; private set; }

        public static TechGroup GroupForPDA => TechGroup.InteriorModules;
        public static TechCategory CategoryForPDA => TechCategory.InteriorModule;

        public static TechType RequiredForUnlock => TechType.BaseMapRoom;

        private const string CLASS_ID = "ScannerMonitor";
        private const string NICE_NAME = "Scanner Monitor";
        private const string DESCRIPTION = "An advanced Scanner Room compressed into a smaller wall mounted screen.";

        private const string ASSET_BUNDLE_NAME =
#if SUBNAUTICA
            "scannermonitor";
#else
            "scannermonitorbz";
#endif

        private static readonly string modFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string assetsFolderPath = Path.Combine(modFolderPath, "Assets");
        private static readonly string assetBundlePath = Path.Combine(assetsFolderPath, ASSET_BUNDLE_NAME);
        private static GameObject processedPrefab;

        private static readonly AssetBundle AssetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        public static GameObject GetGameObject()
        {
            if (processedPrefab != null)
                return processedPrefab;

            processedPrefab = AssetBundle.LoadAsset<GameObject>("ScannerMonitorModel");
            Shader MarmosetUBER = Shader.Find("MarmosetUBER");
            foreach (Renderer renderer in processedPrefab.GetComponentsInChildren<Renderer>())
                renderer.material.shader = MarmosetUBER;
            foreach (var uid in processedPrefab.GetComponentsInChildren<UniqueIdentifier>())
                uid.classId = CLASS_ID;
            processedPrefab.GetComponent<Constructable>().techType = TechType;
            processedPrefab.SetActive(false);

            return processedPrefab;
        }

        private static RecipeData GetBlueprintRecipe()
        {
            TechType tech = EnumHandler.TryGetValue("Kit_BaseMapRoom", out TechType techType) && techType != TechType.None ? techType : TechType.AdvancedWiringKit;

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

            RecipeData data = CraftDataHandler.GetRecipeData(TechType.BaseMapRoom);
            data.Ingredients.Add(new Ingredient(tech, 1));

            return data;

        }

        private static Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(assetsFolderPath, "ScannerMonitor.png"));
        }

        internal static void CreateAndRegister()
        {
            var info = PrefabInfo.WithTechType(CLASS_ID, NICE_NAME, DESCRIPTION, "English", false).WithIcon(GetItemSprite());
            TechType = info.TechType;

            var customPrefab = new CustomPrefab(info);

            customPrefab.SetUnlock(RequiredForUnlock).WithPdaGroupCategory(GroupForPDA, CategoryForPDA);
            customPrefab.AddGadget(new CraftingGadget(customPrefab, GetBlueprintRecipe()).WithCraftingTime(5f));
            customPrefab.SetGameObject(GetGameObject());

            customPrefab.Register();
            
            WorldEntityDatabaseHandler.AddCustomInfo(CLASS_ID, new WorldEntityInfo()
            {
                cellLevel = LargeWorldEntity.CellLevel.Global,
                classId = CLASS_ID,
                localScale = Vector3.one,
                prefabZUp = true,
                slotType = EntitySlot.Type.Small,
                techType = TechType
            });
        }
    }
}
#endif