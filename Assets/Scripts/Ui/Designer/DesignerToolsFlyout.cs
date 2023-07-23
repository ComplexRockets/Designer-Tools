using Assets.Scripts.Design;
using Assets.Scripts.DesignerTools.ReferenceImages;
using Assets.Scripts.DesignerTools.ViewTools;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Ui.Designer
{
    public class DesignerToolsFlyout : MonoBehaviour
    {

        public FlyoutScript flyout = new FlyoutScript();
        private DesignerScript _designer => (DesignerScript)Game.Instance.Designer;
        private Mod _mod = Mod.Instance;
        private static ReferenceImagesPanel _refImgsPanel;
        public XmlLayout xmlLayout { get; private set; }
        public bool refImgsPanelOpen => _refImgsPanel != null;

        public void OnLayoutRebuilt(IXmlLayoutController xmlLayoutController)
        {
            xmlLayout = (XmlLayout)xmlLayoutController.XmlLayout;

            flyout.Initialize(xmlLayout.GetElementById("flyout-DesignerTools"));
            flyout.Open();

            xmlLayout.GetElementById<Toggle>("ortho-toggle").SetIsOnWithoutNotify(_mod.orthoOn);
            xmlLayout.GetElementById<Toggle>("xray-toggle").SetIsOnWithoutNotify(ViewToolsUtilities.viewMode == ViewModes.Xray);
            //xmlLayout.GetElementById<SpinnerScript>("spinner-orientation").Value = Mod.Instance.imageGizmoIsLocalOrientation ? "Local" : "World";
            //xmlLayout.GetElementById<SpinnerScript>("spinner-grid-size").SetNumericValue(Game.Instance.Settings.Game.Designer.GridSize.Value);
        }

        public void RefreshRefImgsPanel()
        {
            if (refImgsPanelOpen)
            {
                OnReferenceImagesButtonClicked();
                OnReferenceImagesButtonClicked();
            }
        }

        private void OnOrthoToggleButtonClicked() => _mod.ToggleOrtho();
        public void OnOrthoToggled(bool ortho) => xmlLayout.RebuildLayout(forceEvenIfXmlUnchanged: true);
        private void OnXrayToggleButtonClicked()
        {
            ViewToolsUtilities.ToggleXray();
            xmlLayout.RebuildLayout(forceEvenIfXmlUnchanged: true);
        }

        private void OnReferenceImagesButtonClicked()
        {
            if (refImgsPanelOpen)
            {
                _refImgsPanel.Close();
                _refImgsPanel = null;
            }
            else
            {
                _refImgsPanel = ReferenceImagesPanel.Create(Game.Instance.Designer.DesignerUi.Transform);
            }
        }

        private void OnManageRefImagesButtonClicked()
        {
            ReferenceImagesManager refImagesManager = new ReferenceImagesManager(Mod.Instance.dataManager.imageDB);
            Game.Instance.UserInterface.CreateListView(refImagesManager);
        }

        private void OnPasteImageDataButtonClicked()
        {
            _mod.dataManager.PasteImageData();
        }

        private void OnGridSizeChanged()
        {
            Game.Instance.Settings.Game.Designer.GridSize.UpdateAndCommit(xmlLayout.GetElementById<SpinnerScript>("spinner-grid-size").NumericValue);
        }

        private void OnOrientationChanged(string value)
        {
            _mod.imageGizmoIsLocalOrientation = value == "Local";
        }

        public void OnFlyoutCloseButtonClicked() => DesignerToolsUI.CloseFlyout();

        public void Close()
        {
            xmlLayout.Hide(() => Destroy(this.gameObject), true);
            if (refImgsPanelOpen)
            {
                _refImgsPanel.Close();
                _refImgsPanel = null;
            }
        }
    }
}
