/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Immersal.AR
{
    [CustomEditor(typeof(ARMap))]
    public class ARMapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ARMap targ = target as ARMap;

            TextAsset map = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Map File", ".bytes file from the Developer Portal"), targ.m_MapFile, typeof(TextAsset), false);
            if (map != targ.m_MapFile)
            {
                targ.m_MapFile = map;
                targ.InitMesh();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Color color = (Color)EditorGUILayout.ColorField(new GUIContent("Color", "point cloud color"), targ.m_Color);
            if (color != targ.m_Color)
            {
                targ.m_Color = color;
                targ.InitMesh();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            ARMap.RenderMode renderMode = (ARMap.RenderMode)EditorGUILayout.EnumPopup(new GUIContent("Render Mode", "when to render the point cloud"), targ.m_RenderMode);
            if (renderMode != targ.m_RenderMode)
            {
                targ.m_RenderMode = renderMode;
                targ.InitMesh();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
