    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.Tools;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi;
    using UnityEngine;
    namespace Assets.Scripts.DesignerTools {
        public class DesignerToolsLoop : MonoBehaviour {

            private Mod _Mod = Mod.Instance;
            public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;

            protected virtual void Update () {
                if (Game.InDesignerScene) {
                    _Mod.DesignerUpdate ();
                }
            }
        }
    }