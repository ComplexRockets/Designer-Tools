    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design.Tools;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.Tools;
    using Assets.Scripts.Ui.Designer;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi;
    using UnityEngine;

    namespace Assets.Scripts.DesignerTools {
        public class PartToolsManager {
            private Mod _Mod = Mod.Instance;
            private PartSelectorManager _SelectorManager;
            private PartSelection _partSelection;
            private DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;

            public void Initialize (PartSelectorManager selector) {
                _SelectorManager = selector;
            }

            public void OnAlignPart () {
                if (_SelectorManager.SelectedParts.Count > 0) {
                    _Designer.DesignerUi.ShowMessage ("Select part to align to or right click to cancel");
                    _SelectorManager.RequestPartSelectionEvent = true;

                } else {
                    _Designer.DesignerUi.ShowMessage ("No Part Selected (Select multiple parts by holding `TAB`");
                }
            }

            public void OnPartSelected (IPartScript part) {
                _SelectorManager.RequestPartSelectionEvent = false;
                _partSelection = PartSelection.CreatePartSelection(part, true);

                foreach (IPartScript selectedPart in _SelectorManager.SelectedParts) {
                    Vector3 slectedPartPos = selectedPart.Transform.position;
                    float DeltaPosition = part.Transform.position.x - slectedPartPos.x;
                    Vector3 newPosition = new Vector3 (slectedPartPos.x + DeltaPosition, slectedPartPos.y, slectedPartPos.z);
                    selectedPart.Transform.position = newPosition;
                }
            }

        }
    }