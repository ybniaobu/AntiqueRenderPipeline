using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using YPipeline;

namespace YPipeline.Editor
{
    using CurveState = InspectorCurveEditor.CurveState;
    
    [CustomEditor(typeof(GlobalColorCorrections))]
    public class GlobalColorCorrectionsEditor : VolumeComponentEditor
    {
        private SerializedDataParameter m_Temperature;
        private SerializedDataParameter m_Tint;
        private SerializedDataParameter m_ColorFilter;
        private SerializedDataParameter m_Hue;
        private SerializedDataParameter m_Exposure;
        private SerializedDataParameter m_Saturation;
        private SerializedDataParameter m_Contrast;
        
        // Color Curve
        private SerializedDataParameter m_Master;
        private SerializedDataParameter m_Red;
        private SerializedDataParameter m_Green;
        private SerializedDataParameter m_Blue;

        private SerializedDataParameter m_HueVsHue;
        private SerializedDataParameter m_HueVsSat;
        private SerializedDataParameter m_SatVsSat;
        private SerializedDataParameter m_LumVsSat;

        // Internal references to the actual animation curves
        // Needed for the curve editor
        private SerializedProperty m_RawMaster;
        private SerializedProperty m_RawRed;
        private SerializedProperty m_RawGreen;
        private SerializedProperty m_RawBlue;

        private SerializedProperty m_RawHueVsHue;
        private SerializedProperty m_RawHueVsSat;
        private SerializedProperty m_RawSatVsSat;
        private SerializedProperty m_RawLumVsSat;

        private SerializedProperty m_SelectedCurve;

        private InspectorCurveEditor m_CurveEditor;
        private Dictionary<SerializedProperty, Color> m_CurveDict;
        private static Material s_MaterialGrid;

        private static GUIStyle s_PreLabel;

        private static string[] s_CurveNames =
        {
            "Master",
            "Red",
            "Green",
            "Blue",
            "Hue Vs Hue",
            "Hue Vs Sat",
            "Sat Vs Sat",
            "Lum Vs Sat"
        };

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalColorCorrections>(serializedObject);

            m_Temperature = Unpack(o.Find(x => x.temperature));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_ColorFilter = Unpack(o.Find(x => x.colorFilter));
            m_Hue = Unpack(o.Find(x => x.hue));
            m_Exposure = Unpack(o.Find(x => x.exposure));
            m_Saturation = Unpack(o.Find(x => x.saturation));
            m_Contrast = Unpack(o.Find(x => x.contrast));
            
            // Color Curve
            m_Master = Unpack(o.Find(x => x.master));
            m_Red = Unpack(o.Find(x => x.red));
            m_Green = Unpack(o.Find(x => x.green));
            m_Blue = Unpack(o.Find(x => x.blue));

            m_HueVsHue = Unpack(o.Find(x => x.hueVsHue));
            m_HueVsSat = Unpack(o.Find(x => x.hueVsSat));
            m_SatVsSat = Unpack(o.Find(x => x.satVsSat));
            m_LumVsSat = Unpack(o.Find(x => x.lumVsSat));

            m_RawMaster = o.Find("master.m_Value.m_Curve");
            m_RawRed = o.Find("red.m_Value.m_Curve");
            m_RawGreen = o.Find("green.m_Value.m_Curve");
            m_RawBlue = o.Find("blue.m_Value.m_Curve");

            m_RawHueVsHue = o.Find("hueVsHue.m_Value.m_Curve");
            m_RawHueVsSat = o.Find("hueVsSat.m_Value.m_Curve");
            m_RawSatVsSat = o.Find("satVsSat.m_Value.m_Curve");
            m_RawLumVsSat = o.Find("lumVsSat.m_Value.m_Curve");

            m_SelectedCurve = o.Find("m_SelectedCurve");

            // Prepare the curve editor
            m_CurveEditor = new InspectorCurveEditor();
            m_CurveDict = new Dictionary<SerializedProperty, Color>();

            SetupCurve(m_RawMaster, new Color(1f, 1f, 1f), 2, false);
            SetupCurve(m_RawRed, new Color(1f, 0f, 0f), 2, false);
            SetupCurve(m_RawGreen, new Color(0f, 1f, 0f), 2, false);
            SetupCurve(m_RawBlue, new Color(0f, 0.5f, 1f), 2, false);
            SetupCurve(m_RawHueVsHue, new Color(1f, 1f, 1f), 0, true);
            SetupCurve(m_RawHueVsSat, new Color(1f, 1f, 1f), 0, true);
            SetupCurve(m_RawSatVsSat, new Color(1f, 1f, 1f), 0, false);
            SetupCurve(m_RawLumVsSat, new Color(1f, 1f, 1f), 0, false);
        }
        
        void SetupCurve(SerializedProperty prop, Color color, uint minPointCount, bool loop)
        {
            var state = CurveState.defaultState;
            state.color = color;
            state.visible = false;
            state.minPointCount = minPointCount;
            state.onlyShowHandlesOnSelection = true;
            state.zeroKeyConstantValue = 0.5f;
            state.loopInBounds = loop;
            m_CurveEditor.Add(prop, state);
            m_CurveDict.Add(prop, color);
        }

        void ResetVisibleCurves()
        {
            foreach (var curve in m_CurveDict)
            {
                var state = m_CurveEditor.GetCurveState(curve.Key);
                state.visible = false;
                m_CurveEditor.SetCurveState(curve.Key, state);
            }
        }

        void SetCurveVisible(SerializedProperty rawProp, SerializedProperty overrideProp)
        {
            var state = m_CurveEditor.GetCurveState(rawProp);
            state.visible = true;
            state.editable = overrideProp.boolValue;
            m_CurveEditor.SetCurveState(rawProp, state);
        }

        void CurveOverrideToggle(SerializedProperty overrideProp)
        {
            overrideProp.boolValue = GUILayout.Toggle(overrideProp.boolValue, EditorGUIUtility.TrTextContent("Override"), EditorStyles.toolbarButton);
        }

        string MakeCurveSelectionPopupLabel(int id)
        {
            string label = s_CurveNames[id];
            const string overrideSuffix = " (Overriding)";
            switch (id)
            {
                case 0: if (m_Master.overrideState.boolValue) label += overrideSuffix; break;
                case 1: if (m_Red.overrideState.boolValue) label += overrideSuffix; break;
                case 2: if (m_Green.overrideState.boolValue) label += overrideSuffix; break;
                case 3: if (m_Blue.overrideState.boolValue) label += overrideSuffix; break;
                case 4: if (m_HueVsHue.overrideState.boolValue) label += overrideSuffix; break;
                case 5: if (m_HueVsSat.overrideState.boolValue) label += overrideSuffix; break;
                case 6: if (m_SatVsSat.overrideState.boolValue) label += overrideSuffix; break;
                case 7: if (m_LumVsSat.overrideState.boolValue) label += overrideSuffix; break;
            }
            return label;
        }

        int DoCurveSelectionPopup(int id)
        {
            var label = MakeCurveSelectionPopupLabel(id);
            GUILayout.Label(label, EditorStyles.toolbarPopup, GUILayout.MaxWidth(150f));

            var lastRect = GUILayoutUtility.GetLastRect();
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && lastRect.Contains(e.mousePosition))
            {
                var menu = new GenericMenu();

                for (int i = 0; i < s_CurveNames.Length; i++)
                {
                    if (i == 4)
                        menu.AddSeparator("");

                    int current = i; // Capture local for closure

                    var menuLabel = MakeCurveSelectionPopupLabel(i);
                    menu.AddItem(new GUIContent(menuLabel), current == id, () =>
                    {
                        m_SelectedCurve.intValue = current;
                        serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.DropDown(new Rect(lastRect.xMin, lastRect.yMax, 1f, 1f));
            }

            return id;
        }

        void DrawBackgroundTexture(Rect rect, int pass)
        {
            if (s_MaterialGrid == null)
                s_MaterialGrid = new Material(Shader.Find("Hidden/Editor/CurveBackground")) { hideFlags = HideFlags.HideAndDontSave };

            float scale = EditorGUIUtility.pixelsPerPoint;

            var oldRt = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(Mathf.CeilToInt(rect.width * scale), Mathf.CeilToInt(rect.height * scale), 0, GraphicsFormat.R8G8B8A8_SRGB);
            s_MaterialGrid.SetFloat("_DisabledState", GUI.enabled ? 1f : 0.5f);

            Graphics.Blit(null, rt, s_MaterialGrid, pass);
            RenderTexture.active = oldRt;

            GUI.DrawTexture(rect, rt);
            RenderTexture.ReleaseTemporary(rt);
        }

        void MarkTextureCurveAsDirty(int curveId)
        {
            var t = target as GlobalColorCorrections;

            if (t == null)
                return;

            switch (curveId)
            {
                case 0: t.master.value.SetDirty(); break;
                case 1: t.red.value.SetDirty(); break;
                case 2: t.green.value.SetDirty(); break;
                case 3: t.blue.value.SetDirty(); break;
                case 4: t.hueVsHue.value.SetDirty(); break;
                case 5: t.hueVsSat.value.SetDirty(); break;
                case 6: t.satVsSat.value.SetDirty(); break;
                case 7: t.lumVsSat.value.SetDirty(); break;
            }
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Temperature);
            PropertyField(m_Tint);
            PropertyField(m_ColorFilter);
            PropertyField(m_Hue);
            PropertyField(m_Exposure);
            PropertyField(m_Saturation);
            PropertyField(m_Contrast);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Color Curves", EditorStyles.boldLabel);
            ResetVisibleCurves();

            using (new EditorGUI.DisabledGroupScope(serializedObject.isEditingMultipleObjects))
            {
                int curveEditingId;
                SerializedProperty currentCurveRawProp = null;

                // Top toolbar
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    curveEditingId = DoCurveSelectionPopup(m_SelectedCurve.intValue);
                    curveEditingId = Mathf.Clamp(curveEditingId, 0, 7);

                    EditorGUILayout.Space();

                    switch (curveEditingId)
                    {
                        case 0:
                            CurveOverrideToggle(m_Master.overrideState);
                            SetCurveVisible(m_RawMaster, m_Master.overrideState);
                            currentCurveRawProp = m_RawMaster;
                            break;
                        case 1:
                            CurveOverrideToggle(m_Red.overrideState);
                            SetCurveVisible(m_RawRed, m_Red.overrideState);
                            currentCurveRawProp = m_RawRed;
                            break;
                        case 2:
                            CurveOverrideToggle(m_Green.overrideState);
                            SetCurveVisible(m_RawGreen, m_Green.overrideState);
                            currentCurveRawProp = m_RawGreen;
                            break;
                        case 3:
                            CurveOverrideToggle(m_Blue.overrideState);
                            SetCurveVisible(m_RawBlue, m_Blue.overrideState);
                            currentCurveRawProp = m_RawBlue;
                            break;
                        case 4:
                            CurveOverrideToggle(m_HueVsHue.overrideState);
                            SetCurveVisible(m_RawHueVsHue, m_HueVsHue.overrideState);
                            currentCurveRawProp = m_RawHueVsHue;
                            break;
                        case 5:
                            CurveOverrideToggle(m_HueVsSat.overrideState);
                            SetCurveVisible(m_RawHueVsSat, m_HueVsSat.overrideState);
                            currentCurveRawProp = m_RawHueVsSat;
                            break;
                        case 6:
                            CurveOverrideToggle(m_SatVsSat.overrideState);
                            SetCurveVisible(m_RawSatVsSat, m_SatVsSat.overrideState);
                            currentCurveRawProp = m_RawSatVsSat;
                            break;
                        case 7:
                            CurveOverrideToggle(m_LumVsSat.overrideState);
                            SetCurveVisible(m_RawLumVsSat, m_LumVsSat.overrideState);
                            currentCurveRawProp = m_RawLumVsSat;
                            break;
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
                    {
                        MarkTextureCurveAsDirty(curveEditingId);

                        switch (curveEditingId)
                        {
                            case 0: m_RawMaster.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f); break;
                            case 1: m_RawRed.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f); break;
                            case 2: m_RawGreen.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f); break;
                            case 3: m_RawBlue.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f); break;
                            case 4: m_RawHueVsHue.animationCurveValue = new AnimationCurve(); break;
                            case 5: m_RawHueVsSat.animationCurveValue = new AnimationCurve(); break;
                            case 6: m_RawSatVsSat.animationCurveValue = new AnimationCurve(); break;
                            case 7: m_RawLumVsSat.animationCurveValue = new AnimationCurve(); break;
                        }
                    }

                    m_SelectedCurve.intValue = curveEditingId;
                }

                // Curve area
                var rect = GUILayoutUtility.GetAspectRect(2f);
                var innerRect = new RectOffset(10, 10, 10, 10).Remove(rect);

                if (Event.current.type == EventType.Repaint)
                {
                    // Background
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

                    if (curveEditingId == 4 || curveEditingId == 5)
                        DrawBackgroundTexture(innerRect, 0);
                    else if (curveEditingId == 6 || curveEditingId == 7)
                        DrawBackgroundTexture(innerRect, 1);

                    // Bounds
                    Handles.color = Color.white * (GUI.enabled ? 1f : 0.5f);
                    Handles.DrawSolidRectangleWithOutline(innerRect, Color.clear, new Color(0.8f, 0.8f, 0.8f, 0.5f));

                    // Grid setup
                    Handles.color = new Color(1f, 1f, 1f, 0.05f);
                    int hLines = (int)Mathf.Sqrt(innerRect.width);
                    int vLines = (int)(hLines / (innerRect.width / innerRect.height));

                    // Vertical grid
                    int gridOffset = Mathf.FloorToInt(innerRect.width / hLines);
                    int gridPadding = ((int)(innerRect.width) % hLines) / 2;

                    for (int i = 1; i < hLines; i++)
                    {
                        var offset = gridOffset * i * Vector2.right;
                        offset.x += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.x, innerRect.yMax - 1) + offset);
                    }

                    // Horizontal grid
                    gridOffset = Mathf.FloorToInt(innerRect.height / vLines);
                    gridPadding = ((int)(innerRect.height) % vLines) / 2;

                    for (int i = 1; i < vLines; i++)
                    {
                        var offset = gridOffset * i * Vector2.up;
                        offset.y += gridPadding;
                        Handles.DrawLine(innerRect.position + offset, new Vector2(innerRect.xMax - 1, innerRect.y) + offset);
                    }
                }

                // Curve editor
                using (new GUI.ClipScope(innerRect))
                {
                    if (m_CurveEditor.OnGUI(new Rect(0, 0, innerRect.width - 1, innerRect.height - 1)))
                    {
                        Repaint();
                        GUI.changed = true;
                        MarkTextureCurveAsDirty(m_SelectedCurve.intValue);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    // Borders
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 20f), new Vector2(rect.xMax, rect.y - 20f));
                    Handles.DrawLine(new Vector2(rect.x, rect.y - 21f), new Vector2(rect.x, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
                    Handles.DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.y - 20f));

                    bool editable = m_CurveEditor.GetCurveState(currentCurveRawProp).editable;
                    string editableString = editable ? string.Empty : "(Not Overriding)\n";

                    // Selection info
                    var selection = m_CurveEditor.GetSelection();
                    var infoRect = innerRect;
                    infoRect.x += 5f;
                    infoRect.width = 100f;
                    infoRect.height = 30f;

                    if (s_PreLabel == null)
                        s_PreLabel = new GUIStyle("ShurikenLabel");

                    if (selection.curve != null && selection.keyframeIndex > -1)
                    {
                        var key = selection.keyframe.Value;
                        GUI.Label(infoRect, $"{key.time:F3}\n{key.value:F3}", s_PreLabel);
                    }
                    else
                    {
                        GUI.Label(infoRect, editableString, s_PreLabel);
                    }
                }
            }
        }
    }
}