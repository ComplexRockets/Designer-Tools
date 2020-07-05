namespace Assets.Scripts.Ui.Designer {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System;
    using DesignerTools;
    using ModApi.Common;
    using ModApi.Ui;
    using TMPro;
    using UI.Xml;
    using UnityEngine.UI;
    using UnityEngine;

    public static class DesignerToolsUIController {
        private const string _buttonId = "DesignerTools-button";

        private static DesignerToolsUI _DesignerToolsUI;
        public static DesignerToolsUI DesignerToolsUI => _DesignerToolsUI;

        public static void Initialize () {
            var userInterface = Game.Instance.UserInterface;
            userInterface.AddBuildUserInterfaceXmlAction (
                UserInterfaceIds.Design.DesignerUi,
                OnBuildDesignerUI);

            Game.Instance.SceneManager.SceneTransitionStarted += (s, e) => _DesignerToolsUI = null;
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
                button.AddOnClickEvent (OnButtonClicked);
            });

            foreach (IFlyout flyout in Game.Instance.Designer.DesignerUi.Flyouts.All) {
                flyout.Opened += OnOtherFlyoutOpened;
            }
        }

        private static void OnButtonClicked () {
            if (_DesignerToolsUI != null) {
                _DesignerToolsUI.Close ();
                _DesignerToolsUI = null;
            } else {
                var ui = Game.Instance.UserInterface;
                _DesignerToolsUI = ui.BuildUserInterfaceFromResource<DesignerToolsUI> ("DesignerTools/Designer/DesignerTools", (script, controller) => script.OnLayoutRebuilt (controller));

                Debug.Log ("Flyouts: ");
                Debug.Log ("Asset.Scripts count: " + Assets.Scripts.Game.Instance.Designer.DesignerUi.Flyouts.All.Count);
                Debug.Log ("ModApi.Common count: " + ModApi.Common.Game.Instance.Designer.DesignerUi.Flyouts.All.Count);

                foreach (IFlyout flyout in Game.Instance.Designer.DesignerUi.Flyouts.All) {
                    Debug.Log (flyout.Title);
                    if (flyout.IsOpen) {
                        Debug.Log ("Closing Flyout: " + flyout.Title);
                        flyout.Close (true);
                    }
                }
            }
        }

        public static void OnOtherFlyoutOpened (IFlyout flyout) {
            Debug.Log ("Other flyout openned");
            _DesignerToolsUI.Close ();
            _DesignerToolsUI = null;
        }
    }
}