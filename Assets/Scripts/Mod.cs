namespace Assets.Scripts {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.Tools;
    using Assets.Scripts.Ui.Designer;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod {
        private DataManager _DataManager = new DataManager ();
        private DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private DesignerToolsUI _DesignerToolsUI => Ui.Designer.DesignerToolsUIController._DesignerToolsUI;
        public ViewToolsUI ViewToolsUI;
        public List<ReferenceImage> ReferenceImages = new List<ReferenceImage> ();
        private Vector3 RootPosition => _Designer.CraftScript.RootPart.Transform.position;
        private Vector3 _Origin = new Vector3 ();
        public string RefImagePath;

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

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;
        }

        public void DesignerUpdate () {
            if (_Designer.CraftScript != null && RootPosition != _Origin) {
                _Origin = RootPosition;
                if (ViewToolsUI != null) ReferenceImages = ViewToolsUI.ReferenceImages;
                foreach (ReferenceImage image in ReferenceImages) {
                    image.UpdateOrigin (_Origin);
                }
            }
        }

        public void OnViewPanelClosed (List<ReferenceImage> referenceImages) {
            ReferenceImages = referenceImages;
            ViewToolsUI = null;
        }

        public void OnSceneLoaded (object sender, SceneEventArgs e) {
            if (e.Scene == "Design") {
                Debug.Log (e.Scene + " Loaded (mod.cs)");
                _Designer.CraftLoaded += OnCraftLoaded;
                DesignerToolsUIController.OnDesignerLoaded ();
            }
        }

        public void OnSceneUnloading (object sender, SceneEventArgs e) {
            if (e.Scene == "Design") {
                Debug.Log (e.Scene + " Unloading (mod.cs)");
                _Designer.CraftLoaded -= OnCraftLoaded;
            }
        }

        public void OnCraftLoaded () {
            List<ReferenceImage> Images = _DataManager.LoadImages (_Designer.CraftScript.Data.Name);

            if (ViewToolsUI != null) ReferenceImages = ViewToolsUI.ReferenceImages;
            if (ReferenceImages != null) {
                lock (ReferenceImages) {
                    foreach (ReferenceImage image in ReferenceImages) {
                        image.Destroy ();
                    }
                }
            }

            if (Images != null) ReferenceImages = Images;
            else ReferenceImages = new List<ReferenceImage> ();

            if (ViewToolsUI != null) ViewToolsUI.UpdateReferenceImages (ReferenceImages);
        }

        public void OnSaveRefImages () {
            if (ViewToolsUI != null) ReferenceImages = ViewToolsUI.ReferenceImages;
            _DataManager.SaveImages (_Designer.CraftScript.Data.Name, ReferenceImages);
            _DataManager.SaveXml ();
        }
    }
}