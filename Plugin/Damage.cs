using BepInEx;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        public class Damage
        {
            public string name { get; set; } = "Undefined";
            public string type { get; set; } = "Undefined";
            public string roll { get; set; } = "";
            public int total { get; set; } = 0;
            public string expansion { get; set; } = "";

            public Damage() {; }

            public Damage(string name, string type, string roll, string expansion, int total)
            {
                this.name = name;
                this.type = type;
                this.roll = roll;
                this.expansion = expansion;
                this.total = total;
            }
        }
    }
}
