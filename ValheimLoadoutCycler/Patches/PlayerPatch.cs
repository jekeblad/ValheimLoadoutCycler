using HarmonyLib;

namespace ValheimLoadoutCycler.Patches
{
    [HarmonyPatch(typeof(Player))]
    public static class PlayerPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void Update(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;

            if (Plugin.IsConfigKeyDown())
                ConfigMode.Toggle();

            if (InventoryGui.IsVisible()) return;

            if (Plugin.IsCycleKeyDown())
                LoadoutManager.CycleNext();
        }

        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        static void OnDestroy(Player __instance)
        {
            if (__instance != Player.m_localPlayer) return;
            LoadoutManager.Save();
            LoadoutManager.ResetLoaded();
        }
    }
}
