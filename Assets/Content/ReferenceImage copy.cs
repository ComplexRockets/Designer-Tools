    // using System.Collections.Generic;
    // using System.IO;
    // using System.Linq;
    // using System.Text;
    // using System.Threading.Tasks;
    // using System;
    // using Assets.Scripts.Design;
    // using Assets.Scripts.Tools.ObjectTransform;
    // using ModApi.Common;
    // using ModApi.Craft.Parts;
    // using ModApi.Input.Events;
    // using ModApi.Ui;
    // using ModApi;
    // using UI.Xml;
    // using UnityEngine;

    // namespace Assets.Scripts.DesignerTools {
    //     public class ReferenceImage : MonoBehaviour {
    //         public IUIResourceDatabase ResourceDatabase => XmlLayoutResourceDatabase.instance;
    //         public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
    //         public string View; //Front Back Left Right Top Bottom
    //         public Texture2D Image;
    //         private bool _EditModeOn = false;
    //         public bool EditModeOn => _EditModeOn;
    //         private bool _Active = true;
    //         private bool IsLocalOrientation = true;
    //         private GameObject _GizmoGameObject;
    //         private GameObject _ImageGameObject;
    //         private Shader _Shader;
    //         private Renderer _Renderer;
    //         private TranslateGizmoAxisScript _XAxisGizmo;
    //         private TranslateGizmoAxisScript _YAxisGizmo;
    //         private static Material DefaultMaterial;
    //         private float Rotation = 0f, OffsetX = 0f, OffsetY = 0f, Scale = 1f, Opacity = 0.3f;

    //         public ReferenceImage (string view, Texture2D image) {
    //             View = view;

    //             _GizmoGameObject = new GameObject ();
    //             _ImageGameObject = GameObject.CreatePrimitive (PrimitiveType.Plane);
    //             _Renderer = _ImageGameObject.GetComponent<Renderer> ();
    //             _Renderer.receiveShadows = false;

    //             Transform parent = _GizmoGameObject.transform;
    //             if (View == "Front") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.up, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             } else if (View == "Back") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.up, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             } else if (View == "Top") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.forward, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             } else if (View == "Bottom") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.forward, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             } else if (View == "Left") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.forward, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.up, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             } else if (View == "Right") {
    //                 _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.right : parent.forward, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //                 _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!IsLocalOrientation) ? Vector3.up : parent.up, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Custom);
    //             }

    //             _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
    //             _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
    //             _XAxisGizmo.name = "XAxisGizmo";
    //             _YAxisGizmo.name = "YAxisGizmo";

    //             UpdateImage (image);
    //         }

    //         public void OnMouseBegin () {
    //             if (View == "Front") {
    //                 OffsetX = -_XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = _YAxisGizmo.transform.localPosition.y;
    //             } else if (View == "Back") {
    //                 OffsetX = _XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = _YAxisGizmo.transform.localPosition.y;
    //             } else if (View == "Top") {
    //                 OffsetX = -_XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = -_YAxisGizmo.transform.localPosition.y;
    //             } else if (View == "Bottom") {
    //                 OffsetX = -_XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = _YAxisGizmo.transform.localPosition.y;
    //             } else if (View == "Left") {
    //                 OffsetX = _XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = _YAxisGizmo.transform.localPosition.y;
    //             } else if (View == "Right") {
    //                 OffsetX = _XAxisGizmo.transform.localPosition.x;
    //                 OffsetY = _YAxisGizmo.transform.localPosition.y;
    //             }
    //             ApplyChanges ();
    //         }

    //         public void UpdateImage (Texture2D image) {
    //             Image = image;
    //             if (DefaultMaterial == null) DefaultMaterial = ResourceDatabase.GetResource<Material> ("DesignerTools/RefImageMaterial");

    //             try {
    //                 _Renderer.material = new Material (DefaultMaterial) {
    //                     color = new Color (1f, 1f, 1f, Opacity),
    //                     mainTexture = Image
    //                 };
    //             } catch (Exception e) { Debug.LogError ("material Error: " + e); }

    //             ApplyChanges ();
    //         }

    //         public void UpdateValue (string setting, float value) {
    //             if (setting == null) { Debug.LogError ("setting Null Error in UpdateValue"); return; }

    //             if (setting == "OffsetX") OffsetX = value;
    //             else if (setting == "OffsetY") OffsetY = value;
    //             else if (setting == "Rotation") Rotation = value;
    //             else if (setting == "Scale") Scale = value;
    //             else if (setting == "Opacity") Opacity = value;

    //             ApplyChanges ();
    //         }

    //         private void ApplyChanges () {
    //             if (View == "Front") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (90f + Rotation, -90f, -90f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (-OffsetX, OffsetY, 0f);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (-OffsetX, OffsetY, 0f);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (OffsetX, OffsetY, 0f);
    //             } else if (View == "Back") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (90f + -Rotation, 270f, 90f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (OffsetX, OffsetY, 0f);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (OffsetX, OffsetY, 0f);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (OffsetX, OffsetY, 0f);
    //             } else if (View == "Top") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (0f, Rotation, 0f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (-OffsetX, 0f, -OffsetY);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (-OffsetX, 0f, -OffsetY);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (-OffsetX, 0f, -OffsetY);
    //             } else if (View == "Bottom") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (0f, -Rotation, 180f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (-OffsetX, 0f, OffsetY);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (-OffsetX, 0f, OffsetY);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (-OffsetX, 0f, OffsetY);
    //             } else if (View == "Left") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (90f + Rotation, 0f, 90f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (0f, -OffsetY, OffsetX);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (0f, OffsetX, OffsetY);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (0f, OffsetX, OffsetY);
    //             } else if (View == "Right") {
    //                 _ImageGameObject.transform.localRotation = Quaternion.Euler (90f + Rotation, 0f, -90f);
    //                 _ImageGameObject.transform.localPosition = new Vector3 (0f, OffsetY, OffsetX);
    //                 _XAxisGizmo.transform.localPosition = new Vector3 (0f, OffsetX, OffsetY);
    //                 _YAxisGizmo.transform.localPosition = new Vector3 (OffsetX, OffsetY, 0f);
    //             }
    //             _ImageGameObject.transform.localScale = new Vector3 (Scale * (Image.width / 1000f), 1f, Scale * (Image.height / 1000f));
    //             _Renderer.material.color = new Color (1f, 1f, 1f, Opacity);
    //         }

    //         public void Toggle () {
    //             _Active = !_Active;
    //             _ImageGameObject.SetActive (_Active);
    //         }

    //         public void EditMode (bool editmode) {
    //             Debug.Log ("EditMode Changed, old: " + _EditModeOn + " new: " + editmode);
    //             _EditModeOn = editmode;

    //             _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (editmode);
    //             _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (editmode);
    //             _Designer.AllowPartSelection = editmode;
    //             if (editmode) _Designer.DeselectPart ();
    //         }

    //         public void Destroy () {
    //             if (_ImageGameObject != null) {
    //                 _XAxisGizmo.AdjustmentGizmoScript.OnDestroy ();
    //                 _YAxisGizmo.AdjustmentGizmoScript.OnDestroy ();
    //                 UnityEngine.Object.Destroy (_XAxisGizmo.AdjustmentGizmoScript.gameObject);
    //                 UnityEngine.Object.Destroy (_YAxisGizmo.AdjustmentGizmoScript.gameObject);
    //                 TranslateGizmoAxisScript.Destroy (_XAxisGizmo);
    //                 TranslateGizmoAxisScript.Destroy (_YAxisGizmo);

    //                 UnityEngine.Object.Destroy (_ImageGameObject);
    //                 _ImageGameObject = null;
    //             }
    //         }
    //     }
    // }