    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Craft.Parts;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Design.Tools;
    using Assets.Scripts.Design;
    using Assets.Scripts.DevConsole;
    using Assets.Scripts.Input;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.Ui.Settings;
    using Assets.Scripts.Ui;
    using CodeStage.AdvancedFPSCounter;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Craft;
    using ModApi.Design;
    using ModApi.Input.Events;
    using ModApi.Input;
    using ModApi.Math;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi;
    using TMPro;
    using UI.Xml;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UnityEngine;

    namespace Assets.Scripts.DesignerTools {
        public class PartStateChange {
            public String Type;

            public String TransformType;
            public Vector3 PositionDelta = new Vector3 ();
            private Vector3 _PositionA = new Vector3 ();
            private Vector3 _PositionB = new Vector3 ();
            public float RotationAngle = 0f;
            private Quaternion _RotationA = new Quaternion ();
            private Quaternion _RotationB = new Quaternion ();

            public String PropertyType;
            public String Modifier;
            public String Property;
            public String SValue;
            public float FValue;

            public PartStateChange (Vector3 posA, Vector3 PosB) {
                Type = "Transform";
                TransformType = "Position";
                _PositionA = posA;
                _PositionB = PosB;

                PositionDelta = PosB - posA;
            }

            public PartStateChange (Quaternion rotA, Quaternion rotB) {
                Type = "Transform";
                TransformType = "Rotation";
                _RotationA = rotA;
                _RotationB = rotB;

                RotationAngle = Quaternion.Angle (rotA, rotB);
            }

            public PartStateChange (String modifier, String property, String Value) {
                Type = "Modifier";
                PropertyType = "String";
                Modifier = modifier;
                Property = property;
                SValue = Value;
            }

            public PartStateChange (String modifier, String property, float Value) {
                Type = "Modifier";
                PropertyType = "Float";
                Modifier = modifier;
                Property = property;
                FValue = Value;
            }
        }
    }