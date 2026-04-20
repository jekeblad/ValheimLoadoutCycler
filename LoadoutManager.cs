using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ValheimLoadoutCycler.Data;

namespace ValheimLoadoutCycler
{
    public static class LoadoutManager
    {
        public static LoadoutSaveData Data { get; private set; } = new LoadoutSaveData();
        public static int ActiveIndex => Data.ActiveIndex;
        public static bool IsLoaded { get; private set; }

        private static string? _savePath;

        public static void Load(string characterName)
        {
            IsLoaded = true;
            _savePath = GetSavePath(characterName);
            Plugin.Log.LogInfo($"Loading loadouts from: {_savePath}");
            if (File.Exists(_savePath))
            {
                try
                {
                    string json = File.ReadAllText(_savePath);
                    Data = JsonConvert.DeserializeObject<LoadoutSaveData>(json) ?? new LoadoutSaveData();
                    while (Data.Loadouts.Count < 4)
                        Data.Loadouts.Add(new Loadout { Index = Data.Loadouts.Count });
                    if (Data.Loadouts.Count > 4)
                        Data.Loadouts.RemoveRange(4, Data.Loadouts.Count - 4);
                    Data.ActiveIndex = Math.Max(0, Math.Min(3, Data.ActiveIndex));
                    for (int i = 0; i < Data.Loadouts.Count; i++)
                        Plugin.Log.LogInfo($"  Loadout {i}: {Data.Loadouts[i].Items.Count} items");
                    Plugin.Log.LogInfo($"Loadouts loaded successfully, ActiveIndex={Data.ActiveIndex}");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Failed to load loadouts: {e.Message}");
                    Data = new LoadoutSaveData();
                }
            }
            else
            {
                Plugin.Log.LogWarning($"No save file found at {_savePath}, starting fresh");
                Data = new LoadoutSaveData();
            }
        }

        public static void ResetLoaded() => IsLoaded = false;

        public static void Save()
        {
            if (_savePath == null)
            {
                Plugin.Log.LogWarning("Save called but no save path set (player not spawned yet?)");
                return;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_savePath)!);
                File.WriteAllText(_savePath, JsonConvert.SerializeObject(Data, Formatting.Indented));
                Plugin.Log.LogInfo($"Loadouts saved to: {_savePath}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Failed to save loadouts: {e.Message}");
            }
        }

        public static void CycleNext()
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            int next = FindNextNonEmptyLoadout();
            if (next == -1) return;

            UnequipCurrentLoadout(player);
            Data.ActiveIndex = next;
            EquipLoadout(player, Data.Loadouts[Data.ActiveIndex]);
            Save();
        }

        private static int FindNextNonEmptyLoadout()
        {
            for (int i = 1; i <= 4; i++)
            {
                int idx = (Data.ActiveIndex + i) % 4;
                if (Data.Loadouts[idx].Items.Count > 0)
                    return idx;
            }
            return -1;
        }

        public static void ToggleItemInLoadout(int loadoutIndex, string prefabName, int quality)
        {
            var loadout = Data.Loadouts[loadoutIndex];
            var existing = loadout.Items.FirstOrDefault(s => s.PrefabName == prefabName && s.Quality == quality);
            if (existing != null)
                loadout.Items.Remove(existing);
            else
                loadout.Items.Add(new LoadoutSlot { PrefabName = prefabName, Quality = quality });
            Save();
        }

        public static bool IsItemInLoadout(int loadoutIndex, string prefabName, int quality)
        {
            return Data.Loadouts[loadoutIndex].Items.Any(s => s.PrefabName == prefabName && s.Quality == quality);
        }

        public static List<int> GetItemLoadoutIndices(string prefabName, int quality)
        {
            var result = new List<int>();
            for (int i = 0; i < Data.Loadouts.Count; i++)
            {
                if (Data.Loadouts[i].Items.Any(s => s.PrefabName == prefabName && s.Quality == quality))
                    result.Add(i);
            }
            return result;
        }

        public static int? GetItemLoadoutIndex(string prefabName, int quality)
        {
            for (int i = 0; i < Data.Loadouts.Count; i++)
            {
                if (Data.Loadouts[i].Items.Any(s => s.PrefabName == prefabName && s.Quality == quality))
                    return i;
            }
            return null;
        }

        private static void EquipLoadout(Player player, Loadout loadout)
        {
            foreach (var slot in loadout.Items)
            {
                var item = FindItem(player.GetInventory(), slot);
                if (item != null && !item.m_equipped)
                    player.EquipItem(item);
            }
        }

        private static void UnequipCurrentLoadout(Player player)
        {
            var currentLoadout = Data.Loadouts[Data.ActiveIndex];
            foreach (var slot in currentLoadout.Items)
            {
                var item = FindItem(player.GetInventory(), slot);
                if (item != null && item.m_equipped)
                    player.UnequipItem(item, triggerEquipEffects: false);
            }
        }

        private static ItemDrop.ItemData? FindItem(Inventory inv, LoadoutSlot slot)
        {
            return inv.GetAllItems()
                .Where(i => i.m_dropPrefab?.name == slot.PrefabName)
                .OrderBy(i => Math.Abs(i.m_quality - slot.Quality))
                .FirstOrDefault();
        }

        private static string GetSavePath(string characterName)
        {
            string valheimSave = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..", "LocalLow", "IronGate", "Valheim", "characters");
            return Path.Combine(valheimSave, $"{characterName}_loadouts.json");
        }
    }
}
