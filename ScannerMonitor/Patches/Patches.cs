#if !UNITY_EDITOR
namespace ScannerMonitor.Patches
{
    using HarmonyLib;
    using UnityEngine;

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.OnPostRebuildGeometry))]
        [HarmonyPrefix]
        public static bool MapRoomFunctionality_OnPostRebuildGeometry_Prefix(MapRoomFunctionality __instance, Base b)
        {
            return !__instance.gameObject.name.Contains("ScannerMonitor");
        }

        [HarmonyPatch(typeof(Builder), nameof(Builder.CreateGhost))]
        [HarmonyPrefix]
        public static void Builder_CreateGhost_Prefix()
        {
            if(Builder.prefab is null || Builder.ghostModel is null || CraftData.GetTechType(Builder.prefab) != EntryPoint.ScannerMonitor.TechType)
                return;


            if(GameInput.GetButtonDown(GameInput.Button.CycleNext) || GameInput.GetButtonHeld(GameInput.Button.CycleNext))
            {
                Builder.prefab.transform.localScale *= 1.01f;
                GameObject.DestroyImmediate(Builder.ghostModel);
                return;
            }

            if(GameInput.GetButtonDown(GameInput.Button.CyclePrev) || GameInput.GetButtonHeld(GameInput.Button.CyclePrev))
            {
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


            string msg1 = $"Press {GameInput.GetBinding(GameInput.GetPrimaryDevice(), GameInput.Button.CycleNext, GameInput.BindingSet.Primary)} to Enlarge Monitor";
            ErrorMessage._Message emsg = ErrorMessage.main.GetExistingMessage(msg1);
            string msg2 = $"Press {GameInput.GetBinding(GameInput.GetPrimaryDevice(),GameInput.Button.CyclePrev, GameInput.BindingSet.Primary)} to Shrink Monitor";
            ErrorMessage._Message emsg2 = ErrorMessage.main.GetExistingMessage(msg2);
            string msg3 = $"Press {GameInput.GetBinding(GameInput.GetPrimaryDevice(), GameInput.Button.Deconstruct, GameInput.BindingSet.Primary)} to Reset Monitor Size";
            ErrorMessage._Message emsg3 = ErrorMessage.main.GetExistingMessage(msg3);

            if(emsg != null)
            {
                emsg.messageText = msg1;
                emsg.entry.text = msg1;
                if(emsg.timeEnd <= Time.time + 1f)
                    emsg.timeEnd += Time.deltaTime;
                else
                    emsg.timeEnd = Time.time + 1f;
            }
            else
            {
                ErrorMessage.AddMessage(msg1);
            }

            if(emsg2 != null)
            {
                emsg2.messageText = msg2;
                emsg2.entry.text = msg2;

                if(emsg2.timeEnd <= Time.time + 1f)
                    emsg2.timeEnd += Time.deltaTime;
                else
                    emsg2.timeEnd = Time.time + 1f;
            }
            else
            {
                ErrorMessage.AddMessage(msg2);
            }

            if(emsg3 != null)
            {
                emsg3.messageText = msg3;
                emsg3.entry.text = msg3;

                if(emsg3.timeEnd <= Time.time + 1f)
                    emsg3.timeEnd += Time.deltaTime;
                else
                    emsg3.timeEnd = Time.time + 1f;
            }
            else
            {
                ErrorMessage.AddMessage(msg3);
            }
        }
    }
}
#endif