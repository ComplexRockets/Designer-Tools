using System;
using Assets.Scripts.Design;
using ModApi.Input.Events;
using ModApi.Ui;
using UnityEngine;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{

    public class AutoScaler : MonoBehaviour
    {
        private Views view;
        private Vector3 A;
        private float gameDistance;
        private bool Aset => pointA != null;
        private bool Bset => pointB != null;
        private bool leftClick = false;
        private Action<float> onScaleSetAction;
        private GameObject pointA, pointB, pointer1, pointer2;
        private Transform parent;
        private Color blue = new (0.1294116f, 0.7058823f, 0.9882353f, 0.2745098f), red = new (0.9058824f, 0.317647f, 0.3529412f, 0.2745098f);

        public AutoScaler Initialise(Views _view, Transform _parent, Action<float> _onScaleSetAction)
        {
            view = _view;
            parent = _parent;
            onScaleSetAction = _onScaleSetAction;
            pointer1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer1.transform.localScale = new Vector3(0.7f, 0.01f, 0.03f);    
            pointer2.transform.localScale = new Vector3(0.03f, 0.01f, 0.7f);
            pointer1.transform.rotation = pointer2.transform.rotation = parent.rotation;
            pointer1.transform.SetParent(parent);
            pointer2.transform.SetParent(parent);
            pointer1.SetActive(false);
            pointer2.SetActive(false);
            pointer1.GetComponent<MeshRenderer>().sharedMaterial = new Material(Mod.Instance.ResourceLoader.LoadAsset<Material>("Assets/Resources/ImageHighlighter/Highlight.mat"));
            pointer2.GetComponent<MeshRenderer>().sharedMaterial = new Material(Mod.Instance.ResourceLoader.LoadAsset<Material>("Assets/Resources/ImageHighlighter/Highlight.mat"));
            DestroyImmediate(pointer1.GetComponent<Collider>());
            DestroyImmediate(pointer2.GetComponent<Collider>());
            ((DesignerScript)Game.Instance.Designer).Click += OnClick;
            return this;
        }

        public void Update()
        {
            Ray ray = Game.Instance.Designer.DesignerCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            bool pointerActive = false;
            bool pointARed = false;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string target = hit.transform.name;
                if (target.StartsWith(ReferenceImage.refImagePrefix))
                {
                    if (target.Remove(0, ReferenceImage.refImagePrefix.Length) == view.ToString())
                    {
                        pointerActive = true;
                        if (leftClick) SetPoint(hit.point);
                    }
                }
                else if (target == "PointA")
                {
                    if (leftClick) GameObject.DestroyImmediate(pointA);
                    else pointARed = true;
                }
            }

            pointer1.SetActive(pointerActive);
            pointer2.SetActive(pointerActive);
            if (pointerActive) pointer1.transform.position = pointer2.transform.position = hit.point;
            if (Aset) pointA.GetComponent<MeshRenderer>().sharedMaterial.color = pointARed ? red : blue;
            leftClick = false;
        }

        private bool OnClick(ClickEventArgs e)
        {
            if (UnityEngine.Input.GetMouseButtonDown(0)) { leftClick = true; }
            return false;
        }

        private void SetPoint(Vector3 P)
        {
            if (!Aset)
            {
                pointA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointA.transform.SetParent(parent);
                pointA.transform.position = P;
                pointA.transform.localScale = Vector3.one * 0.5f;
                pointA.name = "PointA";
                pointA.GetComponent<MeshRenderer>().sharedMaterial = new Material(Mod.Instance.ResourceLoader.LoadAsset<Material>("Assets/Resources/ImageHighlighter/Highlight.mat"));
                A = P;
                return;
            }

            if (!Bset)
            {
                pointB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointB.transform.SetParent(parent);
                pointB.transform.position = P;
                pointB.transform.localScale = Vector3.one * 0.5f;
                pointB.GetComponent<MeshRenderer>().sharedMaterial = new Material(Mod.Instance.ResourceLoader.LoadAsset<Material>("Assets/Resources/ImageHighlighter/Highlight.mat"));

                gameDistance = Vector3.Distance(A, P);

                InputDialogScript inputDialogScript = Game.Instance.UserInterface.CreateInputDialog();
                inputDialogScript.InputPlaceholderText = "LENGTH (m)";
                inputDialogScript.MessageText = "Set the length of the selected segment";
                inputDialogScript.OkayButtonText = "SET";
                inputDialogScript.CancelButtonText = "CANCEL";
                inputDialogScript.InvalidCharacters.AddRange("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()-+={}[]|\\:\";',/<>?é§èçàù€$°_¨ô`ë“‘¶«¡Çø—πôµÙôπœîºÚ†®êÂæ‡Ò∂ƒﬁÌÏÈ¬µÙÙµ¬÷≠…∞~ß◊©≈‹´„”’å»ÛÁØ–∏Ô¥ŒïªŸ™‚ÊÅÆΩ∑∆·ﬂÎÍËÓ‰‰±•¿ı∫√¢⁄›≥ ");
                inputDialogScript.OkayClicked += OnSetSetScale;
                inputDialogScript.CancelClicked += delegate
                {
                    DestroyImmediate(pointB);
                    inputDialogScript.Close();
                };
            }
        }

        private void OnSetSetScale(InputDialogScript d)
        {
            if (float.TryParse(d.InputText, out float realDistance))
            {
                float scale = realDistance / gameDistance;
                onScaleSetAction.Invoke(scale);
                d.Close();
            }
            else
            {
                Mod.Instance.Designer.DesignerUi.ShowMessage(Mod.Instance.errorColor + "Failed to parse float: " + d.InputText);
            }
        }

        public void DestroyPointers()
        {
            if (Aset) DestroyImmediate(pointA);
            if (Bset) DestroyImmediate(pointB);
            if (pointer1 != null) DestroyImmediate(pointer1);
            if (pointer2 != null) DestroyImmediate(pointer2);
        }
    }
}