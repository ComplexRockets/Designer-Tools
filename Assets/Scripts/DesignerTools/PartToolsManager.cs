    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design.Tools;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.Tools;
    using Assets.Scripts.Ui.Designer;
    using ModApi.Common;
    using ModApi.Craft.Parts.Modifiers;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi;
    using UI.Xml;
    using UnityEngine;
    using WNP78;

    namespace Assets.Scripts.DesignerTools {
        public class PartToolsManager {
            private Mod _mod = Mod.Instance;
            private PartSelectorManager _selectorManager;
            private PartSelection _partSelection;
            private PartToolState _toolState;
            private DesignerScript _designer => (DesignerScript) Game.Instance.Designer;
            private ConstructorInfo _partDataConstructor;
            public Type CraftBuilder;
            public Type Symmmetry;

            public void Initialize (PartSelectorManager selector) {
                _selectorManager = selector;
                _partDataConstructor = typeof (PartData).GetConstructor (new Type[] { typeof (XElement), typeof (int), typeof (PartType) });
                CraftBuilder = ReflectionUtils.GetType ("Assets.Scripts.Craft.CraftBuilder");
                Symmmetry = ReflectionUtils.GetType ("Assets.Scripts.Design.Symmetry");
            }

            public void OnAlignPosition (char axis) {
                if (_toolState != null) return;

                if (_selectorManager.selectedParts.Count > 0) {
                    _designer.DesignerUi.ShowMessage ("Select part to align to, on " + axis + " axis, or right click to cancel", 30);
                    _selectorManager.requestPartSelectionEvent = true;
                    _toolState = new PartToolState (PartToolStateTypes.AlignPosition, axis);

                } else _designer.DesignerUi.ShowMessage ("No Part Selected (Select multiple parts by holding 'TAB')");
            }

            public void OnAlignRotation (char axis) {
                if (_toolState != null) return;

                if (_selectorManager.selectedParts.Count > 0) {
                    _designer.DesignerUi.ShowMessage ("Select part to align to, on " + axis + " axis, or right click to cancel", 30);
                    _selectorManager.requestPartSelectionEvent = true;
                    _toolState = new PartToolState (PartToolStateTypes.AlignRotation, axis);

                } else _designer.DesignerUi.ShowMessage ("No Part Selected (Select multiple parts by holding 'TAB')");
            }

            public void OnResizeParts (float value) {
                float factor = value / 100;
                Debug.Log ("Factor : " + factor);
                foreach (IPartScript part in _selectorManager.selectedParts) {
                    List<XElement> xmlData = GetModifiers (part.Data);

                    XElement Config = xmlData.Find (m => m.Name == "Config");
                    String scale = Config.Attributes ().ToList ().Find (a => a.Name == "partScale").Value;
                    List<String> newScale = scale.Split (',').ToList ();
                    Debug.Log ("Scale: " + scale + " => " + (float.Parse (newScale[0]) * factor) + "," + (float.Parse (newScale[1]) * factor) + "," + (float.Parse (newScale[2]) * factor));
                    Config.SetAttributeValue ("partScale", (float.Parse (newScale[0]) * factor) + "," + (float.Parse (newScale[1]) * factor) + "," + (float.Parse (newScale[2]) * factor));

                    XElement Part = xmlData.Find (m => m.Name == "Part");
                    String pos = Part.Attributes ().ToList ().Find (a => a.Name == "position").Value;
                    List<String> newPos = pos.Split (',').ToList ();
                    newPos.ForEach (val => val = (float.Parse (val) * factor).ToString ());
                    Debug.Log ("Position: " + pos + " => " + (float.Parse (newPos[0]) * factor) + "," + (float.Parse (newPos[1]) * factor) + "," + (float.Parse (newPos[2]) * factor));
                    Part.SetAttributeValue ("position", (float.Parse (newPos[0]) * factor) + "," + (float.Parse (newPos[1]) * factor) + "," + (float.Parse (newPos[2]) * factor));

                    ApplyXmlData (xmlData);
                    Debug.Log ("Applied XML");
                }
            }

            public void OnRightClic () {
                if (_selectorManager.requestPartSelectionEvent) {
                    _selectorManager.requestPartSelectionEvent = false;
                    _designer.DesignerUi.ShowMessage ("Canceled");
                    _toolState = null;
                }
            }

            public void OnPartSelected (IPartScript part) {
                _selectorManager.requestPartSelectionEvent = false;
                if (_toolState == null) return;
                if (_toolState.Type == PartToolStateTypes.AlignPosition) AlignPartsPositionTo (part);
                if (_toolState.Type == PartToolStateTypes.AlignRotation) AlignPartsRotationTo (part);
            }

            private void AlignPartsPositionTo (IPartScript part) {
                if (_selectorManager.selectedParts.Count > 0) {
                    if (_toolState != null) {
                        foreach (IPartScript selectedPart in _selectorManager.selectedParts) {
                            Vector3 selectedPartPos = selectedPart.Transform.position;
                            Vector3 newPosition;
                            float DeltaPosition;

                            if (_toolState.Axis == 'X') {
                                DeltaPosition = part.Transform.position.x - selectedPartPos.x;
                                newPosition = new Vector3 (selectedPartPos.x + DeltaPosition, selectedPartPos.y, selectedPartPos.z);
                            } else if (_toolState.Axis == 'Y') {
                                DeltaPosition = part.Transform.position.y - selectedPartPos.y;
                                newPosition = new Vector3 (selectedPartPos.x, selectedPartPos.y + DeltaPosition, selectedPartPos.z);
                            } else {
                                DeltaPosition = part.Transform.position.z - selectedPartPos.z;
                                newPosition = new Vector3 (selectedPartPos.x, selectedPartPos.y, selectedPartPos.z + DeltaPosition);
                            }
                            selectedPart.Transform.position = newPosition;
                        }
                        _designer.DesignerUi.ShowMessage ("Parts Position Aligned");
                    } else _designer.DesignerUi.ShowMessage (_mod.errorColor + "Part Alignment failed : ToolState Changed");
                } else _designer.DesignerUi.ShowMessage (_mod.errorColor = "Part Alignment failed : Part Selection Changed");
                _toolState = null;
            }

            private void AlignPartsRotationTo (IPartScript part) {
                
                if (_selectorManager.selectedParts.Count > 0) {
                    if (_toolState != null) {
                        foreach (IPartScript selectedPart in _selectorManager.selectedParts) {
                            Vector3 selectedPartRot = selectedPart.Transform.rotation.eulerAngles;
                            Vector3 newRotation;
                            float DeltaRotation;

                            if (_toolState.Axis == 'X') {
                                DeltaRotation = part.Transform.rotation.eulerAngles.x - selectedPartRot.x;
                                newRotation = new Vector3 (selectedPartRot.x + DeltaRotation, selectedPartRot.y, selectedPartRot.z);
                            } else if (_toolState.Axis == 'Y') {
                                DeltaRotation = part.Transform.rotation.eulerAngles.y - selectedPartRot.y;
                                newRotation = new Vector3 (selectedPartRot.x, selectedPartRot.y + DeltaRotation, selectedPartRot.z);
                            } else {
                                DeltaRotation = part.Transform.rotation.eulerAngles.z - selectedPartRot.z;
                                newRotation = new Vector3 (selectedPartRot.x, selectedPartRot.y, selectedPartRot.z + DeltaRotation);
                            }
                            selectedPart.Transform.rotation = Quaternion.Euler (newRotation);
                        }
                        _designer.DesignerUi.ShowMessage ("Parts Rotation Aligned");
                    } else _designer.DesignerUi.ShowMessage (_mod.errorColor + "Part Alignment failed : ToolState Changed");
                } else _designer.DesignerUi.ShowMessage (_mod.errorColor = "Part Alignment failed : Part Selection Changed");
                _toolState = null;
            }

            public void PaintPart (IPartScript part, bool flag, int MaterialLevel, int MaterialID, bool paintSymmetricParts) {
                if (MaterialLevel == -1) {
                    for (int i = 0; i < part.Data.MaterialIds.Count; i++) {
                        if (part.Data.MaterialIds[i] != MaterialID) { part.PartMaterialScript.SetMaterial (MaterialID, i); flag = true; }
                    }
                } else if (part.Data.MaterialIds[MaterialLevel] != MaterialID) { part.PartMaterialScript.SetMaterial (MaterialID, MaterialLevel); flag = true; }

                if (paintSymmetricParts) {
                    foreach (IPartScript item in Symmetry.EnumerateSymmetricPartScripts (part)) {
                        PaintPart (item, flag, MaterialLevel, MaterialID, false);
                    }
                }

                if (flag) {
                    foreach (PartModifierData modifier in part.Data.Modifiers) {
                        ((IDesignerPartModifierData) modifier).DesignerPartProperties.OnPartMaterialsChanged ();
                    }
                }
            }

            public List<XElement> GetModifiers (PartData part) {
                XElement data = part.GenerateXml (part.PartScript.CraftScript.Transform, false);
                data.SetAttributeValue ("activated", part.Activated);
                return data.DescendantsAndSelf ().ToList ();
            }

            public void ApplyXmlData (List<XElement> modifers) {
                XElement Part = modifers.Find (m => m.Name == "Part");
                PartData data = _designer.CraftScript.Data.Assembly.Parts.ToList ().Find (part => part.Id == int.Parse (Part.Attribute ("id").Value));
                List<PartConnection> oldConns = data.PartConnections;
                List<AttachPoint> oldAPs = data.AttachPoints;

                modifers.Remove (Part);
                modifers.ForEach (m => Part.Descendants ().Append (m));

                _partDataConstructor.Invoke (data, new object[] { Part, data.PartScript.CraftScript.Data.XmlVersion, data.PartType });

                data.SetP ("PartConnections", oldConns);
                data.SetP ("AttachPoints", oldAPs);

                ISymmetrySlice oldSlice = data.PartScript.SymmetrySlice;

                UnityEngine.Object.Destroy (data.PartScript.GameObject);
                CraftBuilder.CallS ("CreatePartGameObjects", new PartData[1] { data }, Game.Instance.Designer.CraftScript);

                data.PartScript.SymmetrySlice = oldSlice;
                Symmmetry.CallS ("SynchronizeParts", data.PartScript, true);

                _selectorManager.ignoreNextStructureChangedEvent = true;
                Game.Instance.Designer.CraftScript.RaiseDesignerCraftStructureChangedEvent ();
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
            public static String AlignPosition = "AlignPosition";
            public static String AlignRotation = "AlignRotation";
        }
    }