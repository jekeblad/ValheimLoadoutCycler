using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using ValheimPortalMap.Data;

namespace ValheimPortalMap
{
    public class PortalInfo
    {
        public string ZdoKey    { get; set; } = "";
        public string Name      { get; set; } = "";
        public Vector3 Position { get; set; }
        public bool ShowOnMap   { get; set; }
        public Color PinColor   { get; set; } = Color.white;

        // Set by PortalManager after pin sync — null when not shown or clustered with others
        public Minimap.PinData? Pin { get; set; }
    }

    public class PortalGroup
    {
        public string Name { get; set; } = "";
        public List<PortalInfo> Portals { get; } = new List<PortalInfo>();
    }

    public class PortalCluster
    {
        public List<PortalInfo> Members { get; } = new List<PortalInfo>();
        public Minimap.PinData? Pin     { get; set; }

        public Vector3 Centroid
        {
            get
            {
                var c = Vector3.zero;
                foreach (var m in Members) c += m.Position;
                return c / Members.Count;
            }
        }

        public string PinLabel =>
            Members.Count == 1
                ? Members[0].Name
                : string.Join(" / ", Members.Select(m => m.Name));

        public Color PinColor =>
            Members.Count == 1 ? Members[0].PinColor : Color.white;
    }

    public static class PortalManager
    {
        // Ten colours visible on Valheim's dark map background.
        // Portals sharing the same tag (a connected pair) always hash to the same index.
        private static readonly Color[] Palette =
        {
            new Color(0.30f, 1.00f, 1.00f), // cyan
            new Color(1.00f, 0.90f, 0.20f), // yellow
            new Color(1.00f, 0.45f, 0.10f), // orange
            new Color(1.00f, 0.30f, 0.65f), // pink
            new Color(0.30f, 1.00f, 0.40f), // green
            new Color(1.00f, 0.30f, 0.30f), // red
            new Color(0.45f, 0.65f, 1.00f), // blue
            new Color(0.80f, 0.30f, 1.00f), // purple
            new Color(0.30f, 1.00f, 0.80f), // teal
            new Color(1.00f, 0.65f, 0.80f), // rose
        };

        private static readonly List<PortalInfo>    _portals  = new List<PortalInfo>();
        private static readonly List<PortalCluster> _clusters = new List<PortalCluster>();
        private static PortalSaveData _saveData = new PortalSaveData();
        private static Sprite? _portalSprite;

        public static IReadOnlyList<PortalInfo>    Portals  => _portals;
        public static IReadOnlyList<PortalCluster> Clusters => _clusters;

        public static Color ColorForName(string name)
        {
            if (name == "\"UNNAMED PORTAL\"") return Color.white;
            return Palette[Math.Abs(name.GetHashCode()) % Palette.Length];
        }

        // ── Portal discovery ─────────────────────────────────────────────

        public static void Refresh()
        {
            if (ZDOMan.instance == null) return;

            var zdos = ZDOMan.instance.GetPortals();

            var existing = new Dictionary<string, PortalInfo>();
            foreach (var p in _portals) existing[p.ZdoKey] = p;

            var newList = new List<PortalInfo>();
            foreach (var zdo in zdos)
            {
                var key  = zdo.m_uid.ToString();
                var name = zdo.GetString("tag");
                if (string.IsNullOrWhiteSpace(name)) name = "\"UNNAMED PORTAL\"";

                if (existing.TryGetValue(key, out var info))
                {
                    info.Name     = name;
                    info.PinColor = ColorForName(name);
                    info.Position = zdo.GetPosition();
                    newList.Add(info);
                    existing.Remove(key);
                }
                else
                {
                    newList.Add(new PortalInfo
                    {
                        ZdoKey    = key,
                        Name      = name,
                        Position  = zdo.GetPosition(),
                        ShowOnMap = _saveData.GetShow(key),
                        PinColor  = ColorForName(name)
                    });
                }
            }

            _portals.Clear();
            _portals.AddRange(newList);
            RebuildClusters();
        }

        // ── Visibility toggles ───────────────────────────────────────────

        public static void SetShowOnMap(PortalInfo portal, bool show)
        {
            portal.ShowOnMap = show;
            RebuildClusters();
            Save();
        }

        public static void ShowAll()
        {
            foreach (var p in _portals) p.ShowOnMap = true;
            RebuildClusters();
            Save();
        }

        public static void HideAll()
        {
            foreach (var p in _portals) p.ShowOnMap = false;
            RebuildClusters();
            Save();
        }

        // ── Clustering & pin sync ────────────────────────────────────────

        public static void RebuildClusters()
        {
            // Remove existing cluster pins from the minimap
            if (Minimap.instance != null)
                foreach (var c in _clusters)
                    if (c.Pin != null) Minimap.instance.RemovePin(c.Pin);

            foreach (var p in _portals) p.Pin = null;
            _clusters.Clear();

            if (Minimap.instance == null) return;

            var shown = _portals.Where(p => p.ShowOnMap).ToList();
            float threshold = Plugin.ClusterDistance.Value;

            // Greedy proximity grouping
            foreach (var portal in shown)
            {
                PortalCluster? match = null;
                foreach (var cluster in _clusters)
                {
                    foreach (var member in cluster.Members)
                    {
                        if (Vector3.Distance(portal.Position, member.Position) <= threshold)
                        {
                            match = cluster;
                            break;
                        }
                    }
                    if (match != null) break;
                }

                if (match != null)
                    match.Members.Add(portal);
                else
                {
                    var newCluster = new PortalCluster();
                    newCluster.Members.Add(portal);
                    _clusters.Add(newCluster);
                }
            }

            // Create one map pin per cluster
            var sprite = GetPortalSprite();
            foreach (var cluster in _clusters)
            {
                cluster.Pin = Minimap.instance.AddPin(
                    cluster.Centroid, Minimap.PinType.Icon0,
                    cluster.PinLabel, false, false);

                if (cluster.Pin != null)
                {
                    if (sprite != null) cluster.Pin.m_icon = sprite;
                    if (cluster.Pin.m_iconElement != null)
                    {
                        if (sprite != null) cluster.Pin.m_iconElement.sprite = sprite;
                        cluster.Pin.m_iconElement.color = cluster.PinColor;
                    }
                }

                // Link each member back to the cluster pin for reference
                foreach (var member in cluster.Members)
                    member.Pin = cluster.Pin;
            }
        }

        // ── Persistence ──────────────────────────────────────────────────

        public static void Load()
        {
            var path = GetSavePath();
            if (!File.Exists(path)) { _saveData = new PortalSaveData(); return; }
            try
            {
                _saveData = JsonConvert.DeserializeObject<PortalSaveData>(
                    File.ReadAllText(path)) ?? new PortalSaveData();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"PortalMap: failed to load: {e.Message}");
                _saveData = new PortalSaveData();
            }
        }

        public static void Save()
        {
            _saveData.Entries.Clear();
            foreach (var p in _portals)
                _saveData.Entries.Add(new PortalEntry { ZdoKey = p.ZdoKey, ShowOnMap = p.ShowOnMap });
            try { File.WriteAllText(GetSavePath(), JsonConvert.SerializeObject(_saveData, Formatting.Indented)); }
            catch (Exception e) { Plugin.Log.LogWarning($"PortalMap: failed to save: {e.Message}"); }
        }

        public static void Clear()
        {
            if (Minimap.instance != null)
                foreach (var c in _clusters)
                    if (c.Pin != null) Minimap.instance.RemovePin(c.Pin);
            _clusters.Clear();
            foreach (var p in _portals) p.Pin = null;
            _portals.Clear();
            _saveData  = new PortalSaveData();
            _portalSprite = null;
        }

        // ── Portal sprite ────────────────────────────────────────────────

        private static Sprite? GetPortalSprite()
        {
            if (_portalSprite != null) return _portalSprite;
            try
            {
                var prefab = ZNetScene.instance?.GetPrefab("portal_wood");
                _portalSprite = prefab?.GetComponent<Piece>()?.m_icon;
            }
            catch { }
            return _portalSprite;
        }

        // ── Grouped queries ──────────────────────────────────────────────

        public static List<PortalGroup> GetSortedGroups()
        {
            var dict = new Dictionary<string, PortalGroup>();
            foreach (var p in _portals)
            {
                if (!dict.TryGetValue(p.Name, out var group))
                    dict[p.Name] = group = new PortalGroup { Name = p.Name };
                group.Portals.Add(p);
            }
            return dict.Values.OrderBy(g => g.Name).ToList();
        }

        // ── Queries ──────────────────────────────────────────────────────

        // A portal is unconnected when its tag doesn't appear exactly twice
        // (Valheim only connects pairs; 1 = no partner, 3+ = too many).
        public static bool IsUnconnected(PortalInfo portal)
        {
            int count = 0;
            foreach (var p in _portals)
                if (p.Name == portal.Name) count++;
            return count != 2;
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static string GetSavePath()
        {
            string worldName = "default";
            try { worldName = ZNet.instance?.GetWorldName() ?? "default"; }
            catch { }
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "..", "LocalLow", "IronGate", "Valheim", "worlds_local");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{worldName}_portal_pins.json");
        }
    }
}
