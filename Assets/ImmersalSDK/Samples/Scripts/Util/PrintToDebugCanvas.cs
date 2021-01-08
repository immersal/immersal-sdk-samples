/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;

namespace Immersal.Samples.Util
{
    public class PrintToDebugCanvas : MonoBehaviour
    {
        public enum Field { Top, Middle, Bottom };
        public Field field = Field.Top;

        void Update()
        {
            string text = transform.name + "\n" + "pos: " + transform.position.ToString() + "\n" + "rot: " + transform.rotation.eulerAngles.ToString();

            switch (field)
            {
                case Field.Top:
                    DebugCanvas.Instance.field0.text = text;
                    break;
                case Field.Middle:
                    DebugCanvas.Instance.field1.text = text;
                    break;
                case Field.Bottom:
                    DebugCanvas.Instance.field2.text = text;
                    break;
                default:
                    DebugCanvas.Instance.field0.text = text;
                    break;
            }
        }
    }
}
