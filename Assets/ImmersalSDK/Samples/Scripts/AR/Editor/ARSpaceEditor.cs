/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.2.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEditor;

namespace Immersal.AR
{
    [CustomEditor(typeof(ARSpace))]
    public class ARSpaceEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Place AR Maps and all content under this object", MessageType.Info);
            DrawDefaultInspector();
        }
    }
}
