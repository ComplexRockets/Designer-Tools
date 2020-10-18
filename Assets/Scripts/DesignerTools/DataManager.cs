using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Assets.Scripts;
using UnityEngine;

namespace Assets.Scripts.DesignerTools {
    public class DataManager {
        private Mod _mod => Mod.Instance;
        private XmlSerializer _xmlSerializer;
        private FileStream _fileStream;
        public CraftImagesDataBase imageDB;

        public void initialise () {
            //Debug.Log ("DataManagerInitialised");
            string path = _mod.refImagePath + "/ReferenceImagesData.xml";
            _xmlSerializer = new XmlSerializer (typeof (CraftImagesDataBase));

            if (!File.Exists (path)) SaveXml ();

            using (StringReader reader = new StringReader (File.ReadAllText (path))) {
                imageDB = (_xmlSerializer.Deserialize (reader)) as CraftImagesDataBase;
            }
            if (imageDB == null) imageDB = new CraftImagesDataBase ();
        }

        public void SaveImages (string craftName, List<ReferenceImage> images) {
            try {
                CraftImagesData ImageData = GetImages (craftName);
                if (ImageData != null) imageDB.CraftImagesList.Remove (ImageData);
            } catch (Exception e) { Debug.Log ("Existing CraftImagesData check failed: " + e); }

            List<ReferenceImageData> _ImageList = new List<ReferenceImageData> ();
            foreach (ReferenceImage image in images) {
                _ImageList.Add (new ReferenceImageData () {
                    View = image.view,
                        Rotation = image.rotation,
                        OffsetX = image.offsetX,
                        OffsetY = image.offsetY,
                        Scale = image.scale,
                        Opacity = image.opacity,
                        Active = image.active,
                        ImageName = image.image.name
                });
            }

            imageDB.CraftImagesList.Add (new CraftImagesData () {
                CraftName = craftName,
                    ImageList = _ImageList
            });
        }

        public CraftImagesData GetImages (string craftID) {
            foreach (CraftImagesData Images in imageDB.CraftImagesList) {
                if (Images.CraftName == craftID) {
                    return Images;
                }
            }
            return null;
        }

        public List<ReferenceImage> LoadImages (string craftID) {
            //Debug.Log ("LoadingImages...");
            CraftImagesData images = GetImages (craftID);
            if (images != null) {
                List<ReferenceImage> ReferenceImages = new List<ReferenceImage> ();
                foreach (ReferenceImageData image in images.ImageList) {
                    try {
                        Texture2D RefImage = new Texture2D (0, 0);
                        RefImage.LoadImage (File.ReadAllBytes (_mod.refImagePath + image.ImageName));
                        RefImage.name = image.ImageName;

                        ReferenceImages.Add (new ReferenceImage (image.View, RefImage, null));
                        ReferenceImages.Last ().UpdateValue ("OffsetX", image.OffsetX);
                        ReferenceImages.Last ().UpdateValue ("OffsetY", image.OffsetY);
                        ReferenceImages.Last ().UpdateValue ("Scale", image.Scale);
                        ReferenceImages.Last ().UpdateValue ("Rotation", image.Rotation);
                        ReferenceImages.Last ().UpdateValue ("Opacity", image.Opacity);
                        if (ReferenceImages.Last ().active != image.Active) ReferenceImages.Last ().Toggle ();

                    } catch (Exception e) { Debug.LogError ("Image Error: " + e); }
                }
                return ReferenceImages;
            }
            //Debug.Log ("Images null");
            return null;
        }

        public void SaveXml () {
            _fileStream = new FileStream (_mod.refImagePath + "/ReferenceImagesData.xml", FileMode.Create);
            _xmlSerializer.Serialize (_fileStream, imageDB);
            _fileStream.Close ();
            //Debug.Log ("XmlSaved");
        }
    }

    public class CraftImagesDataBase {
        [XmlArrayAttribute ("Crafts")]
        public List<CraftImagesData> CraftImagesList = new List<CraftImagesData> ();
    }

    public class CraftImagesData {
        [XmlAttribute]
        public string CraftName;
        [XmlArray ("ReferenceImages")]
        public List<ReferenceImageData> ImageList = new List<ReferenceImageData> ();
    }

    public class ReferenceImageData {
        [XmlAttribute]
        public string View;
        [XmlAttribute]
        public string ImageName;
        [XmlAttribute]
        public float Rotation, OffsetX, OffsetY, Scale, Opacity;
        [XmlAttribute]
        public bool Active;
    }
}