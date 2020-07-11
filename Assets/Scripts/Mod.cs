namespace Assets.Scripts {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
    using Assets.Scripts.Design;
    using Assets.Scripts.DesignerTools;
    using Assets.Scripts.Tools;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Design;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod {
        private DataManager _DataManager = new DataManager ();
        private DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private ViewToolsUI _ViewToolsUI => Ui.Designer.DesignerToolsUIController._DesignerToolsUI?.GetViewToolsUI ();
        public List<ReferenceImage> ReferenceImages = new List<ReferenceImage> ();
        private Vector3 _Origin = new Vector3 ();
        public string RefImagePath = Application.persistentDataPath + "/UserData/DesignerTools/ReferenceImages/";

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
            System.IO.Directory.CreateDirectory (RefImagePath);
            base.OnModInitialized ();
            Ui.Designer.DesignerToolsUIController.Initialize ();
            _DataManager.initialise ();

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;
        }

        public void DesignerUpdate () {
            if (_Designer.CraftScript.RootPart.Transform.position != _Origin) {
                _Origin = _Designer.CraftScript.RootPart.Transform.position;
                if (_ViewToolsUI != null) ReferenceImages = _ViewToolsUI.ReferenceImages;
                foreach (ReferenceImage image in ReferenceImages) {
                    image.UpdateOrigin (_Origin);
                }
            }
        }

        public void OnViewPanelClosed (List<ReferenceImage> referenceImages) {
            ReferenceImages = referenceImages;
        }

        public void OnSceneLoaded (object sender, SceneEventArgs e) {
            if (e.Scene == "Design") {
                Debug.Log (e.Scene + " Loaded (mod.cs)");
                _Designer.CraftLoaded += OnCraftLoaded;
                //_Designer.BeforeCraftUnloaded += OnCraftUnloading;
            }
        }

        public void OnSceneUnloading (object sender, SceneEventArgs e) {
            if (e.Scene == "Design") {
                Debug.Log (e.Scene + " Unloading (mod.cs)");
                _Designer.CraftLoaded -= OnCraftLoaded;
                _DataManager.SaveXml ();
            }
        }

        public void OnCraftLoaded () {
            List<ReferenceImage> Images = _DataManager.LoadImages (_Designer.CraftScript.Data.Name);

            if (_ViewToolsUI != null) ReferenceImages = _ViewToolsUI.ReferenceImages;
            if (ReferenceImages != null) {
                lock (ReferenceImages) {
                    foreach (ReferenceImage image in ReferenceImages) {
                        image.Destroy ();
                    }
                }
            }

            if (Images != null) ReferenceImages = Images;
            else ReferenceImages = new List<ReferenceImage> ();

            if (_ViewToolsUI != null) _ViewToolsUI.UpdateReferenceImages (ReferenceImages);
        }

        // public void OnCraftUnloading () {
        //     if (_ViewToolsUI != null) ReferenceImages = _ViewToolsUI.ReferenceImages;
        //     foreach (ReferenceImage image in ReferenceImages) {
        //         image.Destroy();
        //         ReferenceImages.Remove(image);
        //     }
        // }

        public void OnSaveRefImages () {
            if (_ViewToolsUI != null) ReferenceImages = _ViewToolsUI.ReferenceImages;
            _DataManager.SaveImages (_Designer.CraftScript.Data.Name, ReferenceImages);
            _DataManager.SaveXml ();
        }
    }
}