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
        private FlyoutScript Flyout = new FlyoutScript ();
        private DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
        private IXmlLayoutController _controller;
        private static ViewToolsUI _ViewToolsUI;
        private XmlLayout _XmlLayout;

        public void OnLayoutRebuilt (IXmlLayoutController xmlLayoutController) {
            _controller = xmlLayoutController;
            _XmlLayout = (XmlLayout) _controller.XmlLayout;

            Flyout.Initialize (_XmlLayout.GetElementById ("flyout-DesignerTools"));
            Flyout.Open ();
        }

        private void OnViewToolButtonClicked () {
            if (_ViewToolsUI != null) {
                _ViewToolsUI.Close ();
                _ViewToolsUI = null;
            } else {
                var ui = Game.Instance.UserInterface;
                _ViewToolsUI = ui.BuildUserInterfaceFromResource<ViewToolsUI> ("DesignerTools/Designer/ViewTools", (script, controller) => script.OnLayoutRebuilt (controller));
            }
        }

        private void OnSaveRefImagesButtonClicked () {
            Mod.Instance.OnSaveRefImages ();
        }

        public void Close () {
            _XmlLayout.Hide (() => Destroy (this.gameObject), true);
            if (_ViewToolsUI != null && !_ViewToolsUI.ViewToolPanelPinned) _ViewToolsUI.Close ();
        }

        public ViewToolsUI GetViewToolsUI () {
            if (_ViewToolsUI != null) return _ViewToolsUI;
            return null;
        }
    }
}