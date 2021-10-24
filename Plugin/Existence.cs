using BepInEx;
using UnityEngine;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        public class Existence
        {
            public Vector3 position { get; set; } = Vector3.zero;
            public Vector3 rotation { get; set; } = Vector3.zero;

            public Existence()
            {

            }

            public Existence(Vector3 pos, Vector3 rot)
            {
                this.position = pos;
                this.rotation = rot;
            }

            public void Apply(Transform transform)
            {
                transform.position = this.position;
                transform.rotation = Quaternion.Euler(this.rotation);
            }
        }
    }
}
