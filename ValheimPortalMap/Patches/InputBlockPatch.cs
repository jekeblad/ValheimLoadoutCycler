using HarmonyLib;
using ValheimPortalMap.UI;

namespace ValheimPortalMap.Patches
{
    // Prevent the minimap from zooming while the mouse is over the portal panel.
    // UpdateMap is where m_largeZoom is modified from scroll input.
    [HarmonyPatch(typeof(Minimap), "UpdateMap")]
    static class BlockMinimapZoomWhenOverPanel
    {
        static float _savedZoom;
        static bool  _blocking;

        static void Prefix(Minimap __instance)
        {
            _blocking = false;
            if (!PortalListUI.IsMouseOverPanel) return;
            if (__instance.m_mode != Minimap.MapMode.Large) return;
            float z = Traverse.Create(__instance).Field<float>("m_largeZoom").Value;
            if (z > 0f) { _savedZoom = z; _blocking = true; }
        }

        static void Postfix(Minimap __instance)
        {
            if (!_blocking) return;
            Traverse.Create(__instance).Field("m_largeZoom").SetValue(_savedZoom);
        }
    }
}
