    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System;
    using Assets.Scripts.Design;
    using Assets.Scripts.Tools.ObjectTransform;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Input.Events;
    using ModApi.Ui;
    using ModApi;
    using UI.Xml;
    using UnityEngine;

    namespace Assets.Scripts.DesignerTools {
        public class ReferenceImage : MonoBehaviour {
            public IUIResourceDatabase ResourceDatabase => XmlLayoutResourceDatabase.instance;
            public DesignerScript _Designer => (DesignerScript) Game.Instance.Designer;
            private ViewToolsUI _ViewToolsUI;
            public string View; //Front Back Left Right Top Bottom
            public Texture2D Image;
            private bool _EditModeOn = false;
            public bool EditModeOn => _EditModeOn;
            private bool _RotateModeOn = false;
            private bool _MoveModeOn = false;
            private bool _Active = true;
            private bool _IsLocalOrientation = true;
            private bool _MovingX = false;
            private bool _MovingY = false;
            private GameObject _ParentGameObject;
            private GameObject _ImageGameObject;
            private Transform _ImageGameObjectTransform => _ImageGameObject.transform;
            private Vector3 _XAxisStart = new Vector3 ();
            private Vector3 _YAxisStart = new Vector3 ();
            private Shader _Shader;
            private Renderer _Renderer;
            private TranslateGizmoAxisScript _XAxisGizmo;
            private TranslateGizmoAxisScript _YAxisGizmo;
            private Transform _XAxisGizmoTransform => _XAxisGizmo.transform;
            private Transform _YAxisGizmoTransform => _YAxisGizmo.transform;
            private static Material _DefaultMaterial;
            private float _Rotation = 0f, _OffsetX = 0f, _OffsetY = 0f, _Scale = 1f, _Opacity = 0.3f;
            public float Rotation => _Rotation;
            public float OffsetX => _OffsetX;
            public float OffsetY => _OffsetY;
            public float Scale => _Scale;

            public ReferenceImage (string view, Texture2D image, ViewToolsUI viewToolsUI) {
                View = view;
                _ViewToolsUI = viewToolsUI;

                _ParentGameObject = new GameObject ();
                _ImageGameObject = GameObject.CreatePrimitive (PrimitiveType.Plane);
                _ImageGameObjectTransform.parent = _ParentGameObject.transform;

                _Renderer = _ImageGameObject.GetComponent<Renderer> ();
                _Renderer.receiveShadows = false;

                Transform parent = _ParentGameObject.transform;
                _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Right);
                _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_IsLocalOrientation) ? Vector3.forward : parent.forward, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Forward);

                _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                _XAxisGizmo.name = "XAxisGizmo";
                _YAxisGizmo.name = "YAxisGizmo";

                if (View == "Front") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (90f, -90f, -90f);
                } else if (View == "Back") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (90f, 270f, 90f);
                } else if (View == "Top") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (0f, 0f, 0f);
                } else if (View == "Bottom") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (0f, 0f, 180f);
                } else if (View == "Left") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (90f + 0f, 0f, 90f);
                } else if (View == "Right") {
                    _ParentGameObject.transform.localRotation = Quaternion.Euler (90f + 0f, 0f, -90f);
                }

                UpdateImage (image);
            }

            public void OnMouseStart () {
                _XAxisStart = _XAxisGizmoTransform.localPosition;
                _YAxisStart = _YAxisGizmoTransform.localPosition;
                _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
            }

            public void OnMouseDown () {
                if (!_MovingX && !_MovingY) {
                    if (_XAxisGizmoTransform.localPosition != _XAxisStart) {
                        _MovingX = true;
                    } else if (_YAxisGizmoTransform.localPosition != _YAxisStart) {
                        _MovingY = true;
                    }
                } else if (_MovingX) {
                    _ImageGameObjectTransform.localPosition = _XAxisGizmoTransform.localPosition;
                } else if (_MovingY) {
                    _ImageGameObjectTransform.localPosition = _YAxisGizmoTransform.localPosition;
                }
                _OffsetX = _ImageGameObjectTransform.localPosition.x;
                _OffsetY = _ImageGameObjectTransform.localPosition.z;
            }

            public void OnMouseEnd () {
                _MovingX = _MovingY = false;
                _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                ApplyChanges ();
            }

            public void UpdateImage (Texture2D image) {
                Image = image;
                if (_DefaultMaterial == null) _DefaultMaterial = ResourceDatabase.GetResource<Material> ("DesignerTools/RefImageMaterial");

                try {
                    _Renderer.material = new Material (_DefaultMaterial) {
                        color = new Color (1f, 1f, 1f, _Opacity),
                        mainTexture = Image
                    };
                } catch (Exception e) { Debug.LogError ("material Error: " + e); }

                ApplyChanges ();
            }

            public void UpdateValue (string setting, float value) {
                if (setting == null) { Debug.LogError ("setting Null Error in UpdateValue"); return; }

                if (setting == "OffsetX") _OffsetX = value;
                else if (setting == "OffsetY") _OffsetY = value;
                else if (setting == "Rotation") _Rotation = value;
                else if (setting == "Scale") _Scale = value;
                else if (setting == "Opacity") _Opacity = value;

                ApplyChanges ();
            }

            private void ApplyChanges () {
                Quaternion temp = _ImageGameObjectTransform.localRotation;
                Quaternion rotation = new Quaternion ();

                if (View == "Front" || View == "Top" || View == "Right") {
                    rotation = Quaternion.Euler (temp.eulerAngles.x, _Rotation, temp.eulerAngles.z);
                } else if (View == "Back" || View == "Bottom" || View == "Left") {
                    rotation = Quaternion.Euler (temp.eulerAngles.x, -_Rotation, temp.eulerAngles.z);
                }
                _ImageGameObjectTransform.localScale = new Vector3 (_Scale * (Image.width / 1000f), 1f, _Scale * (Image.height / 1000f));
                _ImageGameObjectTransform.localRotation = _XAxisGizmoTransform.localRotation = _YAxisGizmoTransform.localRotation = rotation;
                _ImageGameObjectTransform.localPosition = _XAxisGizmoTransform.localPosition = _YAxisGizmoTransform.localPosition = new Vector3 (_OffsetX, 0f, _OffsetY);

                _Renderer.material.color = new Color (1f, 1f, 1f, _Opacity);
            }

            public void Toggle () {
                _Active = !_Active;
                _ImageGameObject.SetActive (_Active);
            }

            public void EditMode (bool editmode) {
                Debug.Log ("EditMode Changed, old: " + _EditModeOn + " new: " + editmode);
                _EditModeOn = editmode;

                _Designer.AllowPartSelection = !editmode;
                if (editmode) _ViewToolsUI.SetReferencePart (true);
                else { _ViewToolsUI.SetReferencePart (false); OnMoveImage (); }
            }

            public void OnMoveImage () {
                if (_EditModeOn) {
                    _MoveModeOn = !_MoveModeOn;
                    _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (_MoveModeOn);
                    _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (_MoveModeOn);
                } else {
                    _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                    _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                }
            }

            public void OnRotateImage () {
                if (_EditModeOn) {
                    _RotateModeOn = !_RotateModeOn;
                }
            }

            public void Destroy () {
                if (_ImageGameObject != null) {
                    _XAxisGizmo.AdjustmentGizmoScript.OnDestroy ();
                    _YAxisGizmo.AdjustmentGizmoScript.OnDestroy ();

                    UnityEngine.Object.Destroy (_XAxisGizmo.AdjustmentGizmoScript.gameObject);
                    UnityEngine.Object.Destroy (_YAxisGizmo.AdjustmentGizmoScript.gameObject);

                    TranslateGizmoAxisScript.Destroy (_XAxisGizmo);
                    TranslateGizmoAxisScript.Destroy (_YAxisGizmo);

                    UnityEngine.Object.Destroy (_ParentGameObject);
                    _ParentGameObject = null;
                }
            }
        }
    }