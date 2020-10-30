/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEditor;

namespace Immersal
{
    [CustomEditor(typeof(ImmersalSDK))]
    public class ImmersalSDKInfo : Editor
    {
        private ImmersalSDK sdk
        {
            get { return target as ImmersalSDK; }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Immersal SDK v" + ImmersalSDK.sdkVersion, MessageType.Info);
            base.OnInspectorGUI();
        }
    }
}