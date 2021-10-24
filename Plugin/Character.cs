using BepInEx;

using System.Collections.Generic;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        public class Character
        {
            public bool NPC { get; set; } = false;
            public List<Roll> attacks { get; set; } = new List<Roll>();
            public List<Roll> saves { get; set; } = new List<Roll>();
            public List<Roll> skills { get; set; } = new List<Roll>();
            public List<string> resistance { get; set; } = new List<string>();
            public List<string> immunity { get; set; } = new List<string>();
        }

        public class Roll
        {
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public string roll { get; set; } = "";
            public string info { get; set; } = "";
            public Roll link { get; set; } = null;
        }
    }
}
