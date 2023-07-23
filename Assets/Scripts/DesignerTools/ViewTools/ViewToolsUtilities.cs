using Assets.Scripts.Design;
using System.Collections.Generic;
using ModApi.Craft.Parts;
using Assets.Scripts.DesignerTools.ReferenceImages;
using HarmonyLib;
using UnityEngine;
using System;

namespace Assets.Scripts.DesignerTools.ViewTools
{
    public enum ViewModes { Normal, Isolate, Xray }

    public static class ViewToolsUtilities
    {
        public static ViewModes viewMode = ViewModes.Normal;
        public static DesignerScript designer => (DesignerScript)Game.Instance.Designer;

        public static void IsolateRefImages(List<ReferenceImage> isolatedImages)
        {
            if (viewMode != ViewModes.Normal && viewMode != ViewModes.Isolate) SetNormalView();
            viewMode = ViewModes.Isolate;
            foreach (ReferenceImage img in Mod.Instance.referenceImages) img.Hide(!isolatedImages.Contains(img));
            HideDesignerPlatform();
        }

        public static void IsolateParts(List<string> isolatedPartsID)
        {
            if (viewMode != ViewModes.Normal && viewMode != ViewModes.Isolate) SetNormalView();
            viewMode = ViewModes.Isolate;
            foreach (PartData part in designer.CraftScript.Data.Assembly.Parts)
            {
                if (isolatedPartsID.Contains(part.Id.ToString())) part.PartScript.DesignerInteractionMode = PartDesignerInteractionMode.Normal;
                else part.PartScript.DesignerInteractionMode = PartDesignerInteractionMode.Disabled;
            }
        }

        public static void HideDesignerPlatform() => Traverse.Create(designer.DesignerPlatform).Field<MeshRenderer>("_platformRenderer").Value.enabled = false;

        public static void SetNormalView()
        {
            foreach (ReferenceImage image in Mod.Instance.referenceImages) image.Hide(false);
            foreach (PartData part in designer.CraftScript.Data.Assembly.Parts) part.PartScript.DesignerInteractionMode = PartDesignerInteractionMode.Normal;
            Traverse.Create(designer.DesignerPlatform).Field<MeshRenderer>("_platformRenderer").Value.enabled = true;
            designer.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            viewMode = ViewModes.Normal;
        }

        internal static void ToggleXray()
        {
            if (viewMode == ViewModes.Normal) SetXrayView();
            else if (viewMode == ViewModes.Xray) SetNormalView();
        }

        public static void SetXrayView()
        {
            IsolateParts(new List<string>());
            HideDesignerPlatform();
            viewMode = ViewModes.Xray;
        }

        public static void RefreshViewMode()
        {
            switch (viewMode)
            {
                case ViewModes.Normal:
                    SetNormalView();
                    break;
                case ViewModes.Xray:
                    SetXrayView();
                    break;
            }
        }
    }
}