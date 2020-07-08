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
    public class PartSelectorManager : MonoBehaviour {
        public List<IPartScript> SelectedParts => _SelectedParts;
        public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private IPartScript _SelectedPart => _Designer.SelectedPart;
        private List<IPartScript> _SelectedParts = new List<IPartScript> ();
        private bool _SelectedPartsAreSame = true;
        private bool TabPressed = false;
        private bool IgnoreNextPartSelectionEvent = false;
        protected virtual void Start () {
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

        }
        public void OnSceneLoaded (object sender, SceneEventArgs e) {
            if (e.Scene == "Design") {
                Debug.Log (e.Scene + " Loaded (PartSelectorManager.cs)");
                _Designer.SelectedPartChanged += OnSelectedPartChanged;
            }

        }

        protected virtual void Update () {
            if (!Game.InDesignerScene) return;

            try {
                if (Input.GetKeyDown (KeyCode.Tab)) TabPressed = true;
                else if (Input.GetKeyUp (KeyCode.Tab)) TabPressed = false;
            } catch (EntryPointNotFoundException e) { Debug.Log ("Error on Tab Check: " + e); }

            try { if (_SelectedParts.Count > 0) UpdatePartHighlight (); } catch (EntryPointNotFoundException e) { Debug.Log ("Error on UpdatePartHighlight: " + e); }
            try { if (Input.GetMouseButtonDown (0) && _Designer.PaintTool.Active) OnPartPainted (GetPart (Input.mousePosition)); } catch (EntryPointNotFoundException e) { Debug.Log ("Error on OnPartPainted: " + e); }
        }

        private void UpdatePartHighlight () {
            IPartHighlighter partHighlighter = _SelectedParts.First ().CraftScript.PartHighlighter;
            ThemeData _themeData = _SelectedParts.First ().Data.ThemeData;

            foreach (IPartScript part in _SelectedParts) {
                partHighlighter.HighlightColor = _themeData.Theme.PartStateColors.Selected;
                partHighlighter.AddPartHighlight (part);
            }
        }

        public void OnSelectedPartChanged (IPartScript OldPart, IPartScript NewPart) {
            Debug.Log("Selected Part Changed: " + NewPart?.Data.PartType.Id);
            if (!IgnoreNextPartSelectionEvent) {
                if (NewPart != null && _Designer.AllowPartSelection == true) {
                    if (OldPart != null && !_SelectedParts.Contains (OldPart)) _SelectedParts.Add (OldPart);

                    if (TabPressed) {
                        if (!_SelectedParts.Contains (NewPart)) {
                            if (_SelectedPartsAreSame && _SelectedParts.Count > 0 && NewPart.Data.PartType.Id != _SelectedParts.Last ().Data.PartType.Id) _SelectedPartsAreSame = false;
                            _SelectedParts.Add (NewPart);
                            Debug.Log ("Added Part to selection: " + NewPart);
                        } else {
                            _SelectedParts.Remove (NewPart);
                            Debug.Log ("Removed Part from selection: " + NewPart);
                            if (!_SelectedPartsAreSame) {
                                _SelectedPartsAreSame = true;
                                foreach (IPartScript part in _SelectedParts) {
                                    if (_SelectedParts.Count > 0 && part.Data.PartType.Id != _SelectedParts.Last ().Data.PartType.Id) { _SelectedPartsAreSame = false; break; }
                                }
                            }
                        }
                        if (!_SelectedPartsAreSame) _Designer.DeselectPart ();
                        else _Designer.SelectPart (_SelectedParts.Last (), null, false);
                        IgnoreNextPartSelectionEvent = true;

                    } else if (_SelectedParts.Count > 0) {
                        IPartHighlighter partHighlighter = _SelectedParts.First ().CraftScript.PartHighlighter;

                        foreach (IPartScript part in _SelectedParts) {
                            partHighlighter.RemovePartHighlight (part);
                        }

                        _SelectedParts = new List<IPartScript> ();
                        _SelectedPartsAreSame = true;
                        Debug.Log ("Deselected All Parts");
                    }
                }
            } else IgnoreNextPartSelectionEvent = false;
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

        private IPartScript GetPart (Vector2 screenPosition) {
            return _Designer.GetPartAtScreenPosition (screenPosition).PartScript;
        }
    }
}