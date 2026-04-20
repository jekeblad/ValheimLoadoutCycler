using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ValheimLoadoutCycler.UI;

namespace ValheimLoadoutCycler
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.joakimekeblad.valheim.loadoutcycler";
        public const string PluginName = "LoadoutCycler";
        public const string PluginVersion = "1.0.0";

        public static ManualLogSource Log { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> CycleKey { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ConfigKey { get; private set; } = null!;
        public static ConfigEntry<float> OutlineThickness { get; private set; } = null!;

        public static bool IsCycleKeyDown()
        {
            var k = CycleKey.Value;
            return Input.GetKeyDown(k.MainKey) && AreModifiersHeld(k) && !IsConfigKeyDown();
        }

        public static bool IsConfigKeyDown()
        {
            var k = ConfigKey.Value;
            return Input.GetKeyDown(k.MainKey) && AreModifiersHeld(k);
        }

        private static bool AreModifiersHeld(KeyboardShortcut shortcut)
        {
            foreach (var mod in shortcut.Modifiers)
                if (!Input.GetKey(mod)) return false;
            return true;
        }

        private InventoryOverlay _overlay = null!;

        private void Update()
        {
            if (LoadoutManager.IsLoaded) return;
            var player = Player.m_localPlayer;
            if (player == null) return;
            var name = player.GetPlayerName();
            if (string.IsNullOrEmpty(name)) return;
            Log.LogInfo($"Plugin.Update: loading for '{name}'");
            LoadoutManager.Load(name);
        }

        private void Awake()
        {
            Log = Logger;

            CycleKey = Config.Bind("Keybinds", "CycleLoadout", new KeyboardShortcut(KeyCode.H),
                "Key to cycle to the next loadout");
            ConfigKey = Config.Bind("Keybinds", "ConfigMode", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Key to toggle loadout config mode (hold Ctrl+H)");
            OutlineThickness = Config.Bind("UI", "OutlineThickness", 2f,
                new BepInEx.Configuration.ConfigDescription("Border width of selected inventory slots in config mode",
                    new BepInEx.Configuration.AcceptableValueRange<float>(1f, 8f)));

            _overlay = gameObject.AddComponent<InventoryOverlay>();

            Application.quitting += LoadoutManager.Save;

            new Harmony(PluginGuid).PatchAll();
            Log.LogInfo($"{PluginName} loaded");
        }
    }
}
