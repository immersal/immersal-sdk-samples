/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Immersal.AR;

namespace Immersal.Samples.ContentPlacement
{
    public class PlaceOnAPlane : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> m_objects = new List<GameObject>();
        [SerializeField]
        private ARSpace m_ARSpace = null;

        public void Place(int index)
        {
            Transform cam = Camera.main?.transform;
            m_ARSpace = FindObjectOfType<ARSpace>();

            if (cam != null && m_objects[index] != null && m_ARSpace != null)
            {
                RaycastHit hit;
                Vector3 direction = cam.forward;
                Vector3 origin = cam.position;

                if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity))
                {
                    if (hit.collider != null)
                    {
                        ARPlane arPlane = hit.collider.GetComponentInParent<ARPlane>();
                        if (arPlane)
                        {
                            Vector3 pos = hit.point;
                            GameObject go = Instantiate(m_objects[index], m_ARSpace.transform);
                            go.transform.localRotation = Quaternion.identity;
                            go.transform.position = pos;
                        }
                    }
                }
            }
        }
    }
}
