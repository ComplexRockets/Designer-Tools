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
            public XName modifer;
            public XName atribute;
            public String oldAttribute;
            public String newAttribute;
            bool isDeltaChange = false;
            Vector3 delta;

            public PartStateChange (XAttribute _oldAtribute, XAttribute _newAtribute, bool allowDeltaChange) {
                modifer = _newAtribute.Parent.Name;
                oldAttribute = _oldAtribute.Value;
                newAttribute = _newAtribute.Value;
                atribute = _newAtribute.Name;

                if (allowDeltaChange && modifer == "Part" && (atribute == "Transform" || atribute == "Rotation")) {
                    isDeltaChange = true;
                    //Delta = Old.Value - New.Value;
                }
            }
        }
    }