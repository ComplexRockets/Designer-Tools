using Assets.Scripts.Design;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.DesignerTools.ViewTools
{
    public class ViewCube : MonoBehaviour
    {
        public Vector2 CubeScreenPosition => new(X, Y);
        public Vector3 CubeWorldPosition => Mod.Instance.viewCubeCamera.ScreenToWorldPoint(new Vector3(X, Y, screenDistance));
        private const float screenDistance = 0.5f;
        private float X => (((OpenedFlyout != null && !flyoutHidden) ? OpenedFlyout.Width * 2 : 150) * Game.UiScale + 1.65f * Scale) * ScreenWidth / 3200;
        private float Y => ScreenHeight - 1.6f * Scale * ScreenHeight / 1800;
        private float Scale => ModSettings.Instance.viewCubeScale;
        private float ScreenHeight => Game.Instance.Settings.Quality.Display.Resolution.Value.height;
        private float ScreenWidth => Game.Instance.Settings.Quality.Display.Resolution.Value.width;
        private GameObject viewCubeObj;
        private Renderer viewCubeRenderer;
        private Renderer _highlighted;
        private Renderer _prevHighlighted;
        public DesignerScript designer;
        private IFlyout OpenedFlyout => designer.DesignerUi.SelectedFlyout;
        private Color _orangeHighlightedColor = new(1f, 0.4f, 0f, 0.5f);
        private Color _hiddenColor = new(1f, 0.4f, 0f, 0f);
        private Color _defaultColor = new(1f, 1f, 1f, 0.2f), _hoveredColor = new(1f, 1f, 1f, 1f);
        private bool _hovered = false;
        public bool flyoutHidden = false;

        public static ViewCube Create(DesignerScript designer)
        {
            GameObject viewCubeObj = Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Resources/ViewCube/ViewCube.prefab"));
            ViewCube viewCube = viewCubeObj.AddComponent<ViewCube>();
            viewCube.viewCubeObj = viewCubeObj;
            viewCube.designer = designer;
            viewCubeObj.transform.parent = Mod.Instance.viewCubeCamera.transform;
            viewCube.viewCubeRenderer = viewCubeObj.GetComponentInChildren<Renderer>();
            return viewCube;
        }

        public void UpdateViewCube()
        {
            viewCubeObj.transform.position = CubeWorldPosition;
            viewCubeObj.transform.rotation = Quaternion.identity;
            float scale = 2.5f * Scale / 100;
            viewCubeObj.transform.localScale = new Vector3(scale, scale, scale);

            float distance = Vector2.Distance(CubeScreenPosition, UnityEngine.Input.mousePosition);
            if (distance < 2 * Scale)
            {
                _hovered = true;
                OnMouseClose();
            }
            else if (_hovered == true)
            {
                _hovered = false;
                OnExit();
            }
        }

        public void Toggle(bool active)
        {
            viewCubeObj.SetActive(active);
        }

        private void OnMouseClose()
        {
            Ray ray = Mod.Instance.viewCubeCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            bool cubeHit = false;
            _highlighted = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 10, 1 << 20))
            {
                if (hit.transform.parent?.name == "ViewCube(Clone)" || hit.transform.parent?.parent?.name == "ViewCube(Clone)")
                {
                    _highlighted = hit.transform.GetComponentInChildren<Renderer>();
                    cubeHit = true;
                }
            }
            UpdateHighlight();

            viewCubeRenderer.material.color = cubeHit ? _hoveredColor : _defaultColor;
        }

        private void OnExit()
        {
            _highlighted = null;
            UpdateHighlight();

            viewCubeRenderer.material.color = _defaultColor;
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
            foreach (Transform child in viewCubeObj.gameObject.GetComponentsInChildren<Transform>())
            {
                Destroy(child.gameObject);
            }
        }
    }
}