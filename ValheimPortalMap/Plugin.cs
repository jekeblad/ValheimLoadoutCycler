using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

using ValheimPortalMap.UI;

namespace ValheimPortalMap
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "com.joakimekeblad.valheim.portalmap";
        public const string PluginName    = "PortalMap";
        public const string PluginVersion = "1.0.0";

        public static ManualLogSource Log { get; private set; } = null!;
        public static ConfigEntry<float> ClusterDistance { get; private set; } = null!;

        private bool _worldLoaded;

        private void Awake()
        {
            Log = Logger;

            ClusterDistance = Config.Bind(
                "UI", "ClusterDistance", 50f,
                new ConfigDescription("World-unit radius within which nearby portal pins are merged into one cluster pin",
                    new AcceptableValueRange<float>(0f, 500f)));

            gameObject.AddComponent<PortalListUI>();

            Application.quitting += PortalManager.Save;

            new Harmony(PluginGuid).PatchAll();
            Log.LogInfo($"{PluginName} v{PluginVersion} loaded");
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            var zdoMan = ZDOMan.instance;

            if (!_worldLoaded)
            {
                if (player != null && zdoMan != null)
                {
                    _worldLoaded = true;
                    PortalManager.Load();
                    PortalManager.Refresh();
                    Log.LogInfo("PortalMap: world loaded, portals synced");
                }
            }
            else if (player == null && zdoMan == null)
            {
                _worldLoaded = false;
                PortalManager.Clear();
            }
        }
    }
}
