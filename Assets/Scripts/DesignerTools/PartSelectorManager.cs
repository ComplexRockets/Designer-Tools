namespace Assets.Scripts.DesignerTools {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Craft.Parts;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Design.Tools;
    using Assets.Scripts.Design;
    using Assets.Scripts.DevConsole;
    using Assets.Scripts.Input;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.Ui.Settings;
    using Assets.Scripts.Ui;
    using CodeStage.AdvancedFPSCounter;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Craft;
    using ModApi.Design;
    using ModApi.Input.Events;
    using ModApi.Input;
    using ModApi.Math;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi;
    using TMPro;
    using UI.Xml;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine;
    public class PartSelectorManager {
        public List<IPartScript> SelectedParts => _SelectedParts;
        public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private IPartScript _SelectedPart => _Designer.SelectedPart;
        private PartData _PrevPartState;
        private List<IPartScript> _SelectedParts = new List<IPartScript> ();
        private IPartHighlighter _PartHighlighter => _Designer.CraftScript.PartHighlighter;
        private DesignerTool _ActiveTool;
        private bool _SelectedPartsAreSame = true;
        private bool TabPressed = false;
        private bool IgnoreNextPartSelectionEvent = false;
        public bool RequestPartSelectionEvent = false;

        public void OnDesignerLoaded () {
            _Designer.SelectedPartChanged += OnSelectedPartChanged;
            _Designer.Click += OnClick;
            _Designer.CraftStructureChanged += OnCraftStructureChanged;
            _Designer.ActiveToolChanged += OnActiveToolChanged;
            _Designer.BeforeCraftUnloaded += OnCraftUnloading;
        }

        public void DesignerUpdate () {
            try {
                if (Input.GetKeyDown (KeyCode.Tab)) TabPressed = true;
                else if (Input.GetKeyUp (KeyCode.Tab)) TabPressed = false;
            } catch (EntryPointNotFoundException e) { Debug.Log ("Error on Tab Check: " + e); }

            try { if (_SelectedParts.Count > 0) UpdatePartHighlight (); } catch (EntryPointNotFoundException e) { Debug.Log ("Error on UpdatePartHighlight: " + e); }
        }

        public void OnCraftStructureChanged () {
            //Debug.Log ("Craft Structure Changed");
            //if (_ActiveTool != null) Debug.Log ("ActiveTool : " + _ActiveTool.Name);
            //else Debug.Log ("No Active Tool");
            //if (_Designer.DesignerUi.Flyouts.PartProperties.IsOpen) Debug.Log ("Part Property Open");
            //else Debug.Log ("Part Property Close");

            //Debug.Log ("Selected Part count: " + _SelectedParts.Count);
            if (_SelectedParts.Count > 0 && _SelectedPart != null) {
                PartStateChange Change;

                Change = GetTransformChange (_PrevPartState, _SelectedPart.Data);
                if (Change == null && _Designer.DesignerUi.Flyouts.PartProperties.IsOpen) Change = GetChangedProperty (_PrevPartState, _SelectedPart.Data);

                if (Change != null) {
                    ApplyChange (Change);
                    _PrevPartState = ClonePartData (_SelectedPart.Data);
                } //else Debug.Log ("Part State Not Changed");
                
                //_PrevPartState = new PartState (_SelectedPart.Data);
            }
        }

        public void OnSelectedPartChanged (IPartScript OldPart, IPartScript NewPart) {
            //Debug.Log ("Selected Part Changed: " + NewPart?.Data.PartType.Id);
            if (!IgnoreNextPartSelectionEvent) {
                if (!RequestPartSelectionEvent) {
                    if (_Designer.AllowPartSelection)
                        ManagePartSelectedEvent (OldPart, NewPart);
                } else
                    Mod.Instance.PartTools.OnPartSelected (NewPart);
            } else
                IgnoreNextPartSelectionEvent = false;
        }

        public void OnCraftUnloading () {
            //Debug.Log ("Craft Unloading");
            DeselectAllParts ();
        }

        public bool OnClick (ClickEventArgs e) {
            try { if (_Designer.PaintTool.Active) OnPartPainted (GetPart (e.Position)); } catch (Exception ex) { Debug.Log ("Error on OnPartPainted: " + ex); }
            return false;
        }

        public void OnActiveToolChanged (DesignerTool tool) {
            _ActiveTool = tool;
        }

        public void OnPartPainted (IPartScript PaintedPart) {
            if (_SelectedParts.Count > 0 && PaintedPart != null && _SelectedParts.Contains (PaintedPart)) {
                bool flag = false;
                int MaterialLevel = _Designer.PaintTool.MaterialLevel;
                int MaterialID = _Designer.PaintTool.MaterialId;

                foreach (IPartScript part in _SelectedParts) {
                    if (part.Data.Id != PaintedPart.Data.Id) PaintPart (part, flag, MaterialLevel, MaterialID, true);
                }
            }
        }

        private void PaintPart (IPartScript part, bool flag, int MaterialLevel, int MaterialID, bool paintSymmetricParts) {
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

        private void UpdatePartHighlight () {
            if (_SelectedParts.Count > 0) {
                _PartHighlighter.HighlightColor = new Color (0f, 1f, 0f, 1f);

                foreach (IPartScript part in _SelectedParts) {
                    _PartHighlighter.AddPartHighlight (part);
                    foreach (IPartScript SymmetricPart in Symmetry.EnumerateSymmetricPartScripts (part)) {
                        _PartHighlighter.AddPartHighlight (SymmetricPart);
                    }
                }
            } else _PartHighlighter.HighlightColor = _Designer.CraftScript.RootPart.Data.ThemeData.Theme.PartStateColors.Highlighted;
        }

        private void ManagePartSelectedEvent (IPartScript OldPart, IPartScript NewPart) {
            if (NewPart != null && TabPressed) {
                if (OldPart != null && !_SelectedParts.Contains (OldPart)) _SelectedParts.Add (OldPart);
                if (!_SelectedParts.Contains (NewPart)) {
                    if (_SelectedPartsAreSame && _SelectedParts.Count > 0 && NewPart.Data.PartType.Id != _SelectedParts.Last ().Data.PartType.Id) _SelectedPartsAreSame = false;
                    _SelectedParts.Add (NewPart);
                    Debug.Log ("Added Part to selection: " + NewPart);
                } else {
                    _SelectedParts.Remove (NewPart);
                    Debug.Log ("Removed Part from selection: " + NewPart);
                    if (!_SelectedPartsAreSame && _SelectedParts.Count > 0) {
                        _SelectedPartsAreSame = true;
                        foreach (IPartScript part in _SelectedParts) {
                            if (part.Data.PartType.Id != _SelectedParts.Last ().Data.PartType.Id) { _SelectedPartsAreSame = false; break; }
                        }
                    }
                }
                if (_SelectedPart != _SelectedParts.Last ()) {
                    _Designer.SelectPart (_SelectedParts.Last (), null, false);
                    IgnoreNextPartSelectionEvent = true;
                }
                //_PrevPartState = new PartState (_SelectedPart.Data);
                _PrevPartState = ClonePartData (_SelectedPart.Data);
            } else DeselectAllParts ();
        }

        private void DeselectAllParts () {
            if (_SelectedParts.Count > 0) {
                foreach (IPartScript part in _SelectedParts) {
                    _PartHighlighter.RemovePartHighlight (part);
                    foreach (IPartScript SymmetricPart in Symmetry.EnumerateSymmetricPartScripts (part)) {
                        _PartHighlighter.RemovePartHighlight (SymmetricPart);
                    }
                }

                _SelectedParts = new List<IPartScript> ();
                _SelectedPartsAreSame = true;
                Debug.Log ("Deselected All Parts ");
            }
        }

        private PartStateChange GetTransformChange (PartData oldPart, PartData newPart) {
            if (oldPart == null || newPart == null) { Debug.LogError ("Can not find transform change (Part State NULL)"); return null; }

            Debug.Log ("Position: " + newPart.Position + " Rotation: " + newPart.Rotation);
            Debug.Log ("Position: " + oldPart.Position + " Rotation: " + oldPart.Rotation);

            if (newPart.Position != oldPart.Position) return new PartStateChange (oldPart.Position, newPart.Position);
            if (newPart.Rotation != oldPart.Rotation) return new PartStateChange (oldPart.Rotation, newPart.Rotation);

            Debug.Log ("Part Transform Not Changed");
            return null;
        }

        private PartStateChange GetChangedProperty (PartData oldPart, PartData newPart) {
            if (oldPart == null || newPart == null) { Debug.LogError ("Can not find property change (Part State NULL)"); return null; }

            Debug.Log ("modifiers: " + oldPart.Modifiers.Count);
            foreach (PartModifierData oldmodifier in oldPart.Modifiers) {
                PartModifierData newmodifer = newPart.GetModifierById (oldmodifier.Id);
                if (newmodifer != null) {
                    if (newmodifer != oldmodifier) {
                        return new PartStateChange (newmodifer.Id, String.Empty, String.Empty);
                    }
                }
                Debug.Log ("Modifier Deleted");
            }
            Debug.Log ("No Change In Modifiers Found");
            return null;
        }

        private void ApplyChange (PartStateChange change) {
            Debug.Log ("Player Changed " + change.Type + " of " + _SelectedPart.Data.Name);
            if (change.Type == "Transform") {
                if (change.TransformType == "Position") MoveParts (SelectedParts, change.PositionDelta);
                else if (change.TransformType == "Rotation") RotateParts (SelectedParts, change.RotationAngle);
            } else if (change.Type == "Modifier") {
                Debug.Log ("Changed modifer: " + change.Modifier);
            }
        }
        private IPartScript GetPart (Vector2 screenPosition) {
            return _Designer.GetPartAtScreenPosition (screenPosition).PartScript;
        }
        private void MoveParts (List<IPartScript> parts, Vector3 deltaPosition) {
            Debug.Log ("Changed Position By " + deltaPosition);

            foreach (IPartScript part in parts) {
                if (part != _SelectedPart) {
                    PartSelection _partSelection = PartSelection.CreatePartSelection (part, true);
                    _partSelection.ContainerParent.Translate (deltaPosition);
                    _partSelection.Deselect();
                }
            }
        }

        private void RotateParts (List<IPartScript> parts, float angle) {
            Debug.Log ("Changed Rotation By " + angle + " on ");
            // Vector3 cor = _SelectedPart.Data.Position;

            // foreach (IPartScript part in parts) {
            //     PartSelection _partSelection = PartSelection.CreatePartSelection (part, true);
            //     _partSelection.ContainerParent.rota (cor, angle);
            // }
        }

        private PartData ClonePartData (PartData data) {
            PartData partData = new PartData (data.GenerateXml (_Designer.CraftScript.Transform, optimizeXml : false), _Designer.CraftScript.Data.XmlVersion, data.PartType);
            return partData;
        }
    }

    public class PartState {
        public Vector3 Position;
        public Vector3 Rotation;
        public List<PartModifierData> Modifiers;

        public PartState (PartData data) {
            Debug.Log ("creating PartState");
            Position = data.Position;
            Rotation = data.Rotation;

            Debug.Log ("Modifiers : " + data.Modifiers.Count);
            Modifiers = data.Modifiers;
            // foreach (PartModifierData modifier in data.Modifiers) {
            //     if (modifier != null) {
            //         Modifiers.Add (PartModifierData.CreateFromStateXml (modifier.GenerateStateXml (false), data, Mod.Instance.CraftXMLVersion));
            //         Debug.Log ("Added Modifier: " + modifier.Name);
            //     } else Debug.LogError ("Modifer Null error");
            // }
            Debug.Log ("created PartState");
        }
    }
}