using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Assets.Scripts.DesignerTools.ReferenceImages;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.DesignerTools
{
    public class DataManager
    {
        private Mod Mod => Mod.Instance;
        private XmlSerializer _xmlSerializer;
        private FileStream _fileStream;
        public CraftImagesDataBase imageDB;
        public CraftImagesData copiedImageData;

        public void Initialise()
        {
            string path = Mod.refImagePath + "ReferenceImagesData.xml";
            _xmlSerializer = new XmlSerializer(typeof(CraftImagesDataBase));

            if (!File.Exists(path)) SaveXml();

            using (StringReader reader = new(File.ReadAllText(path)))
            {
                imageDB = _xmlSerializer.Deserialize(reader) as CraftImagesDataBase;
            }
            imageDB ??= new CraftImagesDataBase();
        }

        public void SaveImages(string craftName, ReferenceImage[] images)
        {
            List<ReferenceImageData> _ImageList = new();
            foreach (ReferenceImage image in images)
            {
                if (image.HasImage || image.missingImage)
                {
                    _ImageList.Add(new ReferenceImageData()
                    {

                        View = image.view,
                        Rotation = image.Rotation,
                        OffsetX = image.OffsetX,
                        OffsetY = image.OffsetY,
                        OffsetZ = image.OffsetZ,
                        Scale = image.Scale,
                        Opacity = image.Opacity,
                        Active = image.active,
                        ImageName = image.imageName
                    });
                }
            }

            if (_ImageList.Count() > 0)
            {
                DeleteImageData(craftName);
                imageDB.CraftImagesList.Add(new CraftImagesData()
                {
                    CraftName = craftName,
                    ImageList = _ImageList
                });
            }
        }

        public void PasteImageData()
        {
            if (copiedImageData == null)
            {
                Mod.Designer.DesignerUi.ShowMessage("No reference image data was copied!");
                return;
            }

            string craftName = Mod.Designer.CraftScript.Data.Name;

            if (!Mod.CraftValidForRefImg())
            {
                Mod.Designer.DesignerUi.ShowMessage(Mod.errorColor + "'" + craftName + "' can't have reference images");
                return;
            }

            if (GetImages(craftName) != null)
            {
                MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
                messageDialogScript.OkayButtonText = "OVERWRITE";
                messageDialogScript.MessageText = string.Format("'" + craftName + "' already has reference images");
                messageDialogScript.UseDangerButtonStyle = true;
                messageDialogScript.OkayClicked += delegate (MessageDialogScript d)
                {
                    d.Close();
                    Paste();
                };
            }
            else Paste();
        }

        private void Paste()
        {
            string craftName = Mod.Designer.CraftScript.Data.Name;
            DeleteImageData(craftName);
            imageDB.CraftImagesList.Add(new CraftImagesData()
            {
                CraftName = craftName,
                ImageList = copiedImageData.ImageList
            });
            Mod.Designer.DesignerUi.ShowMessage("Pasted images from '" + copiedImageData.CraftName + "'");
            Mod.RefreshReferenceImages();
        }

        public CraftImagesData GetImages(string craftID)
        {
            foreach (CraftImagesData images in imageDB.CraftImagesList)
            {
                if (images.CraftName == craftID)
                {
                    return images;
                }
            }
            return null;
        }

        public ReferenceImage[] LoadImages(string craftID)
        {
            CraftImagesData images = GetImages(craftID);
            ReferenceImage[] referenceImages = Mod.EmptyRefImages();
            if (images != null)
            {
                foreach (ReferenceImageData img in images.ImageList)
                {
                    ReferenceImage image;
                    if (File.Exists(Mod.refImagePath + img.ImageName))
                    {
                        Texture2D RefImage = new(0, 0);
                        RefImage.LoadImage(File.ReadAllBytes(Mod.refImagePath + img.ImageName));
                        RefImage.name = img.ImageName;

                        image = new GameObject().AddComponent<ReferenceImage>().Initialise(img.View, RefImage);
                    }
                    else image = new GameObject().AddComponent<ReferenceImage>().Initialise(img.View, img.ImageName);

                    image.UpdateValue("OffsetX", img.OffsetX);
                    image.UpdateValue("OffsetY", img.OffsetY);
                    image.UpdateValue("OffsetZ", img.OffsetZ);
                    image.UpdateValue("Scale", img.Scale);
                    image.UpdateValue("Rotation", img.Rotation);
                    image.UpdateValue("Opacity", img.Opacity);
                    image.Toggle(img.Active);
                    referenceImages[(int)img.View] = image;
                }
            }
            return referenceImages;
        }

        public void DeleteImageData(string craftId)
        {
            CraftImagesData images = GetImages(craftId);
            if (images != null) imageDB.CraftImagesList.Remove(images);
            SaveXml();
        }

        public void SaveXml()
        {
            _fileStream = new FileStream(Mod.refImagePath + "ReferenceImagesData.xml", FileMode.Create);
            _xmlSerializer.Serialize(_fileStream, imageDB);
            _fileStream.Close();
            //Debug.Log ("XmlSaved");
        }
    }

    public class CraftImagesDataBase
    {
        [XmlArray("Crafts")]
        public List<CraftImagesData> CraftImagesList = new();
    }

    public class CraftImagesData
    {
        [XmlAttribute]
        public string CraftName;
        [XmlArray("ReferenceImages")]
        public List<ReferenceImageData> ImageList = new();
    }

    public class ReferenceImageData
    {
        [XmlAttribute]
        public Views View;
        [XmlAttribute]
        public string ImageName;
        [XmlAttribute]
        public float Rotation, OffsetX, OffsetY, OffsetZ, Scale, Opacity;
        [XmlAttribute]
        public bool Active;
    }
}