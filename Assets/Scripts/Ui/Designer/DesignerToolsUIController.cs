namespace Assets.Scripts.Ui.Designer {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Design;
    using DesignerTools;
    using ModApi.Common;
    using ModApi.Design;
    using ModApi.Ui.Inspector;
    using ModApi.Ui;
    using TMPro;
    using UI.Xml;
    using UnityEngine.UI;
    using UnityEngine;

    public static class DesignerToolsUIController {
        private const string _buttonId = "DesignerTools-button";
        public static DesignerToolsUI designerToolsUI;
        private static DesignerScript _designer => (DesignerScript) Game.Instance.Designer;
        private static XmlElement _flyoutButton;
        private static IFlyout _openedFlyout;
        public static IFlyout openedFlyout => _openedFlyout;

        public static void Initialize () {
            var userInterface = Game.Instance.UserInterface;
            userInterface.AddBuildUserInterfaceXmlAction (UserInterfaceIds.Design.DesignerUi, OnBuildDesignerUI);

            Game.Instance.SceneManager.SceneTransitionStarted += (s, e) => designerToolsUI = null;
        }

        private static void OnBuildDesignerUI (BuildUserInterfaceXmlRequest request) {
            var ns = XmlLayoutConstants.XmlNamespace;
            var viewButton = request.XmlDocument
                .Descendants (ns + "Panel")
                .First (x => (string) x.Attribute ("internalId") == "flyout-view");

            viewButton.Parent.Add (
                new XElement (
                    ns + "Panel",
                    new XAttribute ("id", _buttonId),
                    new XAttribute ("class", "toggle-button audio-btn-click"),
                    new XAttribute ("name", "ButtonPanel.DesignerToolsUIController"),
                    new XAttribute ("tooltip", "Designer Tools"),
                    new XElement (
                        ns + "Image",
                        new XAttribute ("class", "toggle-button-icon"),
                        new XAttribute ("sprite", "DesignerTools/Sprites/DesignerToolsIcon"))));

            request.AddOnLayoutRebuiltAction (xmlLayoutController => {
                var button = xmlLayoutController.XmlLayout.GetElementById (_buttonId);
                _flyoutButton = (XmlElement) button;
                button.AddOnClickEvent (OnButtonClicked);
            });
        }

        public static void OnDesignerLoaded () {
            foreach (IFlyout flyout in Game.Instance.Designer.DesignerUi.Flyouts.All) {
                flyout.Opened += OnOtherFlyoutOpened;
                flyout.Closed += OnOtherFlyoutClosed;
            }
            _openedFlyout = null;
        }

        public static void OnOtherFlyoutOpened (IFlyout flyout) {
            //Debug.Log ("Flyout Opened");
            _openedFlyout = flyout;
            if (designerToolsUI != null) {
                //Debug.Log ("Closing DT Flyout");
                designerToolsUI.Close ();
                designerToolsUI = null;
            }
        }

        public static void OnOtherFlyoutClosed (IFlyout flyout) {
            //Debug.Log ("Flyout Closed");
            _openedFlyout = null;
        }

        private static void OnButtonClicked () {
            if (designerToolsUI != null) {
                designerToolsUI.Close ();
                designerToolsUI = null;
                _flyoutButton.SetAndApplyAttribute ("colors", "Button|ButtonHover|ButtonPressed|ButtonDisabled");
                _openedFlyout = null;
            } else {
                var ui = Game.Instance.UserInterface;
                designerToolsUI = ui.BuildUserInterfaceFromResource<DesignerToolsUI> ("DesignerTools/Designer/DesignerTools", (script, controller) => script.OnLayoutRebuilt (controller));
                _designer.DesignerUi.CloseFlyout (_designer.DesignerUi.SelectedFlyout);
                _flyoutButton.SetAndApplyAttribute ("colors", "ButtonPressed|ButtonHover|ButtonPressed|ButtonDisabled");
                _openedFlyout = designerToolsUI.flyout;
            }
        }
    }
}