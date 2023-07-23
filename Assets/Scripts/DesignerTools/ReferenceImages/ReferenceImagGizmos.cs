using System;
using System.Linq;
using Assets.Scripts.Design;
using Assets.Scripts.Tools.ObjectTransform;
using ModApi.Ui;
using TMPro;
using UI.Xml;
using UnityEngine;
using Assets.Scripts.Ui.Designer;
using UnityEngine.UI;
using Assets.Scripts.Design.Tools;
using ModApi.Input.Events;
using ModApi;

namespace Assets.Scripts.DesignerTools.ReferenceImages
{
    public class ReferenceImageGizmos : MovementTool
    {
        DesignerScript designer;
        public TranslateGizmo translateGizmo;
        public RotateGizmo rotateGizmo;
        public override bool IsBaseTool => false;

        public ReferenceImageGizmos(DesignerScript designer) : base(designer)
        {
            this.designer = designer;

            translateGizmo = new TranslateGizmo();
            translateGizmo.GridSizeFunc = () => Game.Instance.Settings.Game.Designer.GridSize;
            //translateGizmo.IsLocalOrientation = Mod.Instance.imageGizmoIsLocalOrientation;
            translateGizmo.Initialize(designer.GizmoCamera);
            translateGizmo.GizmoAdjusted += OnGizmoAdjusted;

            rotateGizmo = new RotateGizmo();
            rotateGizmo.Initialize(designer.GizmoCamera);
            rotateGizmo.GridSizeFunc = () => Game.Instance.Settings.Game.Designer.GridSize;
            //rotateGizmo.IsLocalOrientation = Mod.Instance.imageGizmoIsLocalOrientation;
            rotateGizmo.Sensitivity = 0.5f;
            rotateGizmo.GizmoAdjusted += OnGizmoAdjusted;
        }

        private void OnGizmoAdjusted(MovementGizmo<RotateGizmoAxisScript> source, bool? newAxis) => RaiseToolAdjustmentOccurred();
        private void OnGizmoAdjusted(MovementGizmo<TranslateGizmoAxisScript> source, bool? newAxis) => RaiseToolAdjustmentOccurred();

        public void OnClose()
        {
            translateGizmo.DestroyGizmos();
            rotateGizmo.DestroyGizmos();
        }

        protected override bool OnMouseDrag(ClickEventArgs e)
        {
            base.OnMouseDrag(e);
            return true;
        }

        protected override bool OnMouseEnd()
        {
            return base.OnMouseEnd();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            translateGizmo.Update();
            rotateGizmo.Update();
        }

        public override bool HandleClick(ClickEventArgs e)
        {
            base.HandleClick(e);
            return translateGizmo.HandleClick(e) || rotateGizmo.HandleClick(e);
        }

        public void NudgeSelection(Utilities.UnityTransform.TransformAxis axis, float distance)
        {
            Vector3 position = base.SelectedTransform.position;
            switch (axis)
            {
                case Utilities.UnityTransform.TransformAxis.X:
                    position += (base.LocalOrientation ? base.SelectedTransform.right : Vector3.right) * distance;
                    break;
                case Utilities.UnityTransform.TransformAxis.Y:
                    position += (base.LocalOrientation ? base.SelectedTransform.up : Vector3.up) * distance;
                    break;
                case Utilities.UnityTransform.TransformAxis.Z:
                    position += (base.LocalOrientation ? base.SelectedTransform.forward : Vector3.forward) * distance;
                    break;
            }
            base.SelectedTransform.position = position;
        }

        public void Rotate(Vector3 eulers, Space space)
        {
            base.SelectedTransform.Rotate(eulers, space);
        }

        public void SetWorldPosition(Vector3 position)
        {
            if (base.SelectedTransform != null) base.SelectedTransform.position = position;
        }

        public void SetWorldRotation(Quaternion quaternion)
        {
            base.SelectedTransform.rotation = quaternion;
        }
    }
}