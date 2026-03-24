using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace SuperScrollView
{
    public partial class LoopStaggeredGridViewEditor
    {
        SerializedProperty u_MaxClickCount;
        GUIContent u_MaxClickCountContent = new GUIContent("最大可点击数");

        SerializedProperty u_AutoCancelLast;
        GUIContent u_AutoCancelLastContent = new GUIContent("自动取消上一个选择");

        SerializedProperty u_RepetitionCancel;
        GUIContent u_RepetitionCancelContent = new GUIContent("重复点击则取消");

        partial void OnEnableExtend()
        {
            u_MaxClickCount = serializedObject.FindProperty("u_MaxClickCount");
            u_AutoCancelLast = serializedObject.FindProperty("u_AutoCancelLast");
            u_RepetitionCancel = serializedObject.FindProperty("u_RepetitionCancel");
        }

        partial void OnInspectorGUIExtend(LoopStaggeredGridView listView)
        {
            EditorGUILayout.PropertyField(u_MaxClickCount, u_MaxClickCountContent);
            EditorGUILayout.PropertyField(u_AutoCancelLast, u_AutoCancelLastContent);
            EditorGUILayout.PropertyField(u_RepetitionCancel, u_RepetitionCancelContent);
        }
    }
}