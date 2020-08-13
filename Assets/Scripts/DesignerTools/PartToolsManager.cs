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
            private PartToolState _ToolState;
            private DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;

            public void Initialize (PartSelectorManager selector) {
                _SelectorManager = selector;
            }

            public void OnAlignPart (char axis) {
                if (_ToolState != null) return;

                if (_SelectorManager.SelectedParts.Count > 0) {
                    _Designer.DesignerUi.ShowMessage ("Select part to align to on " + axis + " axis, or right click to cancel", 30);
                    _SelectorManager.RequestPartSelectionEvent = true;
                    _ToolState = new PartToolState (PartToolStateTypes.Align, axis);

                } else _Designer.DesignerUi.ShowMessage ("No Part Selected (Select multiple parts by holding `TAB`)");
            }

            public void OnRightClic () {
                if (_SelectorManager.RequestPartSelectionEvent) {
                    _SelectorManager.RequestPartSelectionEvent = false;
                    _Designer.DesignerUi.ShowMessage ("Canceled");
                    _ToolState = null;
                }
            }

            public void OnPartSelected (IPartScript part) {
                _SelectorManager.RequestPartSelectionEvent = false;
                if (_ToolState == null) return;
                if (_ToolState.Type == PartToolStateTypes.Align) AlignPartsTo (part);
            }

            private void AlignPartsTo (IPartScript part) {
                if (_SelectorManager.SelectedParts.Count > 0) {
                    if (_ToolState != null) {
                        foreach (IPartScript selectedPart in _SelectorManager.SelectedParts) {
                            Vector3 selectedPartPos = selectedPart.Transform.position;
                            Vector3 newPosition;
                            float DeltaPosition;

                            if (_ToolState.Axis == 'X') {
                                DeltaPosition = part.Transform.position.x - selectedPartPos.x;
                                newPosition = new Vector3 (selectedPartPos.x + DeltaPosition, selectedPartPos.y, selectedPartPos.z);
                            } else if (_ToolState.Axis == 'Y') {
                                DeltaPosition = part.Transform.position.y - selectedPartPos.y;
                                newPosition = new Vector3 (selectedPartPos.x, selectedPartPos.y + DeltaPosition, selectedPartPos.z);
                            } else {
                                DeltaPosition = part.Transform.position.z - selectedPartPos.z;
                                newPosition = new Vector3 (selectedPartPos.x, selectedPartPos.y, selectedPartPos.z + DeltaPosition);
                            }
                            selectedPart.Transform.position = newPosition;
                        }
                        _Designer.DesignerUi.ShowMessage ("Part Aligned");
                    } else _Designer.DesignerUi.ShowMessage ("<color=#b33e46> Part Alignment failed : ToolState Changed");
                } else _Designer.DesignerUi.ShowMessage ("<color=#b33e46> Part Alignment failed : Part Selection Changed");
                _ToolState = null;
            }

        }

        public class PartToolState {
            public String Type;
            public char Axis; // X ; Y ; Z

            public PartToolState (String type, char axis) {
                Type = type;
                Axis = axis;
            }
        }

        public static class PartToolStateTypes {
            public static String Align = "Align";
        }
    }