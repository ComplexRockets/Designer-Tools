using UnityEngine;
namespace Assets.Scripts.DesignerTools
{
    public class DesignerToolsLoop : MonoBehaviour
    {
        public void Update()
        {
            if (Mod.Instance.designerInitialised) Mod.Instance.DesignerUpdate();
        }
    }
}