using System;
using System.Collections.Generic;

namespace ValheimPortalMap.Data
{
    [Serializable]
    public class PortalSaveData
    {
        public List<PortalEntry> Entries { get; set; } = new List<PortalEntry>();

        public bool GetShow(string zdoKey)
        {
            foreach (var e in Entries)
                if (e.ZdoKey == zdoKey) return e.ShowOnMap;
            return false;
        }
    }

    [Serializable]
    public class PortalEntry
    {
        public string ZdoKey { get; set; } = "";
        public bool ShowOnMap { get; set; }
    }
}
