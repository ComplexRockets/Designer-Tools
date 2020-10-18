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
            public IUIResourceDatabase resourceDatabase => XmlLayoutResourceDatabase.instance;
            public DesignerScript designer => (DesignerScript) Game.Instance.Designer;
            public ViewToolsUI viewToolsUI;
            public string view; //Front Back Left Right Top Bottom
            public Texture2D image;
            private bool _editModeOn = false;
            public bool editModeOn => _editModeOn;
            private bool _rotateModeOn = false;
            private bool _moveModeOn = false;
            private bool _active = true;
            private bool _isLocalOrientation = true;
            private bool _movingX = false;
            private bool _movingY = false;
            private GameObject _parentGameObject;
            private GameObject _imageGameObject;
            private GameObject _image;
            private GameObject _gizmoGameObject;
            private TranslateGizmoAxisScript _xAxisGizmo;
            private TranslateGizmoAxisScript _yAxisGizmo;
            private RotateGizmoAxisScript _rotateGizmo;
            private Vector3 _xAxisStart = new Vector3 ();
            private Vector3 _yAxisStart = new Vector3 ();
            private Shader _shader;
            private Renderer _renderer;
            private static Material _defaultMaterial;
            private float _rotation = 0f, _OffsetX = 0f, _OffsetY = 0f, _Scale = 1f, _opacity = 0.3f;
            public float rotation => _rotation;
            public float offsetX => _OffsetX;
            public float offsetY => _OffsetY;
            public float scale => _Scale;
            public float opacity => _opacity;
            public bool active => _active;

            public ReferenceImage (string view, Texture2D image, ViewToolsUI viewToolsUI) {
                this.view = view;
                this.viewToolsUI = viewToolsUI;

                _parentGameObject = new GameObject ();
                _parentGameObject.transform.parent = designer.CraftScript?.RootPart.Transform;
                _gizmoGameObject = new GameObject ();
                _gizmoGameObject.transform.parent = _parentGameObject.transform;

                _imageGameObject = new GameObject ();
                _imageGameObject.transform.parent = _parentGameObject.transform;
                _image = GameObject.CreatePrimitive (PrimitiveType.Plane);
                _image.transform.parent = _imageGameObject.transform;

                _renderer = _image.GetComponent<Renderer> ();
                _renderer.receiveShadows = false;

                Transform parent = _gizmoGameObject.transform;
                _xAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_isLocalOrientation) ? Vector3.right : parent.right, new Color (1f, 0f, 0f), 2.5f, true, designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Right);
                _yAxisGizmo = TranslateGizmoAxisScript.Create (parent, () => (!_isLocalOrientation) ? Vector3.forward : parent.forward, new Color (0f, 1f, 0f), 2.5f, true, designer.GizmoCamera, TranslateGizmoAxisScript.GizmoAxisType.Forward);
                //_RotateGizmo = RotateGizmoAxisScript.Create (new RotateGizmo (), parent, Utilities.UnityTransform.TransformAxis.X, new Color (1f, 0f, 0f, 1f), 2.5f);
                _xAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                _yAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);

                //_XAxisGizmo.name = "XAxisGizmo";
                //_YAxisGizmo.name = "YAxisGizmo";

                Quaternion ParentRot = _parentGameObject.transform.parent.rotation;
                _parentGameObject.transform.parent.rotation = designer.ActiveCraftConfiguration.Type == ModApi.Craft.CrafConfigurationType.Plane ? Quaternion.Euler (90f, 0f, 0f) : Quaternion.identity;

                if (this.view == "Front") {
                _parentGameObject.transform.rotation = Quaternion.Euler (90f, -90f, -90f);
                } else if (this.view == "Back") {
                _parentGameObject.transform.rotation = Quaternion.Euler (90f, 270f, 90f);
                } else if (this.view == "Top") {
                _parentGameObject.transform.rotation = Quaternion.Euler (0f, -90f, 0f);
                } else if (this.view == "Bottom") {
                _parentGameObject.transform.rotation = Quaternion.Euler (180f, 90f, 0f);
                } else if (this.view == "Left") {
                _parentGameObject.transform.rotation = Quaternion.Euler (90f, 0f, 90f);
                } else if (this.view == "Right") {
                _parentGameObject.transform.rotation = Quaternion.Euler (90f, 0f, -90f);
                }
                _parentGameObject.transform.parent.rotation = ParentRot;
                //_ParentGameObject.transform.position = (Vector3) _Designer.CraftScript?.RootPart.Transform.position;
                _parentGameObject.transform.localPosition = new Vector3 ();

                UpdateImage (image);
            }

            public void OnMouseStart () {
                if (_moveModeOn) {
                    _xAxisStart = _xAxisGizmo.transform.localPosition;
                    _yAxisStart = _yAxisGizmo.transform.localPosition;
                } else if (_rotateModeOn) {

                }
            }

            public void OnMouseDown () {
                if (_moveModeOn) {
                    if (!_movingX && !_movingY) {
                        if (_xAxisGizmo.transform.localPosition != _xAxisStart) {
                            _movingX = true;
                            _yAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                        } else if (_yAxisGizmo.transform.localPosition != _yAxisStart) {
                            _movingY = true;
                            _xAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                        }
                    } else if (_movingX) {
                        _image.transform.localPosition = _xAxisGizmo.transform.localPosition;
                    } else {
                        _image.transform.localPosition = _yAxisGizmo.transform.localPosition;
                    }
                } else if (_rotateModeOn) {

                } else return;

                viewToolsUI?.OnUIValueChanged (this);
            }

            public void OnMouseEnd () {
                if (_moveModeOn) {
                    if (_movingX || _movingY) {
                        _movingX = _movingY = false;

                        _imageGameObject.transform.position = _gizmoGameObject.transform.position = _image.transform.position;
                        _image.transform.localPosition = _xAxisGizmo.transform.localPosition = _yAxisGizmo.transform.localPosition = new Vector3 ();

                        _OffsetX = _imageGameObject.transform.localPosition.x;
                        _OffsetY = _imageGameObject.transform.localPosition.z;
                    }
                    _xAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                    _yAxisGizmo.AdjustmentGizmoScript.SetVisibility (true);
                } else if (_rotateModeOn) {

                }

                viewToolsUI?.OnUIValueChanged (this);
            }

            public void UpdateImage (Texture2D image) {
                this.image = image;
                if (_defaultMaterial == null) _defaultMaterial = resourceDatabase.GetResource<Material> ("DesignerTools/RefImageMaterial");

                try {
                _renderer.material = new Material(_defaultMaterial) {
                        color = new Color(1f, 1f, 1f, _opacity),
                        mainTexture = this.image
                    };
                } catch (Exception e) { Debug.LogError ("material Error: " + e); }

                ApplyChanges ();
            }

            public void UpdateValue (string setting, float value) {
                if (setting == null) { Debug.LogError ("setting Null Error in UpdateValue"); return; }

                if (setting == "OffsetX") _OffsetX = value;
                else if (setting == "OffsetY") _OffsetY = value;
                else if (setting == "Rotation") _rotation = value;
                else if (setting == "Scale") _Scale = value;
                else if (setting == "Opacity") _opacity = value;

                ApplyChanges ();
            }

            public void UpdateOrigin (Vector3 Origin) {
                //_ParentGameObject.transform.position = Origin;
                //ApplyChanges ();
            }

            public void ApplyChanges () {
                Quaternion temp = _imageGameObject.transform.localRotation;

                _image.transform.localScale = new Vector3 (_Scale * (image.width / 1000f), 1f, _Scale * (image.height / 1000f));
                _imageGameObject.transform.localRotation = _gizmoGameObject.transform.localRotation = Quaternion.Euler (temp.eulerAngles.x, _rotation, temp.eulerAngles.z);
                _imageGameObject.transform.localPosition = _gizmoGameObject.transform.localPosition = new Vector3 (_OffsetX, 0f, _OffsetY);
                _image.transform.localPosition = _xAxisGizmo.transform.localPosition = _yAxisGizmo.transform.localPosition = new Vector3 ();

                _renderer.material.color = new Color (1f, 1f, 1f, _opacity);

                viewToolsUI?.OnUIValueChanged (this);
            }

            public void Toggle () {
                _active = !_active;
                _imageGameObject.SetActive (_active);
            }

            public void EditMode (bool editmode) {
                _editModeOn = editmode;

                if (editmode) //_Designer.FuselageShapeTool.Activate (); 
                    viewToolsUI.SetReferencePart (true);
                else { //_Designer.FuselageShapeTool.Deactivate (); OnMoveImage (); OnRotateImage (); 
                    viewToolsUI.SetReferencePart (false);
                    OnMoveImage ();
                    OnRotateImage ();
                }
            }

            public void OnMoveImage () {
                if (_editModeOn) {
                    _moveModeOn = !_moveModeOn;
                    if (_moveModeOn) _rotateModeOn = false;

                    _xAxisGizmo.AdjustmentGizmoScript.SetVisibility (_moveModeOn);
                    _yAxisGizmo.AdjustmentGizmoScript.SetVisibility (_moveModeOn);
                } else {
                    _xAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                    _yAxisGizmo.AdjustmentGizmoScript.SetVisibility (false);
                    _moveModeOn = false;
                }
            }

            public void OnRotateImage () {
                if (_editModeOn) {
                    _rotateModeOn = !_rotateModeOn;
                    if (_rotateModeOn) _moveModeOn = false;
                } else {
                    _rotateModeOn = false;
                }
            }

            public void Destroy () {
                if (_imageGameObject != null) {
                    _xAxisGizmo.AdjustmentGizmoScript.OnDestroy ();
                    _yAxisGizmo.AdjustmentGizmoScript.OnDestroy ();

                    UnityEngine.Object.Destroy (_xAxisGizmo.AdjustmentGizmoScript.gameObject);
                    UnityEngine.Object.Destroy (_yAxisGizmo.AdjustmentGizmoScript.gameObject);

                    TranslateGizmoAxisScript.Destroy (_xAxisGizmo);
                    TranslateGizmoAxisScript.Destroy (_yAxisGizmo);

                    UnityEngine.Object.Destroy (_parentGameObject);
                    _parentGameObject = null;
                }
            }
        }
    }