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
            public ViewToolsUI ViewToolsUI;
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
            private GameObject _Image;
            private GameObject _GizmoGameObject;
            private TranslateGizmoAxisScript _XAxisGizmo;
            private TranslateGizmoAxisScript _YAxisGizmo;
            private RotateGizmoAxisScript _RotateGizmo;
            private Vector3 _XAxisStart = new Vector3 ();
            private Vector3 _YAxisStart = new Vector3 ();
            private Shader _Shader;
            private Renderer _Renderer;
            private static Material _DefaultMaterial;
            private float _Rotation = 0f, _OffsetX = 0f, _OffsetY = 0f, _Scale = 1f, _Opacity = 0.3f;
            public float Rotation => _Rotation;
            public float OffsetX => _OffsetX;
            public float OffsetY => _OffsetY;
            public float Scale => _Scale;
            public float Opacity => _Opacity;
            public bool Active => _Active;

            public ReferenceImage (string view, Texture2D image, ViewToolsUI viewToolsUI) {
                View = view;
                ViewToolsUI = viewToolsUI;

                _ParentGameObject = new GameObject ();
                _ParentGameObject.transform.parent = _Designer.CraftScript?.RootPart.Transform;
                _GizmoGameObject = new GameObject ();
                _GizmoGameObject.transform.parent = _ParentGameObject.transform;

                _ImageGameObject = new GameObject ();
                _ImageGameObject.transform.parent = _ParentGameObject.transform;
                _Image = GameObject.CreatePrimitive (PrimitiveType.Plane);
                _Image.transform.parent = _ImageGameObject.transform;

                _Renderer = _Image.GetComponent<Renderer> ();
                _Renderer.receiveShadows = false;

                Transform parent = _GizmoGameObject.transform;
                _XAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_IsLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Right);
                _YAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_IsLocalOrientation) ? Vector3.forward : parent.forward, new Color (0f, 1f, 0f), 2.5f, true, _Designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Forward);
                //_RotateGizmo = RotateGizmoAxisScript.Create (new RotateGizmo (), parent, Utilities.UnityTransform.TransformAxis.X, new Color (1f, 0f, 0f, 1f), 2.5f);
                _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);

                //_XAxisGizmo.name = "XAxisGizmo";
                //_YAxisGizmo.name = "YAxisGizmo";

                Quaternion ParentRot = _ParentGameObject.transform.parent.rotation;
                _ParentGameObject.transform.parent.rotation = _Designer.ActiveCraftConfiguration.Type == ModApi.Craft.CrafConfigurationType.Plane ? Quaternion.Euler (90f, 0f, 0f) : Quaternion.identity;

                if (View == "Front") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (90f, -90f, -90f);
                } else if (View == "Back") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (90f, 270f, 90f);
                } else if (View == "Top") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (0f, -90f, 0f);
                } else if (View == "Bottom") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (180f, 90f, 0f);
                } else if (View == "Left") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (90f, 0f, 90f);
                } else if (View == "Right") {
                    _ParentGameObject.transform.rotation = Quaternion.Euler (90f, 0f, -90f);
                }
                _ParentGameObject.transform.parent.rotation = ParentRot;
                //_ParentGameObject.transform.position = (Vector3) _Designer.CraftScript?.RootPart.Transform.position;
                _ParentGameObject.transform.localPosition = new Vector3 ();

                UpdateImage (image);
            }

            public void OnMouseStart () {
                if (_MoveModeOn) {
                    _XAxisStart = _XAxisGizmo.transform.localPosition;
                    _YAxisStart = _YAxisGizmo.transform.localPosition;
                } else if (_RotateModeOn) {

                }
            }

            public void OnMouseDown () {
                if (_MoveModeOn) {
                    if (!_MovingX && !_MovingY) {
                        if (_XAxisGizmo.transform.localPosition != _XAxisStart) {
                            _MovingX = true;
                            _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                        } else if (_YAxisGizmo.transform.localPosition != _YAxisStart) {
                            _MovingY = true;
                            _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                        }
                    } else if (_MovingX) {
                        _Image.transform.localPosition = _XAxisGizmo.transform.localPosition;
                    } else {
                        _Image.transform.localPosition = _YAxisGizmo.transform.localPosition;
                    }
                } else if (_RotateModeOn) {

                } else return;

                ViewToolsUI?.OnUIValueChanged (this);
            }

            public void OnMouseEnd () {
                if (_MoveModeOn) {
                    if (_MovingX || _MovingY) {
                        _MovingX = _MovingY = false;

                        _ImageGameObject.transform.position = _GizmoGameObject.transform.position = _Image.transform.position;
                        _Image.transform.localPosition = _XAxisGizmo.transform.localPosition = _YAxisGizmo.transform.localPosition = new Vector3 ();

                        _OffsetX = _ImageGameObject.transform.localPosition.x;
                        _OffsetY = _ImageGameObject.transform.localPosition.z;
                    }
                    _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                    _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                } else if (_RotateModeOn) {

                }

                ViewToolsUI?.OnUIValueChanged (this);
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

            public void UpdateOrigin (Vector3 Origin) {
                //_ParentGameObject.transform.position = Origin;
                //ApplyChanges ();
            }

            public void ApplyChanges () {
                Quaternion temp = _ImageGameObject.transform.localRotation;

                _Image.transform.localScale = new Vector3 (_Scale * (Image.width / 1000f), 1f, _Scale * (Image.height / 1000f));
                _ImageGameObject.transform.localRotation = _GizmoGameObject.transform.localRotation = Quaternion.Euler (temp.eulerAngles.x, _Rotation, temp.eulerAngles.z);
                _ImageGameObject.transform.localPosition = _GizmoGameObject.transform.localPosition = new Vector3 (_OffsetX, 0f, _OffsetY);
                _Image.transform.localPosition = _XAxisGizmo.transform.localPosition = _YAxisGizmo.transform.localPosition = new Vector3 ();

                _Renderer.material.color = new Color (1f, 1f, 1f, _Opacity);

                ViewToolsUI?.OnUIValueChanged (this);
            }

            public void Toggle () {
                _Active = !_Active;
                _ImageGameObject.SetActive (_Active);
            }

            public void EditMode (bool editmode) {
                _EditModeOn = editmode;

                if (editmode) //_Designer.FuselageShapeTool.Activate (); 
                    ViewToolsUI.SetReferencePart (true);
                else { //_Designer.FuselageShapeTool.Deactivate (); OnMoveImage (); OnRotateImage (); 
                    ViewToolsUI.SetReferencePart (false);
                    OnMoveImage ();
                    OnRotateImage ();
                }
            }

            public void OnMoveImage () {
                if (_EditModeOn) {
                    _MoveModeOn = !_MoveModeOn;
                    if (_MoveModeOn) _RotateModeOn = false;

                    _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (_MoveModeOn);
                    _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (_MoveModeOn);
                } else {
                    _XAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                    _YAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                    _MoveModeOn = false;
                }
            }

            public void OnRotateImage () {
                if (_EditModeOn) {
                    _RotateModeOn = !_RotateModeOn;
                    if (_RotateModeOn) _MoveModeOn = false;
                } else {
                    _RotateModeOn = false;
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