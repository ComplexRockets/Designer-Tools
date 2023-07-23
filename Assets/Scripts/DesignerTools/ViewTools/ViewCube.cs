using Assets.Scripts.Design;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.DesignerTools.ViewTools
{
    public class ViewCube : MonoBehaviour
    {
        public Vector2 cubeScreenPosition => new Vector2(x, y);
        public Vector3 cubeWorldPosition => Mod.Instance.viewCubeCamera.ScreenToWorldPoint(new Vector3(x, y, screenDistance));
        private float screenDistance = 0.5f;
        private float x => (((_openedFlyout != null && !flyoutHidden) ? _openedFlyout.Width * 2 : 150) * Game.UiScale + 1.65f * _scale) * screenWidth / 3200;
        private float y => screenHeight - (1.6f * _scale) * screenHeight / 1800;
        private float _scale => ModSettings.Instance.viewCubeScale;
        private float screenHeight => Game.Instance.Settings.Quality.Display.Resolution.Value.height;
        private float screenWidth => Game.Instance.Settings.Quality.Display.Resolution.Value.width;
        private GameObject _viewCube;
        private Renderer _viewCubeRenderer;
        private Renderer _highlighted;
        private Renderer _prevHighlighted;
        public DesignerScript designer;
        private IFlyout _openedFlyout => designer.DesignerUi.SelectedFlyout;
        private Color _orangeHighlightedColor = new Color(1f, 0.4f, 0f, 0.5f);
        private Color _hiddenColor = new Color(1f, 0.4f, 0f, 0f);
        private Color _defaultColor = new Color(1f, 1f, 1f, 0.2f), _hoveredColor = new Color(1f, 1f, 1f, 1f);
        private bool _hovered = false;
        public bool flyoutHidden = false;

        public ViewCube(DesignerScript designer)
        {
            this.designer = designer;
            _viewCube = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/ViewCube/ViewCube.prefab"));
            _viewCube.transform.parent = Mod.Instance.viewCubeCamera.transform;
            _viewCubeRenderer = _viewCube.GetComponentInChildren<Renderer>();
            //updateScale();
        }

        public void Update()
        {
            _viewCube.transform.position = cubeWorldPosition;
            _viewCube.transform.rotation = Quaternion.identity;
            float scale = 2.5f * _scale / 100;
            _viewCube.transform.localScale = new Vector3(scale, scale, scale);

            float distance = Vector2.Distance(cubeScreenPosition, UnityEngine.Input.mousePosition);
            if (distance < 2 * _scale)
            {
                _hovered = true;
                OnMouseClose(distance);
            }
            else if (_hovered == true)
            {
                _hovered = false;
                OnExit();
            }
        }

        // public void updateScale()
        // {
        //     float scale = 2.5f * _scale / 100;
        //     _viewCube.transform.localScale = new Vector3(scale, scale, scale);
        // }

        public void Toggle(bool active)
        {
            _viewCube.SetActive(active);
        }

        private void OnMouseClose(float distance)
        {
            Ray ray = Mod.Instance.viewCubeCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;
            bool cubeHit = false;
            _highlighted = null;

            if (Physics.Raycast(ray, out hit, 10, 1 << 20))
            {
                if (hit.transform.parent?.name == "ViewCube(Clone)" || hit.transform.parent?.parent?.name == "ViewCube(Clone)")
                {
                    _highlighted = hit.transform.GetComponentInChildren<Renderer>();
                    cubeHit = true;
                }
            }
            UpdateHighlight();

            _viewCubeRenderer.material.color = cubeHit ? _hoveredColor : _defaultColor;
        }

        private void OnExit()
        {
            _highlighted = null;
            UpdateHighlight();

            _viewCubeRenderer.material.color = _defaultColor;
        }

        private void UpdateHighlight()
        {
            if (_highlighted != _prevHighlighted)
            {
                if (_highlighted != null) _highlighted.material.color = _orangeHighlightedColor;
                if (_prevHighlighted != null) _prevHighlighted.material.color = _hiddenColor;
                _prevHighlighted = _highlighted;
            }
        }

        public void Destroy()
        {
            foreach (Transform child in _viewCube.gameObject.GetComponentsInChildren<Transform>())
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}