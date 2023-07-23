using UnityEngine;
namespace Assets.Scripts.DesignerTools
{
    public class DesignerToolsLoop : MonoBehaviour
    {
        private Mod _mod = Mod.Instance;

        protected virtual void Update()
        {
            if (Mod.Instance.designerInitialised) Mod.Instance.DesignerUpdate();
        }
    }
}