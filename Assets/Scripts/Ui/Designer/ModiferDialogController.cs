using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Assets.Scripts.Input;
using ModApi.Common;
using ModApi.Craft.Parts;
using ModApi.Input;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.DesignerTools {
    public class ModiferDialogController : DialogScript {

        private Mod _mod => Mod.Instance;
        private XmlLayout _xmlLayout;
        private XmlElement _modiferTemplate;
        private XmlElement _modiferTextTemplate;
        private XmlElement _propetyRowTemplate;
        private XmlElement _addModifierButton;
        private XmlElement _configModifer;
        private XmlElement _modifierList;
        private XmlElement _propetiesList;
        private XmlElement _partModifer;
        private List<XElement> _modifiers;
        private List<String> _modifiersString;
        private String _openedModifer;

        public static ModiferDialogController Create (PartData part) {
            ModiferDialogController dialog = Game.Instance.UserInterface.CreateDialog<ModiferDialogController> (Game.Instance.Designer.DesignerUi.Transform, registerWithUserInterface : false);
            Game.Instance.UserInterface.BuildUserInterfaceFromResource ("DesignerTools/Designer/ModiferDialog", dialog, delegate (IXmlLayoutController x) {
                dialog.OnLayoutRebuilt ((XmlLayout) x.XmlLayout, part);
            });

            return dialog;
        }

        public static ModiferDialogController Create (XElement part) {
            ModiferDialogController dialog = Game.Instance.UserInterface.CreateDialog<ModiferDialogController> (Game.Instance.Designer.DesignerUi.Transform, registerWithUserInterface : false);
            Game.Instance.UserInterface.BuildUserInterfaceFromResource ("DesignerTools/Designer/ModiferDialog", dialog, delegate (IXmlLayoutController x) {
                dialog.OnLayoutRebuilt ((XmlLayout) x.XmlLayout, part);
            });

            return dialog;
        }

        private void OnLayoutRebuilt (XmlLayout xmlLayout, PartData part) {
            _modifiers = _mod.partTools.GetModifiers (part);
            Initialise (xmlLayout);
        }

        private void OnLayoutRebuilt (XmlLayout xmlLayout, XElement part) {
            _modifiers = part.DescendantsAndSelf ().ToList ();
            Initialise (xmlLayout);
        }

        private void Initialise (XmlLayout xmlLayout) {
            _xmlLayout = xmlLayout;
            _modiferTemplate = _xmlLayout.GetElementById ("ModiferTemplate");
            _modiferTextTemplate = _xmlLayout.GetElementById ("ModiferTextTemplate");
            _propetyRowTemplate = _xmlLayout.GetElementById ("PropetyRowTemplate");
            _addModifierButton = _xmlLayout.GetElementById ("AddModifierButton");
            _configModifer = _xmlLayout.GetElementById ("ConfigModifer");
            _partModifer = _xmlLayout.GetElementById ("PartModifer");
            _modifierList = _xmlLayout.GetElementById ("ModifierList");
            _propetiesList = _xmlLayout.GetElementById ("PropetiesList");

            _modifiersString = GetModiferNames ();
            InitialiseList (_modifiersString);
            OnModiferSelected ("Part");
        }

        public void InitialiseList (List<String> modifiers) {
            foreach (String modifer in modifiers) {
                XmlElement ListItem = GameObject.Instantiate (_modiferTemplate);
                XmlElement component = ListItem.GetComponent<XmlElement> ();
                XmlElement button = component.GetElementByInternalId ("button");

                component.Initialise (_xmlLayout, (RectTransform) ListItem.transform, _modiferTemplate.tagHandler);
                _modifierList.AddChildElement (component);
                button.GetElementByInternalId<TextMeshProUGUI> ("text").text = modifer;
                button.SetAndApplyAttribute ("onClick", "OnModiferSelected(" + modifer + ");");
                component.GetElementByInternalId ("close").SetAndApplyAttribute ("onClick", "OnDeleteModifer(" + modifer + ");");
                component.SetAttribute ("active", "true");
                component.SetAttribute ("id", modifer);
                component.ApplyAttributes ();

                _configModifer.transform.SetAsFirstSibling ();
                _partModifer.transform.SetAsFirstSibling ();
                _addModifierButton.transform.SetAsLastSibling ();
            }
        }

        private List<String> GetModiferNames () {
            List<String> modifers = new List<string> ();
            foreach (XElement modifer in _modifiers) {
                String name = modifer.Name.LocalName;
                if (name != "Config" && name != "AttachPoints" && name != "Part") {
                    if (modifers.Select (m => m.Split ('-').First ()).Contains (name)) {
                        int count = modifers.Where (m => m.Split ('-').First () == name).Count ();
                        if (count == 1) {
                            modifers.Remove (name);
                            modifers.Add (name + "-1");
                        }
                        name += "-" + (count + 1);
                    }
                    modifers.Add (name);
                }
            }
            return modifers;
        }

        private void OnModiferSelected (String modifer) {
            XElement modiferElement;
            _openedModifer = modifer;

            if (modifer == "Part" || modifer == "Config") modiferElement = _modifiers.Find (m => m.Name == modifer);
            else {
                String[] Splited = modifer.Split ('-');
                String modifierName = modifer.Split ('-').First ();
                int modiferId = Splited.Length > 1 ? int.Parse (Splited.Last ()) : 0;
                modiferElement = _modifiers.Where (m => m.Name == modifierName).ElementAt (modiferId);
                _openedModifer = modifierName;
            }

            XmlElement[] properties = _propetiesList.childElements.ToArray ();
            properties.ToList ().ForEach (e => _propetiesList.RemoveChildElement (e, true));
            foreach (XAttribute property in modiferElement.Attributes ()) {
                XmlElement Row = GameObject.Instantiate (_propetyRowTemplate);
                XmlElement row = Row.GetComponent<XmlElement> ();
                row.Initialise (_xmlLayout, (RectTransform) Row.transform, _modiferTemplate.tagHandler);
                row.GetElementByInternalId<TextMeshProUGUI> ("label").text = property.Name.LocalName;
                row.GetElementByInternalId<TextMeshProUGUI> ("value").text = property.Value;
                row.SetAndApplyAttribute ("active", "true");
                _propetiesList.AddChildElement (row);
            }

        }

        private void OnDeleteModifer (String modifer) {
            String modifierName = modifer.Split ('-').First ();
            String[] Splited = modifer.Split ('-');
            int modiferId = Splited.Length > 1 ? int.Parse (Splited.Last ()) : 0;

            _modifiers.Remove (_modifiers.Where (m => m.Name == modifierName).ElementAt (modiferId));
            _modifierList.RemoveChildElement (_xmlLayout.GetElementById (modifer), true);
            if (modifer == _openedModifer) OnModiferSelected ("Part");
        }

        public override void Close () {
            base.Close ();
            base.gameObject.SetActive (value: false);
            UnityEngine.Object.Destroy (base.gameObject);
        }

    }
}