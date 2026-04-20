using HarmonyLib;

namespace ValheimLoadoutCycler.Patches
{
    [HarmonyPatch(typeof(InventoryGui))]
    public static class InventoryGuiPatch
    {
        [HarmonyPatch(nameof(InventoryGui.Hide))]
        [HarmonyPostfix]
        static void Hide()
        {
            ConfigMode.ExitOnInventoryClose();
        }

        [HarmonyPatch("OnRightClickItem")]
        [HarmonyPrefix]
        static bool OnRightClickItem(InventoryGrid grid, ItemDrop.ItemData? item)
        {
            if (!ConfigMode.IsActive) return true;
            if (item == null) return false;

            string? prefabName = item.m_dropPrefab?.name;
            if (prefabName == null) return false;

            LoadoutManager.ToggleItemInLoadout(ConfigMode.EditingLoadoutIndex, prefabName, item.m_quality);
            return false;
        }
    }
}
