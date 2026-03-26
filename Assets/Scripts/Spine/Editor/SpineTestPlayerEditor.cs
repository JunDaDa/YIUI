using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpineTestPlayer))]
public class SpineTestPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpineTestPlayer player = (SpineTestPlayer)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("快速播放", EditorStyles.boldLabel);

        // 运行时读取动画名称列表，显示为按钮
        SerializedProperty animNames = serializedObject.FindProperty("m_AnimationNames");
        if (animNames != null && animNames.isArray && animNames.arraySize > 0)
        {
            // 每行 3 个按钮
            int columns = 3;
            for (int i = 0; i < animNames.arraySize; i++)
            {
                if (i % columns == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                string animName = animNames.GetArrayElementAtIndex(i).stringValue;
                bool isCurrent = player.CurrentAnimation == animName;

                GUI.backgroundColor = isCurrent ? Color.cyan : Color.white;
                if (GUILayout.Button(animName, GUILayout.Height(28)))
                {
                    player.CurrentAnimation = animName;
                    EditorUtility.SetDirty(player);
                }

                if (i % columns == columns - 1 || i == animNames.arraySize - 1)
                {
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUI.backgroundColor = Color.white;
        }
        else if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后显示动画按钮", MessageType.Info);
        }
    }
}
