using System;
using System.Linq;
using Assets.Scripts.Design;
using Assets.Scripts.Tools.ObjectTransform;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using Assets.Scripts.Ui.Designer;
using UnityEngine.UI;
using ModApi.Design;
using Assets.Scripts.Design.Tools;
using System.Collections.Generic;
using HarmonyLib;
using Assets.Scripts.DesignerTools.ViewTools;
using ModApi.Craft;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public enum Views { Front, Back, Top, Bottom, Left, Right }

    public class ReferenceImage : MonoBehaviour
    {
        public Views view;
        public IUIResourceDatabase resourceDatabase => XmlLayoutResourceDatabase.instance;
        public DesignerScript designer => (DesignerScript)Game.Instance.Designer;
        public ReferenceImagesPanel refImgsPanel;
        public Texture2D image;
        private GameObject _parentGameObject, _imageGameObject, _image;
        private Renderer _renderer;
        private XmlElement _selectImageButton, _edit, _settings, _noImage, _nudge, _rotate, _scale;
        private ImageHighlighter imageHighlighter;
        private ReferenceImageGizmos _gizmos;
        private DesignerTool[] tools;
        private static Material _defaultMaterial;
        private AutoScaler autoScaler;
        public bool translateModeOn => gizmosCreated && _gizmos.translateGizmo.GizmosCreated;
        public bool rotateModeOn => gizmosCreated && _gizmos.rotateGizmo.GizmosCreated;
        public bool scaleModeOn => autoScaler != null;
        private bool gizmosCreated => _gizmos != null;
        public static Dictionary<Views, Quaternion> rotations = new Dictionary<Views, Quaternion>(){
            {Views.Front, Quaternion.Euler(90f, -90f, -90f)},
            {Views.Back, Quaternion.Euler(90f, 270f, 90f)},
            {Views.Top, Quaternion.Euler(0f, -90f, 0f)},
            {Views.Bottom, Quaternion.Euler(180f, 90f, 0f)},
            {Views.Left, Quaternion.Euler(90f, 0f, 90f)},
            {Views.Right, Quaternion.Euler(90f, 0f, -90f)}};
        public string imageName = "No Image";
        public float rotation { get; private set; } = 0f;
        public float offsetX { get; private set; } = 0f;
        public float offsetY { get; private set; } = 0f;
        public float offsetZ { get; private set; } = 0f;
        public float scale { get; private set; } = 1f;
        public float opacity { get; private set; } = 0.3f;
        public bool visible => active && !hidden;
        public bool active = true, hidden = false;
        public bool hasImage => _imageGameObject != null;
        private bool _uiActive = false;
        public bool missingImage = false;
        public const string deleteSprite = "Ui/Sprites/Design/IconButtonDeleteItem";
        public const string nugdeSprite = "Ui/Sprites/Design/IconButtonNudgeTool";
        public const string rotateSprite = "Ui/Sprites/Design/IconButtonRotateTool";
        public const string scaleSprite = "Ui/Sprites/Design/IconButtonFuselageShapeTool";
        public const string refImagePrefix = "ReferenceImage_";

        public static ReferenceImage GetReferenceImage(Views view) => Mod.Instance.referenceImages.Where(img => img.view == view).First();

        public ReferenceImage Initialise(Views view)
        {
            this.view = view;
            return this;
        }

        public ReferenceImage Initialise(Views view, string imageName)
        {
            Initialise(view);
            this.missingImage = true;
            this.imageName = imageName;
            return this;
        }

        public ReferenceImage Initialise(Views view, Texture2D image)
        {
            Initialise(view);
            SetImage(image);
            return this;
        }

        public void ResetSettings()
        {
            rotation = offsetX = offsetY = offsetZ = 0;
            scale = 1;
            opacity = 0.3f;
            active = true;
        }

        public void SetImage(Texture2D image)
        {
            imageName = image.name;
            _parentGameObject = new GameObject();
            _parentGameObject.transform.SetParent(Mod.Instance.referenceImagesParent, false);
            _imageGameObject = new GameObject();
            _imageGameObject.transform.SetParent(_parentGameObject.transform, false);
            _image = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _image.name = refImagePrefix + view.ToString();
            _image.transform.SetParent(_imageGameObject.transform, true);
            _imageGameObject.SetActive(true);
            _renderer = _image.GetComponent<Renderer>();
            _renderer.receiveShadows = false;

            _parentGameObject.transform.rotation = rotations[view];
            //_parentGameObject.transform.localPosition = new Vector3();

            UpdateImage(image);
        }

        public void UpdateImage(Texture2D image)
        {
            if (!hasImage) SetImage(image);
            else
            {
                this.image = image;
                if (_defaultMaterial == null) _defaultMaterial = resourceDatabase.GetResource<Material>("DesignerTools/RefImageMaterial");

                try
                {
                    _renderer.material = new Material(_defaultMaterial)
                    {
                        color = new Color(1f, 1f, 1f, opacity),
                        mainTexture = this.image
                    };
                }
                catch (Exception e) { Debug.LogError("material Error: " + e); }

                ApplyChanges();
            }

            UpdateUI();
        }

        public void InitialiseUI(ReferenceImagesPanel refImgsPanel)
        {
            if (_uiActive) OnUIClosed();

            this.refImgsPanel = refImgsPanel;
            _uiActive = true;

            _selectImageButton = refImgsPanel.AddItem(refImgsPanel.imageTextTemplate, null, refImgsPanel.xmlLayout, view + "text");
            _edit = refImgsPanel.AddItem(refImgsPanel.imageEditTemplate, null, refImgsPanel.xmlLayout, view + "edit");
            _settings = refImgsPanel.AddItem(refImgsPanel.imageSettingsTemplate, null, refImgsPanel.xmlLayout, view + "settings");
            _noImage = refImgsPanel.AddItem(refImgsPanel.noImageTemplate, null, refImgsPanel.xmlLayout, view + "noImage");

            _selectImageButton.AddOnClickEvent(delegate { OnSelectImageButtonClicked(); });
            _nudge = _edit.GetElementByInternalId("nudge");
            _nudge.AddOnClickEvent(delegate { OnTranslateImage(); });
            _rotate = _edit.GetElementByInternalId("rotate");
            _rotate.AddOnClickEvent(delegate { OnRotateImage(); });
            _scale = _edit.GetElementByInternalId("scale");
            _scale.AddOnClickEvent(delegate { OnScaleImage(); });
            _edit.GetElementByInternalId("move-up").AddOnClickEvent(delegate { OnSwitchImage(-1); });
            _edit.GetElementByInternalId("move-down").AddOnClickEvent(delegate { OnSwitchImage(1); });
            _settings.GetElementByInternalId("delete").AddOnClickEvent(delegate { Destroy(); });
            _settings.GetElementByInternalId("toggle").AddOnClickEvent(delegate { Toggle(); });
            _settings.GetComponentsInChildren<TMP_InputField>().ToList().ForEach(inputField =>
            {
                inputField.onEndEdit.AddListener(delegate (string s)
                {
                    UpdateValue(inputField.gameObject.GetComponent<XmlElement>().internalId, float.Parse(s));
                });
            });

            UpdateUI();
        }

        private void OnTranslateImage()
        {
            if (_gizmos == null)
            {
                _gizmos = new ReferenceImageGizmos(designer);
                _gizmos.ToolAdjustmentOccurred += OnGizmoAdjusted;
            }

            if (translateModeOn) ToggleTranslateTool(false);
            else if (hasImage && active) ToggleTranslateTool(true, true);
        }

        private void OnRotateImage()
        {
            if (_gizmos == null)
            {
                _gizmos = new ReferenceImageGizmos(designer);
                _gizmos.ToolAdjustmentOccurred += OnGizmoAdjusted;
            }

            if (rotateModeOn) ToggleRotateTool(false);
            else if (hasImage && active) ToggleRotateTool(true);
        }

        private void OnScaleImage()
        {

            if (scaleModeOn) ToggleScaleTool(false);
            else if (hasImage && active) ToggleScaleTool(true);
        }

        private void OnSetScale(float s)
        {
            UpdateValue("Scale", Mathf.Round(s * scale * 1000f) / 1000f);
            ToggleScaleTool(false);
        }

        private void SelectImage()
        {
            if (imageHighlighter == null) imageHighlighter = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/ImageHighlighter/ImageHighlighter.prefab")).GetComponent<ImageHighlighter>();
            imageHighlighter.Toggle(true);
            ApplyChanges();
            designer.DeselectPart();
            designer.AllowPartSelection = false;

            if (gizmosCreated)
            {
                DeleteUnecessaryGizmos();
                designer.SelectTool(_gizmos);
                GizmoIsLocalOrientationChanged();
            }
        }

        private void DeselectImage()
        {
            if (imageHighlighter != null) imageHighlighter.Toggle(false);
            if (gizmosCreated) designer.DeselectTool(_gizmos);
            designer.AllowPartSelection = true;
        }

        public void ToggleTranslateTool(bool active, bool changeSprite = false)
        {
            if (active)
            {
                ToggleAllToolOff();
                _gizmos.translateGizmo.SetAdjustmentTransform(_imageGameObject.transform, true);
                _nudge.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = deleteSprite.ToSprite(reportError: false);
                SelectImage();
            }
            else if (translateModeOn)
            {
                _gizmos?.translateGizmo.SetAdjustmentTransform(null, true);
                _nudge.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = nugdeSprite.ToSprite(reportError: false); ;
                DeselectImage();
            }
        }

        public void ToggleRotateTool(bool active)
        {
            if (active)
            {
                ToggleAllToolOff();
                _gizmos.rotateGizmo.SetAdjustmentTransform(_imageGameObject.transform, true);
                _rotate.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = deleteSprite.ToSprite(reportError: false);
                SelectImage();
            }
            else if (rotateModeOn)
            {
                _gizmos?.rotateGizmo.SetAdjustmentTransform(null, true);
                _rotate.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = rotateSprite.ToSprite(reportError: false);
                DeselectImage();
            }
        }

        public void ToggleScaleTool(bool active)
        {
            if (active)
            {
                ToggleAllToolOff();
                autoScaler = new GameObject().AddComponent<AutoScaler>().Initialise(view, _imageGameObject.transform, (s) => OnSetScale(s));
                _scale.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = deleteSprite.ToSprite(reportError: false);
                ViewToolsUtilities.IsolateParts(new List<string>());
                ViewToolsUtilities.IsolateRefImages(new List<ReferenceImage>() { this });
                SelectImage();
            }
            else if (scaleModeOn)
            {
                autoScaler.DestroyPointers();
                GameObject.DestroyImmediate(autoScaler.gameObject);
                autoScaler = null;
                _scale.GetChildElementsWithClass("toggle-button-icon").First().GetComponent<Image>().sprite = scaleSprite.ToSprite(reportError: false);
                ViewToolsUtilities.SetNormalView();
                DeselectImage();
            }
        }

        private void ToggleAllToolOff()
        {
            foreach (ReferenceImage img in Mod.Instance.referenceImages)
            {
                if (img.hasImage)
                {
                    img.ToggleRotateTool(false);
                    img.ToggleTranslateTool(false);
                    img.ToggleScaleTool(false);
                }
            }
        }

        private void OnGizmoAdjusted(MovementTool source)
        {
            offsetX = Mathf.Round(_imageGameObject.transform.localPosition.x * 1000f) / 1000f;
            offsetY = Mathf.Round(_imageGameObject.transform.localPosition.z * 1000f) / 1000f;
            offsetZ = Mathf.Round(_imageGameObject.transform.localPosition.y * 1000f) / 1000f;
            rotation = Mathf.Round(_imageGameObject.transform.localRotation.eulerAngles.y * 1000f) / 1000f;

            ApplyChanges();
        }

        public void GizmoIsLocalOrientationChanged()
        {
            if (gizmosCreated)
            {
                if (_gizmos.translateGizmo.GizmosActive) _gizmos.translateGizmo.IsLocalOrientation = Mod.Instance.imageGizmoIsLocalOrientation;
                if (_gizmos.rotateGizmo.GizmosActive) _gizmos.rotateGizmo.IsLocalOrientation = Mod.Instance.imageGizmoIsLocalOrientation;
                DeleteUnecessaryGizmos();
            }
        }

        private void DeleteUnecessaryGizmos()
        {
            if (gizmosCreated)
            {
                if (_gizmos.rotateGizmo.GizmosActive)
                {
                    Traverse.Create(_gizmos.rotateGizmo).Field("_xRotation").GetValue<RotateGizmoAxisScript>().gameObject.SetActive(false);
                    Traverse.Create(_gizmos.rotateGizmo).Field("_zRotation").GetValue<RotateGizmoAxisScript>().gameObject.SetActive(false);
                    Traverse.Create(_gizmos.rotateGizmo).Field("_xRotation").Field("_vectorLineRenderer").GetValue<MeshRenderer>().enabled = false;
                    Traverse.Create(_gizmos.rotateGizmo).Field("_zRotation").Field("_vectorLineRenderer").GetValue<MeshRenderer>().enabled = false;
                }
            }
        }

        private void OnSwitchImage(int offset)
        {
            if (!Mod.Instance.CraftValidForRefImg())
            {
                designer.DesignerUi.ShowMessage(Mod.Instance.errorColor + "Remember to save your craft, '" + designer.CraftScript.Data.Name + "' can't have reference images, switch feature is disabled");
                return;
            }

            int newView = (int)view + offset;
            while (newView > 5) newView -= 6;
            while (newView < 0) newView += 6;

            GetReferenceImage((Views)newView).view = view;
            Views olddView = view;
            view = (Views)newView;
            Mod.Instance.OnReferenceImageChanged();
            Mod.Instance.RefreshReferenceImages();
        }


        private void OnSelectImageButtonClicked()
        {
            refImgsPanel.OnSelectImageButtonClicked(view);
        }

        public void UpdateUI()
        {
            if (_uiActive)
            {
                if (visible) _settings.GetElementByInternalId("toggle").SetAndApplyAttribute("color", "White");
                else _settings.GetElementByInternalId("toggle").SetAndApplyAttribute("color", "Button");

                _noImage.SetActive(!hasImage);
                _noImage.SetText(missingImage ? Mod.Instance.errorColor + "Missing Image" : "No Image Selected");
                _settings.SetActive(hasImage);

                UpdateUIButton();
                UpdateUIValues();
            }
        }

        public void UpdateUIButton()
        {
            if (_uiActive)
            {
                _selectImageButton.SetActive(!hasImage);
                _edit.SetActive(hasImage);

                if (hasImage) _selectImageButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Edit Image");
                else _selectImageButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select Image");
            }
        }

        public void UpdateUIValues()
        {
            if (_uiActive)
            {
                _settings.GetElementByInternalId<TMP_InputField>("OffsetX")?.SetTextWithoutNotify(offsetX.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("OffsetY")?.SetTextWithoutNotify(offsetY.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("OffsetZ")?.SetTextWithoutNotify(offsetZ.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Rotation")?.SetTextWithoutNotify(rotation.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Scale")?.SetTextWithoutNotify(scale.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Opacity")?.SetTextWithoutNotify(opacity.ToString());
            }
            Mod.Instance.OnReferenceImageChanged();
        }

        public void UpdateValue(string setting, float value)
        {
            if (setting == null) { Debug.LogError("Setting null error in UpdateValue"); return; }

            if (setting == "OffsetX") offsetX = value;
            else if (setting == "OffsetY") offsetY = value;
            else if (setting == "OffsetZ") offsetZ = value;
            else if (setting == "Rotation") rotation = value;
            else if (setting == "Scale") scale = value;
            else if (setting == "Opacity") opacity = value;
            else Debug.LogError("setting name not recognised : " + setting);

            ApplyChanges();
        }

        public void ApplyChanges()
        {
            if (hasImage)
            {
                Quaternion temp = _imageGameObject.transform.localRotation;

                _image.transform.localScale = new Vector3(scale * (image.width / 1000f), 1f, scale * (image.height / 1000f));
                _imageGameObject.transform.localRotation = Quaternion.Euler(temp.eulerAngles.x, rotation, temp.eulerAngles.z);
                _imageGameObject.transform.localPosition = new Vector3(offsetX, offsetZ, offsetY);
                _image.transform.localPosition = new Vector3();
                _renderer.material.color = new Color(1f, 1f, 1f, opacity);
                UpdateUIValues();

                if (gizmosCreated)
                {
                    _gizmos.SetWorldPosition(_imageGameObject.transform.position);
                    if (_gizmos.translateGizmo.GizmosActive || _gizmos.rotateGizmo.GizmosActive) imageHighlighter?.UpdateHighlighter(_imageGameObject.transform, image.width, image.height, scale);
                }
                if (scaleModeOn) imageHighlighter?.UpdateHighlighter(_imageGameObject.transform, image.width, image.height, scale);
            }
        }

        public void Toggle() => Toggle(!active);
        public void Toggle(bool active)
        {
            this.active = active;
            if (hasImage) _imageGameObject.SetActive(visible);
            UpdateUI();

            _gizmos?.OnClose();
            DeselectImage();
        }
        public void Hide(bool hide)
        {
            this.hidden = hide;
            Toggle(active);
        }

        public void OnUIClosed()
        {
            ToggleAllToolOff();
            _gizmos?.OnClose();
            GameObject.DestroyImmediate(imageHighlighter);
            imageHighlighter = null;
            this.refImgsPanel = null;
            _uiActive = false;
        }

        public void Destroy()
        {
            ToggleAllToolOff();
            _gizmos?.OnClose();
            GameObject.DestroyImmediate(imageHighlighter);
            imageHighlighter = null;
            if (hasImage)
            {
                UnityEngine.Object.DestroyImmediate(_parentGameObject);
                _parentGameObject = null;
                _imageGameObject = null;
                UpdateUI();
            }
            GameObject.Destroy(this);
        }
    }
}