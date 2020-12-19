    using UnityEngine;
    namespace Assets.Scripts.DesignerTools {
        public class DesignerToolsLoop : MonoBehaviour {
            private Mod _mod = Mod.Instance;

            protected virtual void Update () {
                if (Game.InDesignerScene && _mod.craftLoaded && _mod.designer.CraftScript != null) {
                    _mod.DesignerUpdate ();
                    if (ModSettings.Instance.DevMode) _mod.selectorManager.DesignerUpdate ();
                }
            }
        }
    }