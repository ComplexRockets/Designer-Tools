namespace Assets.Scripts {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design.Paint;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.DevConsole;
    using Assets.Scripts.Tools;
    using Assets.Scripts.Ui.Designer;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Input.Events;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Settings.Core.Events;
    using ModApi.Ui;
    using ModApi;
    using UI.Xml;
    using UnityEngine.UI;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod {
        public DesignerScript designer => (DesignerScript) Game.Instance.Designer;
        public Camera designerCamera => designer.DesignerCamera.Camera;
        public Camera gizmoCamera => designer.GizmoCamera;
        public PartToolsManager partTools = new PartToolsManager ();
        public PartSelectorManager selectorManager = new PartSelectorManager ();
        public ViewToolsUI viewToolsUI;
        public List<ReferenceImage> referenceImages {
            get {
                if (viewToolsUI != null) _referenceImages = viewToolsUI.referenceImages;
                return _referenceImages;
            }
        }
        public List<ReferenceImage> _referenceImages = new List<ReferenceImage> ();
        private Vector3 rootPosition => designer.CraftScript.RootPart.Transform.position;
        private Vector3 _origin = new Vector3 ();
        private ViewCube _viewCube;
        public ViewCube viewCube => _viewCube;
        private ColorPickerButtonScript _colorPickerButton;
        private DataManager _dataManager = new DataManager ();
        private DesignerToolsUI _designerToolsUI => Ui.Designer.DesignerToolsUIController.designerToolsUI;
        public string refImagePath;
        public string errorColor = "<color=#b33e46>";
        public int craftXMLVersion;
        public bool craftLoaded = false;
        public bool orthoOn = false;

        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod () : base () { }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod> ();

        protected override void OnModInitialized () {
            refImagePath = Application.persistentDataPath + "/UserData/DesignerTools/ReferenceImages/";
            System.IO.Directory.CreateDirectory (refImagePath);

            base.OnModInitialized ();
            Ui.Designer.DesignerToolsUIController.Initialize ();
            _dataManager.initialise ();
            partTools.Initialize (selectorManager);

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;
            ModSettings.Instance.Changed += OnSettingsChanged;
            //Debug.Log ("Mod Initialized");
            //Debug.Log ("PartTTool: " + PartTools.ToString ());
        }

        public void OnSceneLoaded (object sender, SceneEventArgs e) {
            //Debug.Log (e.Scene + " Loaded (mod.cs)");
            if (e.Scene == ModApi.Scenes.SceneNames.Designer) {
                designer.CraftLoaded += OnCraftLoaded;
                designer.BeforeCraftUnloaded += OnCraftUnloading;
                designer.CraftStructureChanged += OnCraftStructureChanged;
                designer.Click += OnClick;
                DesignerToolsUIController.OnDesignerLoaded ();
                selectorManager.OnDesignerLoaded ();
                if (ModSettings.Instance.viewCube && _viewCube == null) _viewCube = new ViewCube (designer);

                // IFlyout flyout = Game.Instance.Designer.DesignerUi.Flyouts.Tools;
                // IXmlLayout layout = flyout.Transform.GetComponentInChildren<IXmlLayout> ().GetElementById ("PaintTool").XmlLayout;
                // Debug.Log ("Paint Tool Panel: " + layout.Xml);
                // Debug.Log (layout.ToString ());
                // RectTransform root = layout.GetElementById<RectTransform> ("edit-color-panel");
                // Debug.Log ("panel: " + root.ToString ());

                // _ColorPickerButton = Game.Instance.UserInterface.BuildUserInterfaceFromResource<ColorPickerButtonScript> ("DesignerTools/Designer/ColorPickerButton", (s, c) => { s.OnLayoutRebuilt (c.XmlLayout); }, root);
                // _ColorPickerButton.gameObject.AddComponent<LayoutElement> ().minHeight = 30;
                // _ColorPickerButton.transform.SetAsLastSibling ();
            }
        }

        public void DesignerUpdate () {
            if (orthoOn) {
                float zoom = designer.DesignerCamera.CurrentZoom / 2f;
                designerCamera.orthographicSize = zoom;
                gizmoCamera.orthographicSize = zoom;
                if (ModSettings.Instance.viewCube) viewCube?.OnOrthoSizeChanged (zoom);
            }
            if (ModSettings.Instance.viewCube) _viewCube?.Update ();
        }

        public void OnSettingsChanged (object sender, SettingsChangedEventArgs<ModSettings> e) {
            //Debug.Log ("Settings Chaned");
            if (Game.InDesignerScene) {
                foreach (Transform child in designer.DesignerCamera.Camera.transform.GetComponentsInChildren<Transform> ()) {
                    if (child.gameObject.name == "ViewCube(Clone)") {
                        foreach (Transform c in child.gameObject.GetComponentsInChildren<Transform> ()) {
                            GameObject.Destroy (c.gameObject);
                        }
                        _viewCube = null;
                    }
                }

                selectorManager.OnSettingChanged ();
                if (ModSettings.Instance.viewCube) {
                    if (_viewCube == null) _viewCube = new ViewCube (designer);
                    _viewCube.updateScale ();
                }
            }
        }
        public void OnViewPanelClosed (List<ReferenceImage> referenceImages) {
            _referenceImages = referenceImages;
            viewToolsUI = null;
        }

        public void OnSceneUnloading (object sender, SceneEventArgs e) {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer) {
                //Debug.Log (e.Scene + " Unloading (mod.cs)");
                designer.CraftLoaded -= OnCraftLoaded;
                _viewCube?.Destroy ();
                _viewCube = null;
            }
        }

        public void OnCraftLoaded () {
            List<ReferenceImage> Images = _dataManager.LoadImages (designer.CraftScript.Data.Name);

            if (referenceImages != null) {
                referenceImages.ForEach (image => image.Destroy ());
            }

            _referenceImages = Images != null? Images : new List<ReferenceImage> ();
            _origin = rootPosition;

            // foreach (ReferenceImage image in ReferenceImages) {
            //     image.UpdateOrigin (_Origin);
            // }

            if (viewToolsUI != null) viewToolsUI.UpdateReferenceImages (_referenceImages);
            craftXMLVersion = designer.CraftScript.Data.XmlVersion;
            craftLoaded = true;
        }

        public void OnCraftUnloading () {
            craftLoaded = false;
        }

        private void OnCraftStructureChanged () {
            if (designer.CraftScript != null && rootPosition != _origin) {

                _origin = rootPosition;
                foreach (ReferenceImage image in referenceImages) {
                    image.UpdateOrigin (_origin);
                }
            }
        }

        public void OnSaveRefImages () {
            if (designer.CraftScript.Data.Name != "New") {
                if (referenceImages.Count > 0) {
                    _dataManager.SaveImages (designer.CraftScript.Data.Name, referenceImages);
                    _dataManager.SaveXml ();
                    designer.DesignerUi.ShowMessage ("Images Saved");
                } else designer.DesignerUi.ShowMessage (errorColor + "Saving Failed : No Image To Save");
            } else designer.DesignerUi.ShowMessage (errorColor = "Saving Failed :  Remember to save your craft first, saving images for craft 'New' is not allowed");
        }

        public void OnColorPickerButtonClicked () {

        }

        public bool OnClick (ClickEventArgs e) {
            RaycastHit hit;
            bool leftClick = false;
            bool rightClick = false;
            bool middleClick = false;
            if (UnityEngine.Input.GetMouseButtonDown (0)) leftClick = true;
            else if (UnityEngine.Input.GetMouseButtonDown (1)) rightClick = true;
            else if (UnityEngine.Input.GetMouseButtonDown (2)) middleClick = true;
            if (middleClick) Debug.Log("Midlle Clic");

            if (Physics.Raycast (e.Ray, out hit)) {
                if (hit.transform.parent?.name == "ViewCube(Clone)") {
                    String target = hit.transform.name;
                    target = target.Remove (target.Length - 9);
                    if (leftClick) {
                        SetCameraTo (target);
                    } else if (rightClick) {
                        referenceImages.Find (image => image.view == target)?.Toggle ();
                    } else if (middleClick) {
                        ToggleOrtho ();
                    }
                } else if (hit.transform.parent?.parent?.name == "ViewCube(Clone)") {
                    String target = hit.transform.parent.name.Remove (0, 2);
                    if (leftClick) {
                        SetCameraTo (target);
                    } else if (middleClick) {
                        ToggleOrtho ();
                    }
                }
            }
            if (rightClick) partTools?.OnRightClic ();
            return false;
        }

        public void ToggleOrtho () {
            orthoOn = !orthoOn;
            designerCamera.orthographic = orthoOn;
            gizmoCamera.orthographic = orthoOn;

            if (ModSettings.Instance.viewCube) {
                if (orthoOn) viewCube?.OnOrthoSizeChanged (designerCamera.orthographicSize);
                else viewCube?.OnOrthoOff ();
            }
        }

        public void SetCameraTo (String view) {
            DesignerCameraViewDirection designerCameraViewDirection = DesignerCameraViewDirection.None;
            DesignerCameraScript designerCameraScript = Game.Instance.Designer.DesignerCamera as DesignerCameraScript;

            if (view == ("Front")) designerCameraViewDirection = DesignerCameraViewDirection.Front;
            else if (view == ("Back")) designerCameraViewDirection = DesignerCameraViewDirection.Back;
            else if (view == ("Top")) designerCameraViewDirection = DesignerCameraViewDirection.Top;
            else if (view == ("Bottom")) designerCameraViewDirection = DesignerCameraViewDirection.Bottom;
            else if (view == ("Left")) designerCameraViewDirection = DesignerCameraViewDirection.Left;
            else if (view == ("Right")) designerCameraViewDirection = DesignerCameraViewDirection.Right;

            designerCameraScript.SetViewDirection (designerCameraViewDirection);
        }
    }

    class ColorPickerButtonScript : MonoBehaviour {
        XmlElement ButtonObject;
        IXmlLayout XmlLayout;
        public void OnLayoutRebuilt (IXmlLayout xmlLayout) {
            XmlLayout = xmlLayout;
            ButtonObject = xmlLayout.GetElementById<XmlElement> ("ColorPickerButton");
        }
        public void SetButtonEnabled (bool enabled) {
            ButtonObject.SetActive (enabled);
        }
        void OnColorPickerButtonClicked () {
            Mod.Instance.OnColorPickerButtonClicked ();
        }
    }
}