﻿using System;
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
        public Vector2 cubeScreenPosition => new Vector2 (x, y);
        public Vector3 cubeWorldPosition => _designer.DesignerCamera.Camera.ScreenToWorldPoint (new Vector3 (x, y, screenDistance));
        private float screenDistance = 0.5f;
        private int x => _openedFlyout != null?(int) _openedFlyout.Width + 200 : 200;
        private int y => _designer.DesignerCamera.Camera.pixelHeight - 150;
        private GameObject _viewCube;
        private Renderer _viewCubeRenderer;
        private Renderer _highlighted;
        private Renderer _prevHighlighted;
        private DesignerScript _designer;
        private IFlyout _openedFlyout => Ui.Designer.DesignerToolsUIController.openedFlyout;
        private IUIResourceDatabase _resourceDatabase => XmlLayoutResourceDatabase.instance;
        private Color _highlightedColor = new Color (1f, 0.4f, 0f, 0.5f);
        private Color _hidden = new Color (1f, 0.4f, 0f, 0f);
        private bool _hovered = false;

        public ViewCube (DesignerScript designer) {
            _designer = designer;
            // _ViewCube = GameObject.Instantiate (ResourceDatabase.GetResource<GameObject> ("DesignerTools/ViewCube"));
            _viewCube = Instantiate (Mod.Instance.ResourceLoader.LoadAsset<GameObject> ("Assets/Resources/ViewCube/ViewCube.prefab"));
            _viewCube.transform.parent = _designer.DesignerCamera.Camera.transform;
            _viewCube.transform.localScale = new Vector3 (2.5f, 2.5f, 2.5f);
            _viewCubeRenderer = _viewCube.GetComponentInChildren<Renderer> ();
            //_ViewCube.gameObject.layer = 5;
        }

        public void visible (bool visible) {
            _viewCube.SetActive (visible);
        }

        public void Update () {
            _viewCube.transform.position = cubeWorldPosition;
            _viewCube.transform.rotation = Quaternion.identity;

            float distance = Vector2.Distance (cubeScreenPosition, UnityEngine.Input.mousePosition);
            if (distance < 200) {
                _hovered = true;
                OnHover (distance);
            } else if (_hovered == true) {
                _hovered = false;
                OnExit ();
            }
        }

        private void OnHover (float distance) {
            Ray ray = _designer.DesignerCamera.ScreenPointToRay (UnityEngine.Input.mousePosition);
            RaycastHit hit;
            bool cubeHit = false;
            _highlighted = null;

            if (Physics.Raycast (ray, out hit)) {
                if (hit.transform.parent?.name == "ViewCube(Clone)" || hit.transform.parent?.parent?.name == "ViewCube(Clone)") {
                    _highlighted = hit.transform.GetComponentInChildren<Renderer> ();
                    cubeHit = true;
                }
            }
            UpdateHighlight ();

            _viewCubeRenderer.material.color = cubeHit? new Color (1f, 1f, 1f, 1f) : new Color (1f, 1f, 1f, (1 - distance / 200f) + 0.3f);
        }

        private void OnExit () {
            _highlighted = null;
            UpdateHighlight ();

            _viewCubeRenderer.material.color = new Color (1f, 1f, 1f, 0.3f);
        }

        public void OnOrthoOff () {
            _viewCube.transform.localScale = new Vector3 (2.5f, 2.5f, 2.5f);
            screenDistance = 0.5f;
        }

        public void OnOrthoSizeChanged (float orthosize) {
            float scale = 10 * orthosize;
            _viewCube.transform.localScale = new Vector3 (scale, scale, scale);
            screenDistance = 1 + orthosize / 10;
        }

        private void UpdateHighlight () {
            if (_highlighted != _prevHighlighted) {
                if (_highlighted != null) _highlighted.material.color = _highlightedColor;
                if (_prevHighlighted != null) _prevHighlighted.material.color = _hidden;
                _prevHighlighted = _highlighted;
            }
        }
    }
}