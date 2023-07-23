using UnityEngine;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ImageHighlighter : MonoBehaviour
    {
        public GameObject Xplus, Xminus, Zplus, Zminus;

        public void UpdateHighlighter(Transform parent, float imageWidth, float imageHeight, float scale)
        {
            transform.position = parent.position;
            transform.rotation = parent.rotation;
            transform.SetParent(parent);

            float Xlength = scale * (imageWidth / 100f);
            float Zlength = scale * (imageHeight / 100f);
            float s = 0.1f * Mathf.Sqrt(scale);
            Xplus.transform.localScale = Xminus.transform.localScale = new Vector3(s, s * 0.1f, Zlength);
            Zplus.transform.localScale = Zminus.transform.localScale = new Vector3(s, s * 0.1f, Xlength);

            Xplus.transform.localPosition = new Vector3(Xlength / 2f - s / 2f, 0, 0);
            Xminus.transform.localPosition = -Xplus.transform.localPosition;
            Zplus.transform.localPosition = new Vector3(0, 0, Zlength / 2f - s / 2f);
            Zminus.transform.localPosition = -Zplus.transform.localPosition;
            Toggle(true);
        }

        public void Toggle(bool active) { if (gameObject != null) gameObject.SetActive(active); }
    }
}