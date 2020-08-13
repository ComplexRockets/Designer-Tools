    using UnityEngine;
    namespace Assets.Scripts.DesignerTools {
        public class DesignerToolsLoop : MonoBehaviour {
            private Mod _Mod = Mod.Instance;

            protected virtual void Update () {
                if (Game.InDesignerScene && _Mod.CraftLoaded) {
                    _Mod.DesignerUpdate ();
                    _Mod.SelectorManager.DesignerUpdate ();
                }
            }
        }
    }