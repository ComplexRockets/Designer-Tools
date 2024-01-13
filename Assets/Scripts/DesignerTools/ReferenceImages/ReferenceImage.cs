using System;
using System.Linq;
using Assets.Scripts.Design;
using Assets.Scripts.Tools.ObjectTransform;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;
using ModApi.Design;
using Assets.Scripts.Design.Tools;
using System.Collections.Generic;
using HarmonyLib;
using Assets.Scripts.DesignerTools.ViewTools;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public enum Views { Front, Back, Top, Bottom, Left, Right }

    public class ReferenceImage : MonoBehaviour
    {
        public Views view;
        public IUIResourceDatabase ResourceDatabase => XmlLayoutResourceDatabase.instance;
        public DesignerScript Designer => (DesignerScript)Game.Instance.Designer;
        public ReferenceImagesPanel refImgsPanel;
        public Texture2D image;
        private GameObject _parentGameObject, _imageGameObject, _image;
        private Renderer _renderer;
        private XmlElement _selectImageButton, _edit, _settings, _noImage, _nudge, _rotate, _scale;
        private ImageHighlighter imageHighlighter;
        private ReferenceImageGizmos _gizmos;
        private static Material _defaultMaterial;
        private AutoScaler autoScaler;
        public bool TranslateModeOn => GizmosCreated && _gizmos.translateGizmo.GizmosCreated;
        public bool RotateModeOn => GizmosCreated && _gizmos.rotateGizmo.GizmosCreated;
        public bool ScaleModeOn => autoScaler != null;
        private bool GizmosCreated => _gizmos != null;
        public static Dictionary<Views, Quaternion> rotations = new(){
            {Views.Front, Quaternion.Euler(90f, -90f, -90f)},
            {Views.Back, Quaternion.Euler(90f, 270f, 90f)},
            {Views.Top, Quaternion.Euler(0f, -90f, 0f)},
            {Views.Bottom, Quaternion.Euler(180f, 90f, 0f)},
            {Views.Left, Quaternion.Euler(90f, 0f, 90f)},
            {Views.Right, Quaternion.Euler(90f, 0f, -90f)}};
        public string imageName = "No Image";
        public float Rotation { get; private set; } = 0f;
        public float OffsetX { get; private set; } = 0f;
        public float OffsetY { get; private set; } = 0f;
        public float OffsetZ { get; private set; } = 0f;
        public float Scale { get; private set; } = 1f;
        public float Opacity { get; private set; } = 0.3f;
        public bool Visible => active && !hidden;
        public bool active = true, hidden = false;
        public bool HasImage => _imageGameObject != null;
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
            Rotation = OffsetX = OffsetY = OffsetZ = 0;
            Scale = 1;
            Opacity = 0.3f;
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
            if (!HasImage) SetImage(image);
            else
            {
                this.image = image;
                if (_defaultMaterial == null) _defaultMaterial = ResourceDatabase.GetResource<Material>("DesignerTools/RefImageMaterial");

                try
                {
                    _renderer.material = new Material(_defaultMaterial)
                    {
                        color = new Color(1f, 1f, 1f, Opacity),
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
                _gizmos = new ReferenceImageGizmos(Designer);
                _gizmos.ToolAdjustmentOccurred += OnGizmoAdjusted;
            }

            if (TranslateModeOn) ToggleTranslateTool(false);
            else if (HasImage && active) ToggleTranslateTool(true, true);
        }

        private void OnRotateImage()
        {
            if (_gizmos == null)
            {
                _gizmos = new ReferenceImageGizmos(Designer);
                _gizmos.ToolAdjustmentOccurred += OnGizmoAdjusted;
            }

            if (RotateModeOn) ToggleRotateTool(false);
            else if (HasImage && active) ToggleRotateTool(true);
        }

        private void OnScaleImage()
        {

            if (ScaleModeOn) ToggleScaleTool(false);
            else if (HasImage && active) ToggleScaleTool(true);
        }

        private void OnSetScale(float s)
        {
            UpdateValue("Scale", Mathf.Round(s * Scale * 1000f) / 1000f);
            ToggleScaleTool(false);
        }

        private void SelectImage()
        {
            if (imageHighlighter == null) imageHighlighter = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/ImageHighlighter/ImageHighlighter.prefab")).GetComponent<ImageHighlighter>();
            imageHighlighter.Toggle(true);
            ApplyChanges();
            Designer.DeselectPart();
            Designer.AllowPartSelection = false;

            if (GizmosCreated)
            {
                DeleteUnecessaryGizmos();
                Designer.SelectTool(_gizmos);
                GizmoIsLocalOrientationChanged();
            }
        }

        private void DeselectImage()
        {
            imageHighlighter?.Toggle(false);
            if (GizmosCreated) Designer.DeselectTool(_gizmos);
            Designer.AllowPartSelection = true;
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
            else if (TranslateModeOn)
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
            else if (RotateModeOn)
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
            else if (ScaleModeOn)
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
                if (img.HasImage)
                {
                    img.ToggleRotateTool(false);
                    img.ToggleTranslateTool(false);
                    img.ToggleScaleTool(false);
                }
            }
        }

        private void OnGizmoAdjusted(MovementTool source)
        {
            OffsetX = Mathf.Round(_imageGameObject.transform.localPosition.x * 1000f) / 1000f;
            OffsetY = Mathf.Round(_imageGameObject.transform.localPosition.z * 1000f) / 1000f;
            OffsetZ = Mathf.Round(_imageGameObject.transform.localPosition.y * 1000f) / 1000f;
            Rotation = Mathf.Round(_imageGameObject.transform.localRotation.eulerAngles.y * 1000f) / 1000f;

            ApplyChanges();
        }

        public void GizmoIsLocalOrientationChanged()
        {
            if (GizmosCreated)
            {
                if (_gizmos.translateGizmo.GizmosActive) _gizmos.translateGizmo.IsLocalOrientation = Mod.Instance.ImageGizmoIsLocalOrientation;
                if (_gizmos.rotateGizmo.GizmosActive) _gizmos.rotateGizmo.IsLocalOrientation = Mod.Instance.ImageGizmoIsLocalOrientation;
                DeleteUnecessaryGizmos();
            }
        }

        private void DeleteUnecessaryGizmos()
        {
            if (GizmosCreated)
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
                Designer.DesignerUi.ShowMessage(Mod.Instance.errorColor + "Remember to save your craft, '" + Designer.CraftScript.Data.Name + "' can't have reference images, switch feature is disabled");
                return;
            }

            int newView = (int)view + offset;
            while (newView > 5) newView -= 6;
            while (newView < 0) newView += 6;

            GetReferenceImage((Views)newView).view = view;
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
                if (Visible) _settings.GetElementByInternalId("toggle").SetAndApplyAttribute("color", "White");
                else _settings.GetElementByInternalId("toggle").SetAndApplyAttribute("color", "Button");

                _noImage.SetActive(!HasImage);
                _noImage.SetText(missingImage ? Mod.Instance.errorColor + "Missing Image" : "No Image Selected");
                _settings.SetActive(HasImage);

                UpdateUIButton();
                UpdateUIValues();
            }
        }

        public void UpdateUIButton()
        {
            if (_uiActive)
            {
                _selectImageButton.SetActive(!HasImage);
                _edit.SetActive(HasImage);

                if (HasImage) _selectImageButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Edit Image");
                else _selectImageButton.GetComponentInChildren<TextMeshProUGUI>().SetText("Select Image");
            }
        }

        public void UpdateUIValues()
        {
            if (_uiActive)
            {
                _settings.GetElementByInternalId<TMP_InputField>("OffsetX")?.SetTextWithoutNotify(OffsetX.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("OffsetY")?.SetTextWithoutNotify(OffsetY.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("OffsetZ")?.SetTextWithoutNotify(OffsetZ.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Rotation")?.SetTextWithoutNotify(Rotation.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Scale")?.SetTextWithoutNotify(Scale.ToString());
                _settings.GetElementByInternalId<TMP_InputField>("Opacity")?.SetTextWithoutNotify(Opacity.ToString());
            }
            Mod.Instance.OnReferenceImageChanged();
        }

        public void UpdateValue(string setting, float value)
        {
            if (setting == null) { Debug.LogError("Setting null error in UpdateValue"); return; }

            if (setting == "OffsetX") OffsetX = value;
            else if (setting == "OffsetY") OffsetY = value;
            else if (setting == "OffsetZ") OffsetZ = value;
            else if (setting == "Rotation") Rotation = value;
            else if (setting == "Scale") Scale = value;
            else if (setting == "Opacity") Opacity = value;
            else Debug.LogError("setting name not recognised : " + setting);

            ApplyChanges();
        }

        public void ApplyChanges()
        {
            if (HasImage)
            {
                Quaternion temp = _imageGameObject.transform.localRotation;

                _image.transform.localScale = new Vector3(Scale * (image.width / 1000f), 1f, Scale * (image.height / 1000f));
                _imageGameObject.transform.localRotation = Quaternion.Euler(temp.eulerAngles.x, Rotation, temp.eulerAngles.z);
                _imageGameObject.transform.localPosition = new Vector3(OffsetX, OffsetZ, OffsetY);
                _image.transform.localPosition = new Vector3();
                _renderer.material.color = new Color(1f, 1f, 1f, Opacity);
                UpdateUIValues();

                if (GizmosCreated)
                {
                    _gizmos.SetWorldPosition(_imageGameObject.transform.position);
                    if (_gizmos.translateGizmo.GizmosActive || _gizmos.rotateGizmo.GizmosActive) imageHighlighter?.UpdateHighlighter(_imageGameObject.transform, image.width, image.height, Scale);
                }
                if (ScaleModeOn) imageHighlighter?.UpdateHighlighter(_imageGameObject.transform, image.width, image.height, Scale);
            }
        }

        public void Toggle() => Toggle(!active);
        public void Toggle(bool active)
        {
            this.active = active;
            if (HasImage) _imageGameObject.SetActive(Visible);
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
            DestroyImmediate(imageHighlighter);
            imageHighlighter = null;
            refImgsPanel = null;
            _uiActive = false;
        }

        public void Destroy()
        {
            ToggleAllToolOff();
            _gizmos?.OnClose();
            DestroyImmediate(imageHighlighter);
            imageHighlighter = null;
            if (HasImage)
            {
                DestroyImmediate(_parentGameObject);
                _parentGameObject = null;
                _imageGameObject = null;
                UpdateUI();
            }
            Destroy(this);
        }
    }
}