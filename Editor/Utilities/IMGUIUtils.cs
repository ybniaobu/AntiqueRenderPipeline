using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Rendering;

namespace YPipeline.Editor
{
    public static class IMGUIUtils
    {
        public static void DrawTitle(string title)
        {
            Rect labelRect = EditorGUILayout.GetControlRect();
            labelRect.x -= 2;
            labelRect.width += 2;
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            Rect lineRect = new Rect(lastRect.x + 15 * EditorGUI.indentLevel - 2, lastRect.yMax, lastRect.width - 15 * EditorGUI.indentLevel + 2, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
        
        public static void DrawTitle(GUIContent title)
        {
            Rect labelRect = EditorGUILayout.GetControlRect();
            labelRect.x -= 2;
            labelRect.width += 2;
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            Rect lineRect = new Rect(lastRect.x + 15 * EditorGUI.indentLevel - 2, lastRect.yMax, lastRect.width - 15 * EditorGUI.indentLevel + 2, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }
    }
}