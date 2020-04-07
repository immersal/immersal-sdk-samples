/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System;

namespace Immersal.Samples.Util
{
	public class FixOrientation : MonoBehaviour {
#if !UNITY_EDITOR
		private DeviceOrientation m_previousOrientation;
		private RectTransform m_rt;
		private float m_rotAngle = 0f;
		private bool m_bDoRotate = false;

		// Use this for initialization
		private void Awake () {
			m_rt = GetComponent<RectTransform>();
		}

        private void Start()
        {
            SetOrientation();
        }

        private void OnEnable()
        {
            SetOrientation();
        }

        void Update () {
			DeviceOrientation orientation = Input.deviceOrientation;

			if (orientation == DeviceOrientation.Unknown)
				return;

			if (orientation != m_previousOrientation)
			{
				m_previousOrientation = orientation;
				m_bDoRotate = true;

				switch (orientation)
				{
					case DeviceOrientation.LandscapeLeft:
						m_rotAngle = -90f;
						break;
					case DeviceOrientation.LandscapeRight:
						m_rotAngle = 90f;
						break;
					default:
						m_rotAngle = 0f;
						break;
				}
			}

			if (!m_bDoRotate)
				return;

			if (Math.Abs(m_rt.localEulerAngles.z - m_rotAngle) < 0.0001f)
			{
				m_rt.localEulerAngles = new Vector3(0, 0, m_rotAngle);
				m_bDoRotate = false;
			}
			else
			{
				Quaternion target = Quaternion.Euler(0, 0, m_rotAngle);
				m_rt.rotation = Quaternion.Slerp(m_rt.rotation, target, Time.deltaTime * 10f);
			}
		}

        void SetOrientation()
        {
            DeviceOrientation orientation = Input.deviceOrientation;

            switch (orientation)
            {
                case DeviceOrientation.LandscapeLeft:
                    m_rotAngle = -90f;
                    break;
                case DeviceOrientation.LandscapeRight:
                    m_rotAngle = 90f;
                    break;
                default:
                    m_rotAngle = 0f;
                    break;
            }

            m_rt.localEulerAngles = new Vector3(0, 0, m_rotAngle);
            m_previousOrientation = orientation;
        }
#endif
	}
}
