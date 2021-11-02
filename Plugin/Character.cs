using BepInEx;

using System.Collections.Generic;
using UnityEngine;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        public class Character
        {
            public bool NPC { get; set; } = false;
            public int reach { get; set; } = 5;
            public List<Roll> attacks { get; set; } = new List<Roll>();
            public List<Roll> saves { get; set; } = new List<Roll>();
            public List<Roll> skills { get; set; } = new List<Roll>();
            public List<string> resistance { get; set; } = new List<string>();
            public List<string> immunity { get; set; } = new List<string>();
            public bool _usingAttackBonus { get; set; } = false;
            public bool _usingDamageBonus { get; set; } = false;
            public bool _usingSkillBonus { get; set; } = false;
            public string _usingAttackBonusAmount { get; set; } = "";
            public string _usingDamageBonusAmount { get; set; } = "";
            public string _usingSkillBonusAmonunt { get; set; } = "";
        }

        public class Roll
        {
            public string name { get; set; } = "";
            public string type { get; set; } = "";
            public string roll { get; set; } = "";
            public string range { get; set; } = "5,5";
            public string info { get; set; } = "";
            public Roll link { get; set; } = null;

            public Roll()
            {

            }

            public Roll(Roll source)
            {
                Debug.Log("Copying Roll Stats To New Roll Object");
                this.name = source.name;
                this.type = source.type;
                this.roll = source.roll;
                this.range = source.range;
                this.info = source.info;
                if (source.link == null) { this.link = null; } else { this.link = new Roll(source.link); }
            }
        }
    }
}
