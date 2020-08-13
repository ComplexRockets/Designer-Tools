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
    using ModApi.Ui;
    using ModApi;
    using UI.Xml;
    using UnityEngine.UI;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod {
        public DesignerScript Designer => (DesignerScript) Game.Instance.Designer;
        public PartToolsManager PartTools = new PartToolsManager ();
        public PartSelectorManager SelectorManager = new PartSelectorManager ();
        public ViewToolsUI ViewToolsUI;
        public List<ReferenceImage> ReferenceImages {
            get {
                if (ViewToolsUI != null) _ReferenceImages = ViewToolsUI.ReferenceImages;
                return _ReferenceImages;
            }
        }
        public List<ReferenceImage> _ReferenceImages = new List<ReferenceImage> ();
        private Vector3 RootPosition => Designer.CraftScript.RootPart.Transform.position;
        private Vector3 _Origin = new Vector3 ();
        private ViewCube _ViewCube;
        private ColorPickerButtonScript _ColorPickerButton;
        private DataManager _DataManager = new DataManager ();
        private DesignerToolsUI _DesignerToolsUI => Ui.Designer.DesignerToolsUIController._DesignerToolsUI;
        public String RefImagePath;
        public int CraftXMLVersion;
        public bool CraftLoaded = false;

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
            RefImagePath = Application.persistentDataPath + "/UserData/DesignerTools/ReferenceImages/";
            System.IO.Directory.CreateDirectory (RefImagePath);

            base.OnModInitialized ();
            Ui.Designer.DesignerToolsUIController.Initialize ();
            _DataManager.initialise ();
            PartTools.Initialize (SelectorManager);

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;
            Debug.Log ("Mod Initialized");
            //Debug.Log ("PartTTool: " + PartTools.ToString ());
        }

        public void DesignerUpdate () {
            _ViewCube.Update ();
        }

        public void OnViewPanelClosed (List<ReferenceImage> referenceImages) {
            _ReferenceImages = referenceImages;
            ViewToolsUI = null;
        }

        public void OnSceneLoaded (object sender, SceneEventArgs e) {
            Debug.Log (e.Scene + " Loaded (mod.cs)");
            if (e.Scene == ModApi.Scenes.SceneNames.Designer) {
                Designer.CraftLoaded += OnCraftLoaded;
                Designer.BeforeCraftUnloaded += OnCraftUnloading;
                Designer.CraftStructureChanged += OnCraftStructureChanged;
                Designer.Click += OnClick;
                DesignerToolsUIController.OnDesignerLoaded ();
                SelectorManager.OnDesignerLoaded ();
                _ViewCube = new ViewCube (Designer);

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

        public void OnSceneUnloading (object sender, SceneEventArgs e) {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer) {
                //Debug.Log (e.Scene + " Unloading (mod.cs)");
                Designer.CraftLoaded -= OnCraftLoaded;
                _ViewCube = null;
            }
        }

        public void OnCraftLoaded () {
            List<ReferenceImage> Images = _DataManager.LoadImages (Designer.CraftScript.Data.Name);

            if (ReferenceImages != null) {
                ReferenceImages.ForEach (image => image.Destroy ());
            }

            _ReferenceImages = Images != null? Images : new List<ReferenceImage> ();
            _Origin = RootPosition;

            // foreach (ReferenceImage image in ReferenceImages) {
            //     image.UpdateOrigin (_Origin);
            // }

            if (ViewToolsUI != null) ViewToolsUI.UpdateReferenceImages (_ReferenceImages);
            CraftXMLVersion = Designer.CraftScript.Data.XmlVersion;
            CraftLoaded = true;
        }

        public void OnCraftUnloading () {
            CraftLoaded = false;
        }

        private void OnCraftStructureChanged () {
            if (Designer.CraftScript != null && RootPosition != _Origin) {

                _Origin = RootPosition;
                foreach (ReferenceImage image in ReferenceImages) {
                    image.UpdateOrigin (_Origin);
                }
            }
        }

        public void OnSaveRefImages () {
            if (Designer.CraftScript.Data.Name != "New") {
                if (ReferenceImages.Count > 0) {
                    _DataManager.SaveImages (Designer.CraftScript.Data.Name, ReferenceImages);
                    _DataManager.SaveXml ();
                    Designer.DesignerUi.ShowMessage ("Images Saved");
                } else Designer.DesignerUi.ShowMessage ("<color=#b33e46> Saving Failed : No Image To Save");
            } else Designer.DesignerUi.ShowMessage ("<color=#b33e46> Saving Failed :  Remember to save your craft first, saving images for craft 'New' is not allowed");
        }

        public void OnColorPickerButtonClicked () {

        }

        public bool OnClick (ClickEventArgs e) {
            RaycastHit hit;
            bool rightClick = false;
            bool leftClick = false;
            if (UnityEngine.Input.GetMouseButtonDown (0)) leftClick = true;
            else if (UnityEngine.Input.GetMouseButtonDown (1)) rightClick = true;

            if (Physics.Raycast (e.Ray, out hit)) {
                //Debug.Log ("Hit: " + target + " parent: " + hit.transform.parent?.name);

                if (hit.transform.parent?.name == "ViewCube(Clone)") {
                    String target = hit.transform.name;
                    if (leftClick) {
                        //Debug.Log ("ViewCube clicked: " + target);
                        SetCameraTo (target.Remove (target.Length - 9));
                    } else if (rightClick) {
                        ReferenceImages.Find (image => image.View == target.Remove (target.Length - 9))?.Toggle ();
                    }
                } else if (hit.transform.parent?.parent?.name == "ViewCube(Clone)") {
                    String target = hit.transform.parent.name.Remove (0, 2);
                    if (leftClick) {
                        //Debug.Log ("ViewCube clicked: " + target);
                        SetCameraTo (target);
                    }
                }
            }
            if (rightClick) PartTools?.OnRightClic ();
            return false;
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