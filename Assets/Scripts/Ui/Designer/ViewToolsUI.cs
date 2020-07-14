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
using Assets.Scripts.Input;
using Assets.Scripts.Ui;
using Assets.Scripts.Ui.Inspector;
using ModApi;
using ModApi.Common;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using ModApi.Design;
using ModApi.Design.PartProperties;
using ModApi.Input;
using ModApi.Math;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.DesignerTools {
    public class ViewToolsUI : MonoBehaviour {
        private IXmlLayoutController _controller;
        private bool OrthoViewActive;
        private bool SelectingImage = false;
        private bool MouseDown = false;
        public bool ViewToolPanelPinned = false;
        private Camera DesignerCamera = Game.Instance.Designer.DesignerCamera.Camera;
        private float OrthoSize;
        private MouseDrag _MouseDrag;
        private String _Path = Mod.Instance.RefImagePath;
        private List<String> _Images => (Directory.EnumerateFiles (_Path, "*.png").Union (Directory.EnumerateFiles (_Path, "*.jpg")).Union (Directory.EnumerateFiles (_Path, "*.jpeg"))).ToList ();
        private List<ReferenceImage> _ReferenceImages = new List<ReferenceImage> ();
        public List<ReferenceImage> ReferenceImages => _ReferenceImages;
        public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private DesignerPartList _DesignerParts;
        private DesignerPart _Fuselage = new DesignerPart ();
        private PartScript _ReferencePart;
        private XmlLayout _XmlLayout;
        private XmlElement _ZoomPanel;
        private XmlElement _ImageSelector;
        private XmlElement _ViewToolsPin;
        private XmlElement _ImageConfirmButton;
        private Texture2D SelectedImage;
        private string SelectedView;

        public void OnLayoutRebuilt (IXmlLayoutController xmlLayoutController) {
            OrthoSize = DesignerCamera.orthographicSize;
            _MouseDrag = new MouseDrag (_Designer.GizmoCamera);
            Mod.Instance.ViewToolsUI = this;

            _controller = xmlLayoutController;
            _XmlLayout = (XmlLayout) _controller.XmlLayout;
            _ZoomPanel = _XmlLayout.GetElementById ("ZoomPanel");
            _ImageSelector = _XmlLayout.GetElementById ("ImageSelector");
            _ViewToolsPin = _XmlLayout.GetElementById ("ViewToolsPin");
            _ImageConfirmButton = _XmlLayout.GetElementById ("ImageConfirmButton");
            _XmlLayout.GetElementById ("FolderPathText").SetAndApplyAttribute ("text", "Folder location : " + _Path);

            if (DesignerCamera.orthographic == true) {
                XmlElement OrthoToggle = _XmlLayout.GetElementById ("OrthoToggle");
                OrthoToggle.SetAndApplyAttribute ("isOn", "true");
                _ZoomPanel.SetActive (true);
                OrthoViewActive = true;
            }

            UpdateReferenceImages (Mod.Instance.ReferenceImages);

            _DesignerParts = Game.Instance.CachedDesignerParts;
            foreach (DesignerPart part in _DesignerParts.Parts) {
                if (part.PartTypes.First ().Id == "Fuselage1") {
                    _Fuselage = part;
                }
            }

            foreach (PartData part in _Designer.CraftScript.Data.Assembly.Parts) {
                PartScript _part = (PartScript) part.PartScript;
                if (_part.name == "DesignerToolsRefPart") {
                    _Designer.SelectPart (_part, null, false);
                    _Designer.DeleteSelectedParts ();
                }
            }
        }

        public void UpdateReferenceImages (List<ReferenceImage> Images) {
            foreach (ReferenceImage image in _ReferenceImages) {
                image.Destroy ();
            }

            _ReferenceImages = Images;

            foreach (ReferenceImage image in _ReferenceImages) {
                if (image.ViewToolsUI == null) image.ViewToolsUI = this;
                image.ApplyChanges ();

                if (!image.Active) {
                    XmlElement ToggleButton = _XmlLayout.GetElementById ("Toggle" + image.View);
                    ToggleButton.SetAndApplyAttribute ("color", "Button");
                }

                _XmlLayout.GetElementById (image.View + "Settings").SetActive (true);
                _XmlLayout.GetElementById (image.View + "NoImageSelected").SetActive (false);
                _XmlLayout.GetElementById ("Select" + image.View).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Edit Image");
            }
        }

        public void Close () {
            foreach (ReferenceImage image in _ReferenceImages) {
                image.EditMode (false);
            }
            Mod.Instance.OnViewPanelClosed (_ReferenceImages);
            _XmlLayout.Hide (() => Destroy (this.gameObject), true);
        }

        protected virtual void Update () {
            if (UnityEngine.Input.GetMouseButtonDown (0)) {
                MouseDown = true;
                foreach (ReferenceImage image in _ReferenceImages) {
                    if (image.EditModeOn) image.OnMouseStart ();
                }
            } else if (UnityEngine.Input.GetMouseButtonUp (0)) {
                MouseDown = false;
                foreach (ReferenceImage image in _ReferenceImages) {
                    if (image.EditModeOn) image.OnMouseEnd ();
                }
            }
            if (MouseDown) {
                foreach (ReferenceImage image in _ReferenceImages) {
                    if (image.EditModeOn) image.OnMouseDown ();
                }
            }
        }

        public void SetReferencePart (bool on) {
            bool editmode = false;
            foreach (ReferenceImage image in _ReferenceImages) {
                if (image.EditModeOn) { editmode = true; break; }
            }

            if (on && _ReferencePart == null) {
                _Designer.AddPart (_Fuselage, new Vector2 ());
                _ReferencePart = (PartScript) _Designer.CraftScript.Data.Assembly.Parts.Last ().PartScript;
                _ReferencePart.name = "DesignerToolsRefPart";
                _Designer.SelectPart (_ReferencePart, null, false);
            } else if (!on && _ReferencePart != null) {
                if (!editmode) {
                    _Designer.SelectPart (_ReferencePart, null, false);
                    _Designer.DeleteSelectedParts ();
                    _ReferencePart = null;
                }
            }
            _Designer.AllowPartSelection = !editmode;
        }

        private void OnViewButtonClicked (XmlElement button) {
            DesignerCameraViewDirection designerCameraViewDirection = DesignerCameraViewDirection.None;
            DesignerCameraScript designerCameraScript = Game.Instance.Designer.DesignerCamera as DesignerCameraScript;

            if (button.id.Contains ("Front")) designerCameraViewDirection = DesignerCameraViewDirection.Front;
            else if (button.id.Contains ("Back")) designerCameraViewDirection = DesignerCameraViewDirection.Back;
            else if (button.id.Contains ("Top")) designerCameraViewDirection = DesignerCameraViewDirection.Top;
            else if (button.id.Contains ("Bottom")) designerCameraViewDirection = DesignerCameraViewDirection.Bottom;
            else if (button.id.Contains ("Left")) designerCameraViewDirection = DesignerCameraViewDirection.Left;
            else if (button.id.Contains ("Right")) designerCameraViewDirection = DesignerCameraViewDirection.Right;

            designerCameraScript.SetViewDirection (designerCameraViewDirection);
        }

        private void OnSelectImageButtonClicked (XmlElement button) {
            if (SelectingImage == false) {
                string view = button.id.Remove (0, 6);

                ReferenceImage refimage = GetReferenceImage (view);
                if (refimage?.View != null) {
                    if (refimage.Active) refimage.EditMode (true);
                    if (refimage.EditModeOn) {
                        _XmlLayout.GetElementById ("Select" + view).SetActive (false);
                        _XmlLayout.GetElementById ("EditModeSettings" + view).SetActive (true);
                    } else {
                        _XmlLayout.GetElementById ("Select" + view).SetActive (true);
                        _XmlLayout.GetElementById ("EditModeSettings" + view).SetActive (false);
                    }
                    return;
                }

                SelectingImage = true;
                _ImageSelector.SetActive (true);
                _ImageConfirmButton.SetActive (false);
                UpdateList ("Image", _Images);
                SelectedView = view;
            }
        }

        private void OnImageSelected (XmlElement image) {
            if (_Path == null) _Path = (Application.persistentDataPath + "/mods/DesignerTools/ReferenceImages/");

            XmlElement OldImage = new XmlElement ();
            try { OldImage = _XmlLayout.GetElementById ("Image" + SelectedImage.name); } catch { }
            XmlElement Preview = _XmlLayout.GetElementById ("PreviewImage");
            Preview.SetAndApplyAttribute ("image", _Path + image.id.Remove (0, 5));

            SelectedImage = new Texture2D (0, 0);
            SelectedImage.LoadImage (File.ReadAllBytes (_Path + image.id.Remove (0, 5)));
            SelectedImage.name = image.id.Remove (0, 5);

            image.SetAndApplyAttribute ("colors", "ButtonPressed|ButtonHover|ButtonHover|ButtonDisabled");
            try { OldImage.SetAndApplyAttribute ("colors", "Button|ButtonHover|ButtonHover|ButtonDisabled"); } catch { }
            _ImageConfirmButton.SetActive (true);
        }

        private void OnImageConfirm () {
            ReferenceImage refimage = GetReferenceImage (SelectedView);
            if (refimage != null) refimage.UpdateImage (SelectedImage);
            else _ReferenceImages.Add (new ReferenceImage (SelectedView, SelectedImage, this));

            _XmlLayout.GetElementById (SelectedView + "Settings").SetActive (true);
            _XmlLayout.GetElementById (SelectedView + "NoImageSelected").SetActive (false);
            _XmlLayout.GetElementById ("Select" + SelectedView).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Edit Image");

            SelectingImage = false;
            _ImageSelector.SetActive (false);
        }

        private void OnDeleteImageClicked (string view) {
            ReferenceImage refimage = GetReferenceImage (view);
            OnCloseEditMode (view);
            refimage?.Destroy ();
            _ReferenceImages.Remove (refimage);

            _XmlLayout.GetElementById (view + "Settings").SetActive (false);
            _XmlLayout.GetElementById (view + "NoImageSelected").SetActive (true);
            _XmlLayout.GetElementById ("Select" + view).GetComponentInChildren<TextMeshProUGUI> ().SetText ("Select Image");
        }

        private void UpdateList (String List, List<String> ListItems) {
            XmlElement scrollView = _XmlLayout.GetElementById (List + "List");
            XmlElement ListItemTemplate = _XmlLayout.GetElementById (List + "template");
            List<string> _ListItems = new List<string> ();

            foreach (String item in ListItems) {
                string _item = item.Split ('/').Last ();
                _ListItems.Add (_item);

                if (_XmlLayout.GetElementById (List + _item) == null) {

                    //Debug.Log ("Adding New item to " + List + " list : " + _item);

                    XmlElement ListItem = GameObject.Instantiate (ListItemTemplate);
                    ListItem.name = List + _item;
                    XmlElement component = ListItem.GetComponent<XmlElement> ();

                    component.Initialise (_XmlLayout, (RectTransform) ListItem.transform, ListItemTemplate.tagHandler);
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
            if (refimage.Active) image.SetAndApplyAttribute ("color", "White");
            else image.SetAndApplyAttribute ("color", "Button");
        }

        private void OnRefImageSettingChanged (XmlElement inputfield) {
            String[] id = inputfield.id.Split ('/');
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
            _XmlLayout.GetElementById ("Select" + view).SetActive (true);
            _XmlLayout.GetElementById ("EditModeSettings" + view).SetActive (false);
        }

        private void OnPin (XmlElement Panel) {
            ViewToolPanelPinned = !ViewToolPanelPinned;
            if (ViewToolPanelPinned) _ViewToolsPin.SetAndApplyAttribute ("color", "Primary");
            else _ViewToolsPin.SetAndApplyAttribute ("color", "labeltext");
        }

        private void OnClose (XmlElement panel) {
            if (panel.id == "ImageSelectorClose") {
                SelectingImage = false;
                _ImageSelector.SetActive (false);
            }
        }

        private void OnOrthoToggleButtonClicked () {
            OrthoViewActive = !OrthoViewActive;
            DesignerCamera.orthographic = OrthoViewActive;
            _Designer.GizmoCamera.orthographic = OrthoViewActive;
            _ZoomPanel.SetActive (OrthoViewActive);
        }

        private void OnZoomMinusClicked () {
            OrthoSize++;
            DesignerCamera.orthographicSize = OrthoSize;
            _Designer.GizmoCamera.orthographicSize = OrthoSize;
        }
        private void OnZoomPlusButtonClicked () {
            OrthoSize--;
            if (OrthoSize <= 0) OrthoSize = 0.1f;
            DesignerCamera.orthographicSize = OrthoSize;
        }

        private ReferenceImage GetReferenceImage (string view) {
            foreach (ReferenceImage image in _ReferenceImages) {
                if (image.View == view) return image;
            }
            return null;
        }
    }
}