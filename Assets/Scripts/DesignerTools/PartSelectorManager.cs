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
        public List<IPartScript> selectedParts => _selectedParts;
        public DesignerScript designer => (DesignerScript) Game.Instance.Designer;
        private PartToolsManager _partTools => _mod.partTools;
        private Mod _mod => Mod.Instance;
        private IPartScript _selectedPart => designer.SelectedPart;
        private XElement _prevPartState;
        private List<IPartScript> _selectedParts = new List<IPartScript> ();
        public List<String> excludedModifers = new List<string> () { "AttachPoints" };
        public List<String> excludedAtributes = new List<string> () { "id", "symmetryId" };
        private IPartHighlighter _partHighlighter => designer.CraftScript.PartHighlighter;
        private DesignerTool _activeTool;
        //private bool _SelectedPartsAreSame = true;
        public bool ignoreNextPartSelectionEvent = false;
        public bool ignoreNextStructureChangedEvent = false;
        public bool requestPartSelectionEvent = false;

        public void OnDesignerLoaded () {
            designer.SelectedPartChanged += OnSelectedPartChanged;

            if (!ModSettings.Instance.DevMode) return;
            designer.Click += OnClick;
            designer.CraftStructureChanged += OnCraftStructureChanged;
            designer.ActiveToolChanged += OnActiveToolChanged;
            designer.BeforeCraftUnloaded += OnCraftUnloading;
        }

        public void OnSettingChanged () {
            if (!ModSettings.Instance.DevMode) {
                Debug.LogWarning ("DesignerTools: DevMode Off");
                try { designer.Click -= OnClick; } catch { }
                try { designer.CraftStructureChanged -= OnCraftStructureChanged; } catch { }
                try { designer.ActiveToolChanged -= OnActiveToolChanged; } catch { }
                try { designer.BeforeCraftUnloaded -= OnCraftUnloading; } catch { }
            } else {
                Debug.LogWarning ("DesignerTools: DevMode On");
                try { designer.Click += OnClick; } catch { }
                try { designer.CraftStructureChanged += OnCraftStructureChanged; } catch { }
                try { designer.ActiveToolChanged += OnActiveToolChanged; } catch { }
                try { designer.BeforeCraftUnloaded += OnCraftUnloading; } catch { }
            }
        }

        public void DesignerUpdate () {
            // try {
            //     if (Input.GetKeyDown (KeyCode.Tab)) TabPressed = true;
            //     else if (Input.GetKeyUp (KeyCode.Tab)) TabPressed = false;
            // } catch (EntryPointNotFoundException e) { Debug.Log ("Error on Tab Check: " + e); }

            try { if (_selectedParts.Count > 0) UpdatePartHighlight (); } catch (EntryPointNotFoundException e) { Debug.Log ("Error on UpdatePartHighlight: " + e); }
        }

        public void OnCraftStructureChanged () {
            Debug.Log ("Craft Structure Changed");
            if (ignoreNextStructureChangedEvent) {
                ignoreNextStructureChangedEvent = false;
                return;
            }
            //if (_ActiveTool != null) Debug.Log ("ActiveTool : " + _ActiveTool.Name);
            //else Debug.Log ("No Active Tool");
            //if (_Designer.DesignerUi.Flyouts.PartProperties.IsOpen) Debug.Log ("Part Property Open");
            //else Debug.Log ("Part Property Close");

            //Debug.Log ("Selected Part count: " + _SelectedParts.Count);
            if (_selectedParts.Count > 1 && _selectedPart != null) {
                List<PartStateChange> Change = GetPartStateChange (_prevPartState, _selectedPart.Data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false));

                if (Change != null) {
                    ApplyChange (Change, _selectedParts.Select (part => part.Data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false)).ToList ());
                    _prevPartState = new XElement (_selectedPart.Data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false));
                } else Debug.Log ("Part State Not Changed");
            } else SelectLastPart ();
        }

        public void OnSelectedPartChanged (IPartScript OldPart, IPartScript NewPart) {
            //Debug.Log ("Selected Part Changed: " + NewPart?.Data.PartType.Id);
            if (!ignoreNextPartSelectionEvent) {
                if (NewPart != null) {
                    if (!requestPartSelectionEvent) {
                        if (designer.AllowPartSelection && ModSettings.Instance.DevMode) ManagePartSelectedEvent (OldPart, NewPart);
                    } else {
                        _mod.partTools.OnPartSelected (NewPart);
                        SelectLastPart ();
                    }
                }
            } else ignoreNextPartSelectionEvent = false;

            if (NewPart != null) {
                if (!_selectedParts.Contains (NewPart)) _selectedParts.Add (NewPart);

                if (!ModSettings.Instance.DevMode) return;

                _prevPartState = _selectedPart.Data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false);
                Debug.Log (NewPart.Data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false));
            } else DeselectAllParts ();
        }

        public void OnCraftUnloading () {
            DeselectAllParts ();
        }

        public bool OnClick (ClickEventArgs e) {
            try { if (designer.PaintTool.Active) OnPartPainted (GetPart (e.Position)); } catch (Exception ex) { Debug.Log ("Error on OnPartPainted: " + ex); }
            return false;
        }

        public void OnActiveToolChanged (DesignerTool tool) {
            _activeTool = tool;
        }

        public void OnPartPainted (IPartScript PaintedPart) {
            if (_selectedParts.Count > 0 && PaintedPart != null && _selectedParts.Contains (PaintedPart)) {
                bool flag = false;
                int MaterialLevel = designer.PaintTool.MaterialLevel;
                int MaterialID = designer.PaintTool.MaterialId;

                foreach (IPartScript part in _selectedParts) {
                    if (part.Data.Id != PaintedPart.Data.Id) _partTools.PaintPart (part, flag, MaterialLevel, MaterialID, true);
                }
            }
        }

        public void SelectAllParts () {
            DeselectAllParts ();
            foreach (PartData part in designer.CraftScript.Data.Assembly.Parts) {
                _selectedParts.Add (part.PartScript);
            }
            SelectLastPart ();
        }

        private void UpdatePartHighlight () {
            if (_partHighlighter == null) return;
            if (_selectedParts.Count > 1) {
                _partHighlighter.HighlightColor = new Color (0f, 1f, 0f, 1f);

                foreach (IPartScript part in _selectedParts) {
                    _partHighlighter.AddPartHighlight (part);
                    foreach (IPartScript SymmetricPart in Symmetry.EnumerateSymmetricPartScripts (part)) {
                        _partHighlighter.AddPartHighlight (SymmetricPart);
                    }
                }
            } else _partHighlighter.HighlightColor = designer.CraftScript.RootPart.Data.ThemeData.Theme.PartStateColors.Highlighted;
        }

        private void ManagePartSelectedEvent (IPartScript OldPart, IPartScript NewPart) {
            if (Input.GetKey (KeyCode.Tab)) {
                if (OldPart != null && !_selectedParts.Contains (OldPart)) _selectedParts.Add (OldPart);
                if (!_selectedParts.Contains (NewPart)) {
                    _selectedParts.Add (NewPart);
                    Debug.Log ("Added Part to selection: " + NewPart);
                } else {
                    _partHighlighter.RemovePartHighlight (NewPart);
                    _selectedParts.Remove (NewPart);
                    Debug.Log ("Removed Part from selection: " + NewPart);
                }
                SelectLastPart ();
            } else DeselectAllParts ();
        }

        private void DeselectAllParts () {
            if (_selectedParts.Count > 0) {
                foreach (IPartScript part in _selectedParts) {
                    _partHighlighter.RemovePartHighlight (part);
                    foreach (IPartScript SymmetricPart in Symmetry.EnumerateSymmetricPartScripts (part)) {
                        _partHighlighter.RemovePartHighlight (SymmetricPart);
                    }
                }

                _selectedParts = new List<IPartScript> ();
                Debug.Log ("Deselected All Parts ");
            }
        }

        private void SelectLastPart () {
            if (_selectedParts.Count > 1 && _selectedPart != _selectedParts.Last ()) {
                ignoreNextPartSelectionEvent = true;
                designer.SelectPart (_selectedParts.Last (), null, false);
            }

        }

        private List<PartStateChange> GetPartStateChange (XElement oldState, XElement newState) {
            List<XElement> Old = oldState.DescendantsAndSelf ().ToList ();
            List<XElement> New = newState.DescendantsAndSelf ().ToList ();
            List<PartStateChange> changes = new List<PartStateChange> ();

            foreach (XElement oldModifer in New) {
                if (!excludedModifers.Contains (oldModifer.Name.LocalName)) {
                    XElement newModifer = New.Find (m => m.Name == oldModifer.Name);
                    if (newModifer != null) {
                        if (oldModifer.ToString () != newModifer.ToString ()) {
                            Debug.Log ("Modifers " + oldModifer.Name + " Was Changed");
                            foreach (XAttribute oldAtribute in oldModifer.Attributes ()) {
                                if (!excludedAtributes.Contains (oldAtribute.Name.LocalName)) {
                                    XAttribute newAtribute = newModifer.Attributes ().ToList ().Find (a => a.Name == oldAtribute.Name);
                                    if (newAtribute != null) {
                                        if (newAtribute.Value != oldAtribute.Value) changes.Add (new PartStateChange (oldAtribute, newAtribute, false));
                                    } else Debug.Log ("Atribute " + oldAtribute.Name + " Deleted");
                                }
                            }
                        } else Debug.Log ("Modifers " + oldModifer.Name + " Was not Changed");
                    } else Debug.Log ("Modifer " + oldModifer.Name + " Was Deleted");
                }
            }
            return changes;
        }

        private void ApplyChange (List<PartStateChange> changes, List<XElement> parts) {
            List<List<XElement>> xmls = parts.Select (p => p.DescendantsAndSelf ().ToList ()).ToList ();
            foreach (PartStateChange change in changes) {
                foreach (List<XElement> xml in xmls) {
                    xml.Find (m => m.Name == change.modifer)?.SetAttributeValue (change.atribute, change.newAttribute);
                }
            }
            foreach (List<XElement> xml in xmls) {
                _partTools.ApplyXmlData (xml);
            }
        }
        private IPartScript GetPart (Vector2 screenPosition) {
            return designer.GetPartAtScreenPosition (screenPosition).PartScript;
        }
        private void MoveParts (List<IPartScript> parts, Vector3 deltaPosition) {
            Debug.Log ("Changed Position By " + deltaPosition);

            foreach (IPartScript part in parts) {
                if (part != _selectedPart) {
                    PartSelection _partSelection = PartSelection.CreatePartSelection (part, true);
                    _partSelection.ContainerParent.Translate (deltaPosition);
                    _partSelection.Deselect ();
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
            return new PartData (data.GenerateXml (designer.CraftScript.Transform, optimizeXml : false), _mod.craftXMLVersion, data.PartType);
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
            //         Modifiers.Add (PartModifierData.CreateFromStateXml (modifier.GenerateStateXml (false), data, _Mod.CraftXMLVersion));
            //         Debug.Log ("Added Modifier: " + modifier.Name);
            //     } else Debug.LogError ("Modifer Null error");
            // }
            Debug.Log ("created PartState");
        }
    }
}