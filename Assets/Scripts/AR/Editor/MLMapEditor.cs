/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Immersal.AR
{
    [CustomEditor(typeof(MLMap))]
    public class MLMapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MLMap targ = target as MLMap;

            TextAsset map = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Map File", ".bytes file from the Developer Portal"), targ.mapFile, typeof(TextAsset), false);
            if (map != targ.mapFile)
            {
                targ.FreeMap();
                targ.mapFile = map;
                targ.InitMesh();
                targ.LoadMap();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Color color = (Color)EditorGUILayout.ColorField(new GUIContent("Color", "point cloud color"), targ.color);
            if (color != targ.color)
            {
                targ.color = color;
                targ.InitMesh();
                targ.LoadMap();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            MLMap.MLRenderMode renderMode = (MLMap.MLRenderMode)EditorGUILayout.EnumPopup(new GUIContent("Render Mode", "when to render the point cloud"), targ.MLrenderMode);
            if (renderMode != targ.MLrenderMode)
            {
                targ.MLrenderMode = renderMode;
                targ.renderMode = (ARMap.RenderMode) renderMode;
                targ.InitMesh();
                targ.LoadMap();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
