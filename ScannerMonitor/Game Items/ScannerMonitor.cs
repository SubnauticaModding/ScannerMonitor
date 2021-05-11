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
#if SN1
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif


    /**
    * The generic resource monitor object that will be shared among all different types of the resource monitor.
    */
    public class ScannerMonitor : Buildable
    {
        public override TechGroup GroupForPDA => TechGroup.InteriorModules;
        public override TechCategory CategoryForPDA => TechCategory.InteriorModule;

        public override TechType RequiredForUnlock => TechType.ScannerRoomBlueprint;

        public static readonly string CLASS_ID = "ScannerMonitor";
        public static readonly string NICE_NAME = "Scanner Tracking Screen";
        public static readonly string DESCRIPTION = "Small Scale Scanner Module.";

        public ScannerMonitor() : base(CLASS_ID, NICE_NAME, DESCRIPTION)
        {
        }

        public override GameObject GetGameObject()
        {
            GameObject screen = Object.Instantiate(EntryPoint.SCANNER_MONITOR_DISPLAY_MODEL);

            foreach(Renderer renderer in screen.GetComponentsInChildren<Renderer>())
                renderer.material.shader = Shader.Find("MarmosetUBER");
            foreach(var uid in screen.GetComponentsInChildren<UniqueIdentifier>())
                uid.classId = ClassID;

            screen.GetComponent<Constructable>().techType = this.TechType;
            screen.GetComponent<TechTag>().type = this.TechType;

            return screen;
        }

        protected override RecipeData GetBlueprintRecipe()
        {
            TechType tech = TechTypeHandler.TryGetModdedTechType("Kit_BaseMapRoom", out TechType techType) ? techType : TechType.AdvancedWiringKit;
            if(tech == TechType.AdvancedWiringKit)
            {
                RecipeData data = CraftDataHandler.GetTechData(TechType.BaseMapRoom);
                data.Ingredients.Add(new Ingredient(tech, 2));

                return data;
            }

            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                    new Ingredient(tech, 1),
                    new Ingredient(TechType.AdvancedWiringKit, 2)
                }
            };
        }
        protected override Sprite GetItemSprite()
        {
            return ImageUtils.LoadSpriteFromFile(Path.Combine(EntryPoint.MOD_FOLDER_LOCATION, "Assets/ScannerMonitor.png"));
        }
    }
}
#endif