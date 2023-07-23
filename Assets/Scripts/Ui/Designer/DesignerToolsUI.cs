using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Design;
using ModApi.Ui;
using UI.Xml;

namespace Assets.Scripts.Ui.Designer
{
    public class DesignerToolsUI
    {
        private const string _buttonId = "DesignerTools-button";
        private static DesignerScript _designer => (DesignerScript)Game.Instance.Designer;
        private static XmlElement _flyoutButton;
        public static bool flyoutOpened => designerToolsFlyout != null;
        public static DesignerToolsFlyout designerToolsFlyout;

        public static void Initialize()
        {
            var userInterface = Game.Instance.UserInterface;
            userInterface.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Design.DesignerUi, OnBuildDesignerUI);
        }

        private static void OnBuildDesignerUI(BuildUserInterfaceXmlRequest request)
        {
            var ns = XmlLayoutConstants.XmlNamespace;
            var viewButton = request.XmlDocument
                .Descendants(ns + "Panel")
                .First(x => (string)x.Attribute("internalId") == "flyout-view");

            viewButton.Parent.Add(
                new XElement(
                    ns + "Panel",
                    new XAttribute("id", _buttonId),
                    new XAttribute("class", "toggle-button audio-btn-click"),
                    new XAttribute("name", "ButtonPanel.DesignerToolsUIController"),
                    new XAttribute("tooltip", "Designer Tools"),
                    new XElement(
                        ns + "Image",
                        new XAttribute("class", "toggle-button-icon"),
                        new XAttribute("sprite", "DesignerTools/Sprites/DesignerToolsIcon"))));

            request.AddOnLayoutRebuiltAction(xmlLayoutController =>
            {
                var button = xmlLayoutController.XmlLayout.GetElementById(_buttonId);
                _flyoutButton = (XmlElement)button;
                button.AddOnClickEvent(ToggleFlyout);
            });

            _designer.DesignerUi.SelectedFlyoutChanged += SelectedFlyoutChanged;
        }

        private static void SelectedFlyoutChanged(IFlyout flyout)
        {
            if (flyout != null && flyoutOpened && flyout.Title != designerToolsFlyout.flyout.Title) CloseFlyout();
        }

        public static void ToggleFlyout()
        {
            if (flyoutOpened)
            {
                _designer.DesignerUi.SelectedFlyout = null;
                CloseFlyout();
            }
            else OpenFlyout();
        }

        public static void OpenFlyout()
        {
            var ui = Game.Instance.UserInterface;
            designerToolsFlyout = ui.BuildUserInterfaceFromResource<DesignerToolsFlyout>("DesignerTools/Designer/DesignerTools", (script, controller) => script.OnLayoutRebuilt(controller));
            _flyoutButton.AddClass("toggle-button-toggled");
            _designer.DesignerUi.SelectedFlyout = designerToolsFlyout.flyout;
        }

        public static void CloseFlyout()
        {
            designerToolsFlyout.Close();
            designerToolsFlyout = null;
            _flyoutButton.RemoveClass("toggle-button-toggled");
        }
    }
}
