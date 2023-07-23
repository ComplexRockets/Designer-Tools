namespace Assets.Scripts
{
    using System;
    using Assets.Scripts.Design;
    using Assets.Scripts.Design.Tools;
    using Assets.Scripts.DesignerTools.ViewTools;
    using Assets.Scripts.DesignerTools.ReferenceImages;
    using Assets.Scripts.Ui.Designer;
    using ModApi.Design;
    using ModApi.Design.Events;
    using ModApi.Input.Events;
    using ModApi.Scenes.Events;
    using ModApi.Settings.Core.Events;
    using ModApi.Ui;
    using UnityEngine;
    using HarmonyLib;
    using System.Collections.Generic;
    using Assets.Scripts.Web;
    using Assets.Scripts.DesignerTools;
    using System.Reflection;

    public class Mod : ModApi.Mods.GameMod
    {
        private Mod() : base()
        {
        }
        public static Mod Instance { get; } = GetModInstance<Mod>();
        public delegate void EmptyEventHandler();
        public DataManager dataManager = new DataManager();
        public DesignerScript designer => (DesignerScript)Game.Instance.Designer;
        public ReferenceImagesPanel refImgsPanel;
        private ViewCube _viewCube;
        public ViewCube viewCube => _viewCube;
        //public IFlyout openedFlyout => DesignerToolsUI.flyoutOpened ? DesignerToolsUI.designerToolsFlyout.flyout : Game.Instance.Designer.DesignerUi.SelectedFlyout;
        public Camera viewCubeCamera;
        public Camera designerCamera => designer.DesignerCamera.Camera;
        public Camera gizmoCamera => designer.GizmoCamera;
        public Transform referenceImagesParent;
        public delegate void PartEventHandler(PartRaycastResult partRayResult);
        public string errorColor = "<color=#e7515a>";//"<color=#b33e46>";
        public string refImagePath;
        public bool designerInitialised = false;
        private bool _orthoOn = false;
        private bool _imageGizmoIsLocalOrientation = true;
        public bool imageGizmoIsLocalOrientation
        {
            get => _imageGizmoIsLocalOrientation;
            set
            {
                _imageGizmoIsLocalOrientation = imageGizmoIsLocalOrientation;
                foreach (ReferenceImage img in referenceImages) img.GizmoIsLocalOrientationChanged();
            }
        }
        public bool orthoOn
        {
            get => _orthoOn;
            private set
            {
                _orthoOn = designerCamera.orthographic = gizmoCamera.orthographic = viewCubeCamera.orthographic = value;
                DesignerToolsUI.designerToolsFlyout?.OnOrthoToggled(value);
            }
        }

        public ReferenceImage[] referenceImages;

        public static ReferenceImage[] emptyRefImages()
        {
            ReferenceImage[] refImages = new ReferenceImage[6];
            for (int i = 0; i < 6; i++) refImages[i] = new GameObject().AddComponent<ReferenceImage>().Initialise((Views)i);
            return refImages;
        }

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            refImagePath = Application.persistentDataPath + "/UserData/DesignerTools/ReferenceImages/";
            System.IO.Directory.CreateDirectory(refImagePath);
            referenceImages = emptyRefImages();

            Ui.Designer.DesignerToolsUI.Initialize();
            dataManager.initialise();

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;
            ModSettings.Instance.Changed += OnSettingsChanged;

            try
            {
                if (ModSettings.Instance.DevMode) Harmony.DEBUG = true;
                Harmony harmony = new Harmony("com.aram.designer-tools");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch
            {
                MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.Okay);
                messageDialogScript.MessageText = string.Format(errorColor + "Harmony seems to be missing. The dependency needs to be installed and enabled for Designer Tools to work");
                messageDialogScript.OkayClicked += delegate (MessageDialogScript d)
                {
                    d.Close();
                    WebUtility.OpenUrl(Game.SimpleRocketsWebsiteUrl + "/Mods/View/234638/Juno-Harmony");
                };
            }
        }

        public void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                GameObject CameraObj = new GameObject();
                CameraObj.transform.SetParent(designerCamera.gameObject.transform);
                CameraObj.transform.localPosition = CameraObj.transform.localEulerAngles = new Vector3();
                viewCubeCamera = CameraObj.AddComponent<Camera>();
                viewCubeCamera.clearFlags = CameraClearFlags.Depth;
                viewCubeCamera.depth = 100;
                viewCubeCamera.orthographicSize = 0.25f;

                referenceImagesParent = new GameObject().transform;

                LayerMask mask = 1 << 20;
                viewCubeCamera.cullingMask = mask;
                designerCamera.cullingMask &= ~mask;
                gizmoCamera.cullingMask &= ~mask;

                if (ModSettings.Instance.viewCube && _viewCube == null) _viewCube = new ViewCube(designer);

                designer.CraftLoaded += OnCraftLoaded;
                designer.Click += OnClick;
                ((MovePartTool)designer.MovePartTool).DragPartSelectionStarted += OnDragPartSelectionStarted;
                ((MovePartTool)designer.MovePartTool).DragPartSelectionEnded += OnDragPartSelectionEnded;
                designer.PartAdded += (object sender, DesignerPartAddedEventArgs e) => OnDragPartSelectionStarted();
                designerInitialised = true;
            }
        }

        public void OnSceneUnloading(object sender, SceneEventArgs e)
        {
            if (e.Scene == ModApi.Scenes.SceneNames.Designer)
            {
                designerInitialised = false;
                designer.CraftLoaded -= OnCraftLoaded;
                designer.Click -= OnClick;
                _viewCube?.Destroy();
                _viewCube = null;
                orthoOn = false;
            }
        }

        public bool OnClick(ClickEventArgs e)
        {
            bool leftClick = false;
            bool rightClick = false;
            bool middleClick = false;
            if (UnityEngine.Input.GetMouseButtonDown(0)) leftClick = true;
            else if (UnityEngine.Input.GetMouseButtonDown(1)) rightClick = true;
            else if (UnityEngine.Input.GetMouseButtonDown(2)) middleClick = true;

            Ray ray = viewCubeCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1 << 20))
            {
                if (hit.transform.parent?.name == "ViewCube(Clone)")
                {
                    string target = hit.transform.name;
                    target = target.Remove(target.Length - 9);

                    if (leftClick) SetCameraTo(target);
                    else if (rightClick)
                    {
                        ReferenceImage.GetReferenceImage((Views)Enum.Parse(typeof(Views), target)).Toggle();
                    }
                    else if (middleClick) ToggleOrtho();
                    return true;
                }
                else if (hit.transform.parent?.parent?.name == "ViewCube(Clone)")
                {
                    string target = hit.transform.parent.name.Remove(0, 2);

                    if (leftClick) SetCameraTo(target);
                    else if (middleClick) ToggleOrtho();
                    return true;
                }
            }
            return false;
        }

        private void OnDragPartSelectionStarted()
        {
            _viewCube?.Toggle(false);
            if (DesignerToolsUI.flyoutOpened) DesignerToolsUI.designerToolsFlyout.flyout.IsHidden = true;
        }
        private void OnDragPartSelectionEnded()
        {
            _viewCube?.Toggle(true);
            if (DesignerToolsUI.flyoutOpened) DesignerToolsUI.designerToolsFlyout.flyout.IsHidden = false;
        }

        public void OnCraftLoaded()
        {
            ViewToolsUtilities.RefreshViewMode();
            RefreshReferenceImages();
        }

        public void RefreshReferenceImages()
        {
            ReferenceImage[] images = dataManager.LoadImages(designer.CraftScript.Data.Name);
            for (int i = 0; i < 6; i++) referenceImages[i]?.Destroy();
            referenceImages = images;

            DesignerToolsUI.designerToolsFlyout?.RefreshRefImgsPanel();
        }

        public void OnReferenceImageChanged()
        {
            string name = designer.CraftScript.Data.Name;
            if (CraftValidForRefImg())
            {
                dataManager.SaveImages(name, referenceImages);
                dataManager.SaveXml();
            }
            else designer.DesignerUi.ShowMessage(errorColor + "Remember to save your craft, '" + name + "' can't have reference images");
        }

        public void OnSettingsChanged(object sender, SettingsChangedEventArgs<ModSettings> e)
        {
            if (Game.InDesignerScene)
            {
                _viewCube?.Destroy();
                _viewCube = null;

                if (ModSettings.Instance.viewCube)
                {
                    if (_viewCube == null) _viewCube = new ViewCube(designer);
                }
            }
        }

        private List<string> forbbidenCrafts = new List<string>() { "New", "New Airplane", "Tutorial", "Like a Bird Tutorial Craft", "OptimumTrajectory", "First Payload Tutorial", "First Race Tutorial Plane", "The Jump Tutorial Craft", "Vertical Shot Tutorial Craft" };
        public bool CraftValidForRefImg() => !forbbidenCrafts.Contains(designer.CraftScript.Data.Name);

        public void DeleteImageData(string craftId)
        {
            dataManager.DeleteImageData(craftId);
            RefreshReferenceImages();
        }

        public void DesignerUpdate()
        {
            if (orthoOn)
            {
                float zoom = designer.DesignerCamera.CurrentZoom / 2f;
                designerCamera.orthographicSize = zoom;
                gizmoCamera.orthographicSize = zoom;
            }
            if (ModSettings.Instance.viewCube) _viewCube?.Update();
            referenceImagesParent.transform.position = designer.CraftScript.RootPart.Transform.position;
        }

        public void ToggleOrtho() => orthoOn = !orthoOn;

        public void SetCameraTo(string viewString)
        {
            DesignerCameraViewDirection view;
            if (!Enum.TryParse(viewString, true, out view))
            {
                Debug.LogError("Failed to set camera view, unable to parse view string");
                return;
            }
            DesignerCameraScript designerCameraScript = Game.Instance.Designer.DesignerCamera as DesignerCameraScript;
            designerCameraScript.SetViewDirection(view);
        }
    }

    [HarmonyPatch(typeof(DesignerScript), "SaveCraft")]
    class DesignerScriptSaveCraftPatch
    {
        static void Postfix() { Debug.Log("Save post fix " + Game.Instance.Designer.CraftScript.Data.Name); Mod.Instance.OnReferenceImageChanged(); }
    }


    [HarmonyPatch(typeof(CameraTool), "HideFlyouts")]
    class CameraToolHideFlyoutsPatch
    {
        static void Postfix(bool hide)
        {
            //if (hide) return false;
            Mod.Instance.viewCube.flyoutHidden = hide;
            //if (DesignerToolsUI.flyoutOpened) DesignerToolsUI.designerToolsFlyout.flyout.IsHidden = false;
        }
    }

    [HarmonyPatch(typeof(CameraTool), "HandleClick")]
    class CameraToolHandleClickPatch
    {
        static bool Prefix(CameraTool __instance, ClickEventArgs e, ref bool __result)
        {
            if (!Mod.Instance.designer.DisableCameraMovement)
            {
                if (e.InputState == InputState.Begin)
                {
                    __result = true;
                    return false;
                }
                if (e.InputState == InputState.Updated)
                {
                    Traverse.Create(__instance).Method("HideFlyouts", new[] { typeof(bool) }).GetValue(true);
                    Mod.Instance.viewCube.flyoutHidden = true;
                }
                else if (e.InputState == InputState.End) Mod.Instance.viewCube.flyoutHidden = false;
            }
            return true;
        }
    }

}