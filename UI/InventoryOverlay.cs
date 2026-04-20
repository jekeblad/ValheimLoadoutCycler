using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLoadoutCycler.UI
{
    public class InventoryOverlay : MonoBehaviour
    {
        private readonly Color[] LoadoutColors = new Color[]
        {
            new Color(1f, 1f, 1f, 0.85f),    // white
            new Color(0.2f, 0.85f, 0.2f, 0.85f),  // green
            new Color(1f, 0.7f, 0.1f, 0.85f),     // orange
            new Color(0.9f, 0.2f, 0.9f, 0.85f),   // purple
        };

        private GUIStyle? _badgeStyle;
        private GUIStyle? _panelStyle;
        private GUIStyle? _buttonStyle;
        private GUIStyle? _activeButtonStyle;

        private void OnGUI()
        {
            if (!InventoryGui.IsVisible()) return;
            var inventoryGui = InventoryGui.instance;
            if (inventoryGui == null) return;

            EnsureStyles();
            DrawBadges(inventoryGui);

            if (ConfigMode.IsActive)
                DrawConfigPanel(inventoryGui);
        }

        private void DrawBadges(InventoryGui inventoryGui)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;

            var itemByPos = new Dictionary<(int, int), ItemDrop.ItemData>();
            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (item.m_gridPos.x >= 0 && item.m_gridPos.y >= 0)
                    itemByPos[(item.m_gridPos.x, item.m_gridPos.y)] = item;
            }

            var elements = Traverse.Create(inventoryGui.m_playerGrid)
                .Field("m_elements").GetValue() as System.Collections.IList;
            if (elements == null) return;

            foreach (var element in elements)
            {
                var t = Traverse.Create(element);
                if (!t.Field("m_used").GetValue<bool>()) continue;

                var pos = t.Field("m_pos").GetValue<Vector2i>();
                if (!itemByPos.TryGetValue((pos.x, pos.y), out var item)) continue;

                string? prefabName = item.m_dropPrefab?.name;
                if (prefabName == null) continue;

                var go = t.Field("m_go").GetValue<GameObject>();
                if (go == null) continue;

                var rt = go.GetComponent<RectTransform>();
                if (rt == null) continue;

                var slotRect = GetScreenRect(rt);
                var indices = LoadoutManager.GetItemLoadoutIndices(prefabName, item.m_quality);

                if (ConfigMode.IsActive && indices.Contains(ConfigMode.EditingLoadoutIndex))
                    DrawOutline(slotRect, LoadoutColors[ConfigMode.EditingLoadoutIndex % LoadoutColors.Length], Plugin.OutlineThickness.Value);

                for (int b = 0; b < indices.Count; b++)
                {
                    int li = indices[b];
                    var badgeRect = new Rect(slotRect.x + 2 + b * 18, slotRect.y + 2, 16, 14);
                    var prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = LoadoutColors[li % LoadoutColors.Length];
                    GUI.Box(badgeRect, GUIContent.none);
                    GUI.backgroundColor = prevColor;
                    GUI.Label(badgeRect, $"{li + 1}", _badgeStyle!);
                }
            }
        }

        private static void DrawOutline(Rect rect, Color color, float thickness = 2f)
        {
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private void DrawConfigPanel(InventoryGui gui)
        {
            var gridRt = gui.m_playerGrid.GetComponent<RectTransform>();
            if (gridRt == null) return;

            var inventoryRect = GetScreenRect(gridRt);
            float panelWidth = 320f;
            float panelHeight = 52f;
            var panelRect = new Rect(inventoryRect.x, inventoryRect.y - panelHeight - 4, panelWidth, panelHeight);

            GUI.Box(panelRect, GUIContent.none, _panelStyle!);

            float btnWidth = 44f;
            float y = panelRect.y + 8;
            float x = panelRect.x + 8;

            GUI.Label(new Rect(x, y + 8, 70, 20), "Loadout:", _badgeStyle!);
            x += 74;

            for (int i = 0; i < 4; i++)
            {
                var style = ConfigMode.EditingLoadoutIndex == i ? _activeButtonStyle! : _buttonStyle!;
                if (GUI.Button(new Rect(x + i * (btnWidth + 4), y, btnWidth, 36), $"#{i + 1}", style))
                    ConfigMode.EditingLoadoutIndex = i;
            }

            float doneX = x + 4 * (btnWidth + 4) + 8;
            if (GUI.Button(new Rect(doneX, y, 48, 36), "Done", _buttonStyle!))
                ConfigMode.Exit();
        }

        private static Rect GetScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float x = corners[0].x;
            float y = Screen.height - corners[2].y;
            float w = corners[2].x - corners[0].x;
            float h = corners[2].y - corners[0].y;
            return new Rect(x, y, w, h);
        }

        private void EnsureStyles()
        {
            if (_badgeStyle != null) return;

            _badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _badgeStyle.normal.textColor = Color.white;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(4, 4, 4, 4),
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
            };

            _activeButtonStyle = new GUIStyle(_buttonStyle);
            _activeButtonStyle.normal.textColor = Color.yellow;
        }
    }
}
