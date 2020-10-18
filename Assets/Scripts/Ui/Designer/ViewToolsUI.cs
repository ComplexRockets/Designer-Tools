using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Assets.Scripts;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Design;
using Assets.Scripts.Design.Tools;
using Assets.Scripts.Flight.GameView.UI;
using Assets.Scripts.Flight.GameView.UI.Inspector;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.Input;
using Assets.Scripts.Ui;
using Assets.Scripts.Ui.Inspector;
using ModApi;
using ModApi.Common;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using ModApi.Design;
using ModApi.Design.PartProperties;
using ModApi.Flight.UI;
using ModApi.Input;
using ModApi.Math;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.DesignerTools {
    public class ViewToolsUI : MonoBehaviour {
        private IXmlLayoutController _controller;
        private bool _orthoViewActive;
        private bool _selectingImage = false;
        private bool _mouseDown = false;
        public bool viewToolPanelPinned = false;
        private Camera _designerCamera = Game.Instance.Designer.DesignerCamera.Camera;
        private float _orthoSize;
        private MouseDrag _mouseDrag;
        private String _path = Mod.Instance.refImagePath;
        private List<String> _images => (Directory.EnumerateFiles (_path, "*.png").Union (Directory.EnumerateFiles (_path, "*.jpg")).Union (Directory.EnumerateFiles (_path, "*.jpeg"))).ToList ();
        private List<ReferenceImage> _referenceImages = new List<ReferenceImage> ();
        public List<ReferenceImage> referenceImages => _referenceImages;
        public DesignerScript _designer => (DesignerScript) Game.Instance.Designer;
        private Mod _mod => Mod.Instance;
        private DesignerPartList _designerParts;
        private DesignerPart _fuselage = new DesignerPart ();
        private PartScript _referencePart;
        private XmlLayout _xmlLayout;
        private XmlElement _zoomPanel;
        private XmlElement _imageSelector;
        private XmlElement _viewToolsPin;
        private XmlElement _imageConfirmButton;
        private Texture2D selectedImage;
        private string selectedView;

        public void OnLayoutRebuilt (IXmlLayoutController xmlLayoutController) {
            _orthoSize = _designerCamera.orthographicSize;
            //_MouseDrag = new MouseDrag (_Designer.GizmoCamera);
            _mod.viewToolsUI = this;

            _controller = xmlLayoutController;
            _xmlLayout = (XmlLayout) _controller.XmlLayout;
            _zoomPanel = _xmlLayout.GetElementById ("ZoomPanel");
            _imageSelector = _xmlLayout.GetElementById ("ImageSelector");
            _viewToolsPin = _xmlLayout.GetElementById ("ViewToolsPin");
            _imageConfirmButton = _xmlLayout.GetElementById ("ImageConfirmButton");
            _xmlLayout.GetElementById ("FolderPathText").SetAndApplyAttribute ("text", "Folder location : " + _path);

            if (_designerCamera.orthographic == true) {
                XmlElement orthoToggle = _xmlLayout.GetElementById ("OrthoToggle");
                //orthoToggle.SetAndApplyAttribute ("isOn", "true");
                _zoomPanel.SetActive (true);
                _orthoViewActive = true;
            }

            UpdateReferenceImages (_mod._referenceImages);

            _designerParts = Game.Instance.CachedDesignerParts;
            foreach (DesignerPart part in _designerParts.Parts) {
                if (part.PartTypes.First ().Id == "Fuselage1") {
                    _fuselage = part;
                }
            }

            foreach (PartData part in _designer.CraftScript.Data.Assembly.Parts) {
                PartScript _part = (PartScript) part.PartScript;
                if (_part.name == "DesignerToolsRefPart") {
                    _designer.SelectPart (_part, null, false);
                    _designer.DeleteSelectedParts ();
                }
            }
        }

        public void UpdateReferenceImages (List<ReferenceImage> Images) {
            foreach (ReferenceImage image in _referenceImages) {
                image.Destroy ();
            }

            _referenceImages = Images;

            foreach (ReferenceImage image in _referenceImages) {
                if (image.viewToolsUI == null) image.viewToolsUI = this;
                image.ApplyChanges ();

                if (!image.active) {
                    XmlElement ToggleButton = _xmlLayout.GetElementById ("Toggle" + image.view);
                    ToggleButton.SetAndApplyAttribute ("color", "Button");
                }

                _xmlLayout.GetElementById (image.view + "Settings").SetActive (true);
                _xmlLayout.GetElementById (image.view + "NoImageSelected").SetActive (false);
                _xmlLayout.GetElementById ("Select" + image.view).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Edit Image");
            }
        }

        public void Close () {
            foreach (ReferenceImage image in _referenceImages) {
                image.EditMode (false);
            }
            _mod.OnViewPanelClosed (_referenceImages);
            _xmlLayout.Hide (() => Destroy (this.gameObject), true);
        }

        public void OnUIValueChanged (ReferenceImage image) {
            String prefix = image.view + "-";
            _xmlLayout.GetElementById<InputField> (prefix + "OffsetX")?.SetTextWithoutNotify ((image.offsetX).ToString ());
            _xmlLayout.GetElementById<InputField> (prefix + "OffsetY")?.SetTextWithoutNotify ((image.offsetY).ToString ());
            _xmlLayout.GetElementById<InputField> (prefix + "Rotation")?.SetTextWithoutNotify (image.rotation.ToString ());
            _xmlLayout.GetElementById<InputField> (prefix + "Scale")?.SetTextWithoutNotify (image.scale.ToString ());
            _xmlLayout.GetElementById<InputField> (prefix + "Opacity")?.SetTextWithoutNotify (image.opacity.ToString ());
        }

        protected virtual void Update () {
            if (UnityEngine.Input.GetMouseButtonDown (0)) {
                _mouseDown = true;
                foreach (ReferenceImage image in _referenceImages) {
                    if (image.editModeOn) image.OnMouseStart ();
                }
            } else if (UnityEngine.Input.GetMouseButtonUp (0)) {
                _mouseDown = false;
                foreach (ReferenceImage image in _referenceImages) {
                    if (image.editModeOn) image.OnMouseEnd ();
                }
            }
            if (_mouseDown) {
                foreach (ReferenceImage image in _referenceImages) {
                    if (image.editModeOn) image.OnMouseDown ();
                }
            }
        }

        public void SetReferencePart (bool on) {
            bool editmode = false;
            foreach (ReferenceImage image in _referenceImages) {
                if (image.editModeOn) { editmode = true; break; }
            }

            if (on && _referencePart == null) {
                _designer.AddPart (_fuselage, new Vector2 ());
                _referencePart = (PartScript) _designer.CraftScript.Data.Assembly.Parts.Last ().PartScript;
                _referencePart.name = "DesignerToolsRefPart";
                _designer.SelectPart (_referencePart, null, false);
            } else if (!on && _referencePart != null) {
                if (!editmode) {
                    _designer.SelectPart (_referencePart, null, false);
                    _designer.DeleteSelectedParts ();
                    _referencePart = null;
                }
            }
            _designer.AllowPartSelection = !editmode;
        }

        private void OnViewButtonClicked (String view) {
            _mod.SetCameraTo (view);
        }

        private void OnSelectImageButtonClicked (XmlElement button) {
            if (_selectingImage == false) {
                string view = button.id.Remove (0, 6);

                ReferenceImage refimage = GetReferenceImage (view);
                if (refimage?.view != null) {
                    if (refimage.active) refimage.EditMode (true);
                    if (refimage.editModeOn) {
                        _xmlLayout.GetElementById ("Select" + view).SetActive (false);
                        _xmlLayout.GetElementById ("EditModeSettings" + view).SetActive (true);
                    } else {
                        _xmlLayout.GetElementById ("Select" + view).SetActive (true);
                        _xmlLayout.GetElementById ("EditModeSettings" + view).SetActive (false);
                    }
                    return;
                }

                _selectingImage = true;
                _imageSelector.SetActive (true);
                _imageConfirmButton.SetActive (false);
                UpdateList ("Image", _images);
                selectedView = view;
            }
        }

        private void OnImageSelected (XmlElement image) {
            if (_path == null) _path = (Application.persistentDataPath + "/mods/DesignerTools/ReferenceImages/");

            XmlElement OldImage = new XmlElement ();
            try { OldImage = _xmlLayout.GetElementById ("Image" + selectedImage.name); } catch { }
            XmlElement Preview = _xmlLayout.GetElementById ("PreviewImage");
            Preview.SetAndApplyAttribute ("image", _path + image.id.Remove (0, 5));

            selectedImage = new Texture2D (0, 0);
            selectedImage.LoadImage (File.ReadAllBytes (_path + image.id.Remove (0, 5)));
            selectedImage.name = image.id.Remove (0, 5);

            image.SetAndApplyAttribute ("colors", "ButtonPressed|ButtonHover|ButtonHover|ButtonDisabled");
            try { OldImage.SetAndApplyAttribute ("colors", "Button|ButtonHover|ButtonHover|ButtonDisabled"); } catch { }
            _imageConfirmButton.SetActive (true);
        }

        private void OnImageConfirm () {
            ReferenceImage refimage = GetReferenceImage (selectedView);
            if (refimage != null) refimage.UpdateImage (selectedImage);
            else _referenceImages.Add (new ReferenceImage (selectedView, selectedImage, this));

            _xmlLayout.GetElementById (selectedView + "Settings").SetActive (true);
            _xmlLayout.GetElementById (selectedView + "NoImageSelected").SetActive (false);
            _xmlLayout.GetElementById ("Select" + selectedView).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Edit Image");

            _selectingImage = false;
            _imageSelector.SetActive (false);
        }

        private void OnDeleteImageClicked (string view) {
            ReferenceImage refimage = GetReferenceImage (view);
            OnCloseEditMode (view);
            refimage?.Destroy ();
            _referenceImages.Remove (refimage);

            _xmlLayout.GetElementById (view + "Settings").SetActive (false);
            _xmlLayout.GetElementById (view + "NoImageSelected").SetActive (true);
            _xmlLayout.GetElementById ("Select" + view).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Select Image");
        }

        private void UpdateList (String List, List<String> ListItems) {
            XmlElement scrollView = _xmlLayout.GetElementById (List + "List");
            XmlElement ListItemTemplate = _xmlLayout.GetElementById (List + "template");
            List<string> _ListItems = new List<string> ();

            foreach (String item in ListItems) {
                string _item = item.Split ('/').Last ();
                _ListItems.Add (_item);

                if (_xmlLayout.GetElementById (List + _item) == null) {

                    //Debug.Log ("Adding New item to " + List + " list : " + _item);

                    XmlElement ListItem = GameObject.Instantiate (ListItemTemplate);
                    ListItem.name = List + _item;
                    XmlElement component = ListItem.GetComponent<XmlElement> ();

                    component.Initialise (_xmlLayout, (RectTransform) ListItem.transform, ListItemTemplate.tagHandler);
                    scrollView.AddChildElement (component);

                    component.SetAttribute ("active", "true");
                    component.SetAttribute ("id", List + _item);
                    component.GetElementByInternalId<TextMeshProUGUI> (List + "Name").text = _item;
                    component.ApplyAttributes ();
                }
            }

            List<Button> buttons = scrollView.GetComponentsInChildren<Button> ().ToList ();
            List<XmlElement> xmlElements = scrollView.GetComponentsInChildren<XmlElement> ().ToList ();

            for (int i = 0; i < buttons.Count; i++) {
                if (!_ListItems.Contains (buttons[i].GetComponentInChildren<TextMeshProUGUI> ().text)) scrollView.RemoveChildElement (xmlElements[i]);
            }
        }

        private void OnToggleImageClicked (XmlElement image) {
            string view = image.id.Remove (0, 6);
            ReferenceImage refimage = GetReferenceImage (view);
            refimage?.Toggle ();
            refimage?.EditMode (false);
            if (refimage.active) image.SetAndApplyAttribute ("color", "White");
            else image.SetAndApplyAttribute ("color", "Button");
        }

        private void OnRefImageSettingChanged (XmlElement inputfield) {
            String[] id = inputfield.id.Split ('-');
            string view = id.First ();
            string setting = id.Last ();
            float value = float.Parse (inputfield.GetValue ());

            GetReferenceImage (view)?.UpdateValue (setting, value);
        }

        private void OnMoveImageButtonClicked (string view) {
            GetReferenceImage (view).OnMoveImage ();
        }

        private void OnRotateImageButtonClicked (string view) {
            GetReferenceImage (view).OnRotateImage ();
        }

        private void OnCloseEditMode (string view) {
            GetReferenceImage (view).EditMode (false);
            _xmlLayout.GetElementById ("Select" + view).SetActive (true);
            _xmlLayout.GetElementById ("EditModeSettings" + view).SetActive (false);
        }

        private void OnPin (XmlElement Panel) {
            viewToolPanelPinned = !viewToolPanelPinned;
            if (viewToolPanelPinned) _viewToolsPin.SetAndApplyAttribute ("color", "Primary");
            else _viewToolsPin.SetAndApplyAttribute ("color", "labeltext");
        }

        private void OnClose (XmlElement panel) {
            if (panel.id == "ImageSelectorClose") {
                _selectingImage = false;
                _imageSelector.SetActive (false);
            }
        }

        private void OnOrthoToggleButtonClicked () {
            _orthoViewActive = !_orthoViewActive;
            _designerCamera.orthographic = _orthoViewActive;
            _designer.GizmoCamera.orthographic = _orthoViewActive;
            _zoomPanel.SetActive (_orthoViewActive);
            if (_orthoViewActive) _mod.viewCube?.OnOrthoSizeChanged (_orthoSize);
            else _mod.viewCube?.OnOrthoOff ();
        }

        private void OnZoomMinusClicked () {
            _orthoSize++;
            _designerCamera.orthographicSize = _orthoSize;
            _designer.GizmoCamera.orthographicSize = _orthoSize;
            _mod.viewCube?.OnOrthoSizeChanged (_orthoSize);
        }
        private void OnZoomPlusButtonClicked () {
            _orthoSize--;
            if (_orthoSize < 0.1f) _orthoSize = 0.1f;
            _designerCamera.orthographicSize = _orthoSize;
            _designer.GizmoCamera.orthographicSize = _orthoSize;
            _mod.viewCube?.OnOrthoSizeChanged (_orthoSize);
        }

        private ReferenceImage GetReferenceImage (string view) {
            return referenceImages.Find (image => image.view == view);
        }
    }
}