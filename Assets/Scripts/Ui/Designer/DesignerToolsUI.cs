using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Assets.Scripts.Design;
using Assets.Scripts.DesignerTools;
using Assets.Scripts.Input;
using Assets.Scripts.Ui;
using ModApi;
using ModApi.Common;
using ModApi.Design;
using ModApi.Input;
using ModApi.Math;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Ui.Designer {
    public class DesignerToolsUI : MonoBehaviour {
        public FlyoutScript flyout = new FlyoutScript ();
        private DesignerScript _designer => (DesignerScript) Game.Instance.Designer;
        private Mod _mod = Mod.Instance;
        private IXmlLayoutController _controller;
        private static ViewToolsUI _viewToolsUI;
        private XmlLayout _xmlLayout;
        private XmlElement _resizeSlider;
        private XmlElement _resizeSliderValue;
        private float resizeSliderValue = 0;

        public void OnLayoutRebuilt (IXmlLayoutController xmlLayoutController) {
            _controller = xmlLayoutController;
            _xmlLayout = (XmlLayout) _controller.XmlLayout;
            _resizeSliderValue = _xmlLayout.GetElementById ("Resizeslider-value");

            flyout.Initialize (_xmlLayout.GetElementById ("flyout-DesignerTools"));
            flyout.Open ();
        }

        private void OnViewToolButtonClicked () {
            if (_viewToolsUI != null) {
                _viewToolsUI.Close ();
                _viewToolsUI = null;
            } else {
                var ui = Game.Instance.UserInterface;
                _viewToolsUI = ui.BuildUserInterfaceFromResource<ViewToolsUI> ("DesignerTools/Designer/ViewTools", (script, controller) => script.OnLayoutRebuilt (controller));
            }
        }

        private void OnSaveRefImagesButtonClicked () {
            _mod.OnSaveRefImages ();
        }

        private void OnAlignPositionButtonClicked (char axis) {
            if (!ModSettings.Instance.DevMode) { DevModeOffError (); return; }
            _mod.partTools.OnAlignPosition (axis);
        }

        private void OnAlignRotationButtonClicked (char axis) {
            if (!ModSettings.Instance.DevMode) { DevModeOffError (); return; }
            _mod.partTools.OnAlignRotation (axis);
        }

        private void ResizeSliderValueChanged (float value) {
            if (!ModSettings.Instance.DevMode) return;
            _mod.partTools.OnResizeParts (resizeSliderValue);
            _resizeSliderValue.SetAndApplyAttribute ("text", value + "%");
            resizeSliderValue = value;
        }

        private void ResizeSliderMouseExit () {
            //if (ResizeSliderValue != 0)     
            //ResizeSliderValue = 0;
        }

        private void OnModifierPanelButtonClicked () {
            if (_mod.selectorManager.selectedParts.Count > 0) {
                ModiferDialogController.Create (_designer.SelectedPart.Data);
            } else _designer.DesignerUi.ShowMessage (_mod.errorColor + "No Part Selected");
        }

        public void OnFlyoutCloseButtonClicked () {
            Close ();
        }

        public void DevModeOffError () {
            _designer.DesignerUi.ShowMessage (_mod.errorColor + "This feature is not yet fully working and is hidden to avoid bugs, it can be activated through the settings");
        }

        public void Close () {
            _xmlLayout.Hide (() => Destroy (this.gameObject), true);
            if (_viewToolsUI != null && !_viewToolsUI.viewToolPanelPinned) {
                _viewToolsUI.Close ();
                _viewToolsUI = null;
            }
        }

        public ViewToolsUI GetViewToolsUI () {
            if (_viewToolsUI != null) return _viewToolsUI;
            return null;
        }
    }
}