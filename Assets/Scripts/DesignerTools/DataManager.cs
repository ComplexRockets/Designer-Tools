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
    public class DataManager : MonoBehaviour {
        private XmlSerializer _XmlSerializer;
        private FileStream _FileStream;
        public CraftImagesDataBase ImageDB;

        public void initialise () {
            Debug.Log ("DataManagerInitialised");
            string path = Mod.Instance.RefImagePath + "/ReferenceImagesData.xml";
            _XmlSerializer = new XmlSerializer (typeof (CraftImagesDataBase));

            if (!File.Exists (path)) SaveXml ();

            using (StringReader reader = new StringReader (File.ReadAllText (path))) {
                ImageDB = (_XmlSerializer.Deserialize (reader)) as CraftImagesDataBase;
            }
            if (ImageDB == null) ImageDB = new CraftImagesDataBase ();
        }

        public void SaveImages (string craftName, List<ReferenceImage> images) {
            try {
                CraftImagesData ImageData = GetImages (craftName);
                if (ImageData != null) ImageDB.CraftImagesList.Remove (ImageData);
            } catch (Exception e) { Debug.Log ("Existing CraftImagesData check failed: " + e); }

            List<ReferenceImageData> _ImageList = new List<ReferenceImageData> ();
            foreach (ReferenceImage image in images) {
                _ImageList.Add (new ReferenceImageData () {
                    View = image.View,
                        Rotation = image.Rotation,
                        OffsetX = image.OffsetX,
                        OffsetY = image.OffsetY,
                        Scale = image.Scale,
                        Opacity = image.Opacity,
                        Active = image.Active,
                        ImageName = image.Image.name
                });
            }

            ImageDB.CraftImagesList.Add (new CraftImagesData () {
                CraftName = craftName,
                    ImageList = _ImageList
            });
        }

        public CraftImagesData GetImages (string craftID) {
            foreach (CraftImagesData Images in ImageDB.CraftImagesList) {
                if (Images.CraftName == craftID) {
                    return Images;
                }
            }
            return null;
        }

        public List<ReferenceImage> LoadImages (string craftID) {
            Debug.Log ("LoadingImages...");
            CraftImagesData images = GetImages (craftID);
            if (images != null) {
                List<ReferenceImage> ReferenceImages = new List<ReferenceImage> ();
                foreach (ReferenceImageData image in images.ImageList) {
                    try {
                        Texture2D RefImage = new Texture2D (0, 0);
                        RefImage.LoadImage (File.ReadAllBytes (Mod.Instance.RefImagePath + image.ImageName));
                        RefImage.name = image.ImageName;

                        ReferenceImages.Add (new ReferenceImage (image.View, RefImage, null));
                        ReferenceImages.Last ().UpdateValue ("OffsetX", image.OffsetX);
                        ReferenceImages.Last ().UpdateValue ("OffsetY", image.OffsetY);
                        ReferenceImages.Last ().UpdateValue ("Scale", image.Scale);
                        ReferenceImages.Last ().UpdateValue ("Rotation", image.Rotation);
                        ReferenceImages.Last ().UpdateValue ("Opacity", image.Opacity);
                        if (ReferenceImages.Last ().Active != image.Active) ReferenceImages.Last ().Toggle ();

                    } catch (Exception e) { Debug.Log ("Image Error: " + e); }
                }
                return ReferenceImages;
            }
            Debug.Log ("Images null");
            return null;
        }

        public void SaveXml () {
            _FileStream = new FileStream (Mod.Instance.RefImagePath + "/ReferenceImagesData.xml", FileMode.Create);
            _XmlSerializer.Serialize (_FileStream, ImageDB);
            _FileStream.Close ();
            Debug.Log ("XmlSaved");
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