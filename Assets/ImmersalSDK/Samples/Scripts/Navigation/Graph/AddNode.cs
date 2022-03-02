/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Immersal.AR;

namespace Immersal.Samples.Navigation
{
    [RequireComponent(typeof(Button))]
    public class AddNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private GameObject waypointObject = null;
        [SerializeField]
        private GameObject targetObject = null;
        [SerializeField]
        private Material overrideMaterial = null;

        private enum NodeToAdd
        {
            Waypoint, Target
        };
        [SerializeField]
        private NodeToAdd m_NodeToAdd = NodeToAdd.Waypoint;

        private Button button = null;
        private bool isPressed = false;

        private GameObject nodeInstance = null;

        private Camera mainCamera = null;

        private Vector3 pos = Vector3.zero;
        private Quaternion rot = Quaternion.identity;
        private Quaternion randomRotation = Quaternion.identity;

        private ARSpace arspace = null;

        void Start()
        {
            button = GetComponent<Button>();
            mainCamera = Camera.main;
            arspace = FindObjectOfType<ARSpace>();
        }

        void Update()
        {
            if (isPressed)
            {
                if (nodeInstance != null)
                {
                    pos = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;
                    Vector3 x = Vector3.Cross(Vector3.up, mainCamera.transform.forward);
                    Vector3 z = Vector3.Cross(x, Vector3.up);

                    rot = Quaternion.LookRotation(z, Vector3.up) * randomRotation;

                    nodeInstance.transform.position = pos;
                    nodeInstance.transform.rotation = rot;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode)
            {
                isPressed = true;

                if (nodeInstance == null && waypointObject != null && targetObject != null)
                {
                    switch (m_NodeToAdd)
                    {
                        case NodeToAdd.Waypoint:
                            nodeInstance = Instantiate(waypointObject);
                            break;
                        case NodeToAdd.Target:
                            nodeInstance = Instantiate(targetObject);
                            break;
                        default:
                            nodeInstance = Instantiate(waypointObject);
                            break;

                    }

                    // REPLACE ALL MATERIALS WHILE PLACING THE WAYPOINT
                    if (overrideMaterial != null)
                    {
                        foreach (MeshRenderer rend in nodeInstance.GetComponentsInChildren<MeshRenderer>())
                        {
                            if (rend != null)
                            {
                                Material[] mats = new Material[rend.materials.Length];

                                for (int i = 0; i < mats.Length; i++)
                                {
                                    mats[i] = overrideMaterial;
                                }

                                rend.materials = mats;
                            }
                        }
                    }
                }
                randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;

            if (Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode)
            {
                if (nodeInstance != null)
                {
                    Destroy(nodeInstance);
                }

                // ACTUALLY PLACE THE WAYPOINT
                if (arspace != null)
                {
                    GameObject nodeInstance;

                    switch (m_NodeToAdd)
                    {
                        case NodeToAdd.Waypoint:
                            nodeInstance = Instantiate(waypointObject, arspace.transform);
                            break;
                        case NodeToAdd.Target:
                            nodeInstance = Instantiate(targetObject, arspace.transform);
                            break;
                        default:
                            nodeInstance = Instantiate(waypointObject, arspace.transform);
                            break;

                    }

                    nodeInstance.transform.position = pos;
                    nodeInstance.transform.rotation = rot;
                }
            }
        }
    }
}