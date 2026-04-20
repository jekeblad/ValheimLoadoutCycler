using System.Collections.Generic;
using Newtonsoft.Json;

namespace ValheimLoadoutCycler.Data
{
    public class LoadoutSlot
    {
        public string PrefabName { get; set; } = "";
        public int Quality { get; set; } = 1;
    }

    public class Loadout
    {
        public int Index { get; set; }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<LoadoutSlot> Items { get; set; } = new List<LoadoutSlot>();
    }

    public class LoadoutSaveData
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<Loadout> Loadouts { get; set; } = new List<Loadout>
        {
            new Loadout { Index = 0 },
            new Loadout { Index = 1 },
            new Loadout { Index = 2 },
            new Loadout { Index = 3 },
        };
        public int ActiveIndex { get; set; } = 0;
    }
}
