using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ValheimPortalMap.UI
{
    public class PortalListUI : MonoBehaviour
    {
        // Valheim-inspired dark wooden colour palette
        private static readonly Color BgColor     = new Color(0.09f, 0.07f, 0.03f, 0.96f);
        private static readonly Color BorderCol   = new Color(0.55f, 0.38f, 0.12f, 1.00f);
        private static readonly Color TitleCol    = new Color(1.00f, 0.80f, 0.20f, 1.00f);
        private static readonly Color TextCol     = new Color(0.94f, 0.87f, 0.68f, 1.00f);
        private static readonly Color BtnNormal   = new Color(0.22f, 0.15f, 0.07f, 1.00f);
        private static readonly Color BtnHover    = new Color(0.33f, 0.23f, 0.11f, 1.00f);
        private static readonly Color BtnOnNormal = new Color(0.18f, 0.38f, 0.14f, 1.00f);
        private static readonly Color BtnOnHover  = new Color(0.26f, 0.50f, 0.20f, 1.00f);

        private GUIStyle? _windowStyle;
        private GUIStyle? _btnStyle;
        private GUIStyle? _btnOnStyle;
        private GUIStyle? _panelGroupStyle;
        private GUIStyle? _panelTitleStyle;
        private bool _stylesInit;

        private Vector2 _panelScroll;

        private readonly Dictionary<string, int> _focusIndex = new Dictionary<string, int>();

        private const float PanelW = 405f;

        public static bool IsMouseOverPanel { get; private set; }

        // ── Unity callbacks ───────────────────────────────────────────────

        private void OnGUI()
        {
            if (Minimap.instance != null && Minimap.instance.m_mode == Minimap.MapMode.Large)
                DrawMapOverlay();
            else
                IsMouseOverPanel = false;
        }

        // ── Map overlay ───────────────────────────────────────────────────

        private void DrawMapOverlay()
        {
            InitStyles();

            const float bw = 160f, bh = 30f;
            float x = 10f, y = 10f;

            bool anyShown = false;
            foreach (var p in PortalManager.Portals) if (p.ShowOnMap) { anyShown = true; break; }

            GUI.backgroundColor = anyShown ? BtnOnNormal : BtnNormal;
            if (GUI.Button(new Rect(x, y, bw, bh), anyShown ? "◼  Hide All Portals" : "▶  Show All Portals",
                anyShown ? _btnOnStyle! : _btnStyle!))
            {
                if (anyShown) PortalManager.HideAll();
                else          PortalManager.ShowAll();
            }
            GUI.backgroundColor = Color.white;

            DrawPortalListPanel(y + bh + 8f);
        }

        private void DrawPortalListPanel(float startY)
        {
            var groups = PortalManager.GetSortedGroups();
            if (groups.Count == 0) return;

            const float rowH     = 40f;
            const float groupGap = 4f;
            float innerW = PanelW - 20f;

            float contentH = 4f + groups.Count * (rowH + groupGap);
            float panelH   = Mathf.Min(contentH + 32f, Screen.height - startY - 10f);

            var panelRect = new Rect(10, startY, PanelW, panelH);
            IsMouseOverPanel = panelRect.Contains(Event.current.mousePosition);

            GUI.backgroundColor = new Color(0.09f, 0.07f, 0.03f, 0.92f);
            GUI.Box(new Rect(10, startY, PanelW, panelH), GUIContent.none, _windowStyle!);
            GUI.backgroundColor = Color.white;

            GUI.color = TitleCol;
            GUI.Label(new Rect(16, startY + 4, PanelW - 20, 22), "PORTALS", _panelTitleStyle!);
            GUI.color = new Color(BorderCol.r, BorderCol.g, BorderCol.b, 1f);
            GUI.DrawTexture(new Rect(12, startY + 25, PanelW - 8, 1), Texture2D.whiteTexture);
            GUI.color = Color.white;

            _panelScroll = GUI.BeginScrollView(
                new Rect(10, startY + 28, PanelW, panelH - 30f),
                _panelScroll,
                new Rect(0, 0, innerW, contentH),
                false, true);

            float y = 4f;
            foreach (var group in groups)
            {
                bool anyShown = false;
                foreach (var p in group.Portals) if (p.ShowOnMap) { anyShown = true; break; }

                bool unconnected = group.Portals.Count != 2;
                string nameLabel = unconnected ? group.Name + " *" : group.Name;

                GUI.backgroundColor = BtnNormal;
                if (GUI.Button(new Rect(4, y + 4, innerW - 118, rowH - 8), nameLabel, _panelGroupStyle!))
                    FocusNextInGroup(group.Name, group.Portals);
                GUI.backgroundColor = Color.white;

                GUI.backgroundColor = anyShown ? BtnOnNormal : BtnNormal;
                if (GUI.Button(new Rect(innerW - 112, y + 4, 110, rowH - 8),
                    anyShown ? "✓  On Map" : "+  Add to Map",
                    anyShown ? _btnOnStyle! : _btnStyle!))
                {
                    foreach (var p in group.Portals)
                        p.ShowOnMap = !anyShown;
                    PortalManager.RebuildClusters();
                    PortalManager.Save();
                }
                GUI.backgroundColor = Color.white;

                y += rowH + groupGap;
            }

            GUI.EndScrollView();
        }

        private void FocusNextInGroup(string name, List<PortalInfo> portals)
        {
            if (portals.Count == 0) return;
            int idx = _focusIndex.TryGetValue(name, out var i) ? i % portals.Count : 0;
            var pos = portals[idx].Position;
            CenterMap(pos);
            if (Chat.instance != null) Chat.instance.SendPing(pos);
            _focusIndex[name] = (idx + 1) % portals.Count;
        }

        private static void CenterMap(Vector3 worldPos)
        {
            if (Minimap.instance == null || Player.m_localPlayer == null) return;
            var offset = worldPos - Player.m_localPlayer.transform.position;
            offset.y = 0f;
            Traverse.Create(Minimap.instance).Field("m_mapOffset").SetValue(offset);
        }

        // ── Style init ────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            _windowStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(0, 0, 0, 0),
                border  = new RectOffset(2, 2, 2, 2),
            };
            _windowStyle.normal.background = MakeTex(BgColor);

            _btnStyle   = MakeBtn(BtnNormal,  BtnHover,   TextCol);
            _btnOnStyle = MakeBtn(BtnOnNormal, BtnOnHover, Color.white);

            _panelTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _panelTitleStyle.normal.textColor = TitleCol;

            _panelGroupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _panelGroupStyle.normal.textColor = TextCol;
        }

        private static GUIStyle MakeBtn(Color normal, Color hover, Color textColor)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding   = new RectOffset(6, 6, 3, 3),
            };
            s.normal.background = MakeTex(normal);
            s.hover.background  = MakeTex(hover);
            s.active.background = MakeTex(hover);
            s.normal.textColor  = textColor;
            s.hover.textColor   = Color.white;
            s.active.textColor  = Color.white;
            return s;
        }

        private static Texture2D MakeTex(Color col)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }
    }
}
