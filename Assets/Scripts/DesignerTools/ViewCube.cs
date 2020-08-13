using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.Craft.Parts.Modifiers.Fuselage;
using Assets.Scripts.Design;
using Assets.Scripts.DesignerTools;
using Assets.Scripts.Tools;
using Assets.Scripts.Ui.Designer;
using ModApi;
using ModApi.Common;
using ModApi.Craft.Parts;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Mods;
using ModApi.Scenes.Events;
using ModApi.Ui;
using UI.Xml;
using UnityEngine;

namespace Assets.Scripts.DesignerTools {
    public class ViewCube : MonoBehaviour {
        public Vector2 CubeScreenPosition => new Vector2 (x, y);
        public Vector3 CubeWorldPosition => _Designer.DesignerCamera.Camera.ScreenToWorldPoint (new Vector3 (x, y, 20));
        private int x => _OpenedFlyout != null? (int) _OpenedFlyout.Width + 200 : 200;
        private int y => _Designer.DesignerCamera.Camera.pixelHeight - 150;
        private GameObject _ViewCube;
        private Renderer _ViewCubeRenderer;
        private Renderer _Highlighted;
        private Renderer _PrevHighlighted;
        private DesignerScript _Designer;
        private IFlyout _OpenedFlyout => Ui.Designer.DesignerToolsUIController.OpenedFlyout;
        private IUIResourceDatabase ResourceDatabase => XmlLayoutResourceDatabase.instance;
        private Color Highlighted = new Color (1f, 0.4f, 0f, 0.5f);
        private Color Hidden = new Color (1f, 0.4f, 0f, 0f);
        private bool Hovered = false;

        public ViewCube (DesignerScript designer) {
            _Designer = designer;
            // _ViewCube = GameObject.Instantiate (ResourceDatabase.GetResource<GameObject> ("DesignerTools/ViewCube"));
            _ViewCube = Instantiate (Mod.Instance.ResourceLoader.LoadAsset<GameObject> ("Assets/Resources/ViewCube/ViewCube.prefab"));
            _ViewCube.transform.parent = _Designer.DesignerCamera.Camera.transform;
            _ViewCubeRenderer = _ViewCube.GetComponentInChildren<Renderer> ();
            //_ViewCube.gameObject.layer = 5;
        }

        public void visible (bool visible) {
            _ViewCube.SetActive (visible);
        }

        public void Update () {
            _ViewCube.transform.position = CubeWorldPosition;
            _ViewCube.transform.rotation = Quaternion.identity;

            float distance = Vector2.Distance (CubeScreenPosition, UnityEngine.Input.mousePosition);
            if (distance < 200) {
                Hovered = true;
                OnHover (distance);
            } else if (Hovered == true) {
                Hovered = false;
                OnExit ();
            }
        }

        private void OnHover (float distance) {
            Ray ray = _Designer.DesignerCamera.ScreenPointToRay (UnityEngine.Input.mousePosition);
            RaycastHit hit;
            bool cubeHit = false;
            _Highlighted = null;

            if (Physics.Raycast (ray, out hit)) {
                if (hit.transform.parent?.name == "ViewCube(Clone)" || hit.transform.parent?.parent?.name == "ViewCube(Clone)") {
                    _Highlighted = hit.transform.GetComponentInChildren<Renderer> ();
                    cubeHit = true;
                }
            }
            UpdateHighlight ();

            _ViewCubeRenderer.material.color = cubeHit? new Color (1f, 1f, 1f, 1f) : new Color (1f, 1f, 1f, (1 - distance / 200f) + 0.3f);
        }

        private void OnExit () {
            _Highlighted = null;
            UpdateHighlight ();

            _ViewCubeRenderer.material.color = new Color (1f, 1f, 1f, 0.3f);
        }

        private void UpdateHighlight () {
            if (_Highlighted != _PrevHighlighted) {
                if (_Highlighted != null) _Highlighted.material.color = Highlighted;
                if (_PrevHighlighted != null) _PrevHighlighted.material.color = Hidden;
                _PrevHighlighted = _Highlighted;
            }
        }
    }
}