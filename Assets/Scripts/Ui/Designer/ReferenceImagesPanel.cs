using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;
//using static Assets.Scripts.DesignerTools.ReferenceImage;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ReferenceImagesPanel : DialogScript
    {
        private string _path = Mod.Instance.refImagePath;
        private List<String> _images => (Directory.EnumerateFiles(_path, "*.png").Union(Directory.EnumerateFiles(_path, "*.jpg")).Union(Directory.EnumerateFiles(_path, "*.jpeg"))).ToList();
        private Mod _mod => Mod.Instance;
        public XmlLayout xmlLayout { get; private set; }
        private XmlElement _imageConfirmButton;
        public XmlElement imageTextTemplate { get; private set; }
        public XmlElement imageEditTemplate { get; private set; }
        public XmlElement noImageTemplate { get; private set; }
        public XmlElement imageSettingsTemplate { get; private set; }
        private Views selectedView;

        public static ReferenceImagesPanel Create(Transform parent)
        {
            ReferenceImagesPanel dialog = Game.Instance.UserInterface.CreateDialog<ReferenceImagesPanel>(parent, registerWithUserInterface: false);
            Game.Instance.UserInterface.BuildUserInterfaceFromResource("DesignerTools/Designer/ReferenceImagesPanel", dialog, delegate (IXmlLayoutController x)
            {
                dialog.OnLayoutRebuilt(x.XmlLayout);
            });
            return dialog;
        }

        public void OnLayoutRebuilt(IXmlLayout layout)
        {
            _mod.refImgsPanel = this;
            xmlLayout = (XmlLayout)layout;
            _imageConfirmButton = xmlLayout.GetElementById("ImageConfirmButton");
            imageTextTemplate = xmlLayout.GetElementById("image-text-template");
            imageEditTemplate = xmlLayout.GetElementById("image-edit-template");
            imageSettingsTemplate = xmlLayout.GetElementById("image-settings-template");
            noImageTemplate = xmlLayout.GetElementById("no-image-text-template");

            foreach (ReferenceImage image in _mod.referenceImages)
            {
                image.InitialiseUI(this);
                image.ApplyChanges();
            }
        }

        private void OnViewButtonClicked(string view)
        {
            _mod.SetCameraTo(view);
        }

        public void OnSelectImageButtonClicked(Views view)
        {
            selectedView = view;
            ImageSelector imageSelector = new ImageSelector(_images, view.ToString());
            imageSelector.ImageSelected = (string image) => OnImageConfirm(image);
            Game.Instance.UserInterface.CreateListView(imageSelector);
        }

        private void OnImageConfirm(string image)
        {
            Texture2D img = new Texture2D(0, 0);
            img.LoadImage(File.ReadAllBytes(image));
            img.name = image.Split('/').Last();
            ReferenceImage refImg = ReferenceImage.GetReferenceImage(selectedView);
            refImg.ResetSettings();
            refImg.UpdateImage(img);
        }

        public XmlElement AddItem(XmlElement template, XmlElement parent, XmlLayout xmlLayout, string id)
        {
            if (parent == null) parent = template.parentElement;

            XmlElement item = GameObject.Instantiate(template);
            XmlElement component = item.GetComponent<XmlElement>();
            item.name = id;

            component.Initialise(xmlLayout, (RectTransform)item.transform, template.tagHandler);
            parent.AddChildElement(component);

            component.SetAttribute("active", "true");
            component.SetAttribute("id", id);
            component.ApplyAttributes();

            return component;
        }

        public override void Close()
        {
            foreach (ReferenceImage image in _mod.referenceImages)
            {
                image.OnUIClosed();
            }
            xmlLayout.Hide(() => Destroy(this.gameObject), true);
        }
    }
}
