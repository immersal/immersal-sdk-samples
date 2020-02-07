/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections.Generic;
using Immersal.Samples.Mapping;

namespace Immersal.Samples.Navigation
{
    public class EditNode : MonoBehaviour
    {
        private Node m_firstNode = null;
        private Node m_secondNode = null;

        [SerializeField]
        private float m_ClickHoldTime = 1f;
        private float m_timeHold = 0f;

        private Transform m_CameraTransform;
        private float m_MovePlaneDistance;

        private bool m_MovingNode = false;

        private void Start()
        {
            m_CameraTransform = Camera.main.transform;
        }
        private void Update()
        {
            if (m_MovingNode)
            {
                Vector3 projection = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, m_MovePlaneDistance));
                transform.position = projection;
            }
        }

        private void Reset()
        {
            m_firstNode = null;
            m_secondNode = null;

            m_timeHold = 0f;
            m_MovingNode = false;
        }

        private void OnMouseDown()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    Node node = hit.collider.GetComponent<Node>();
                    if (node)
                    {
                        m_firstNode = node;
                        //Debug.Log(m_firstNode.name);
                    }
                }
            }
        }

        private void OnMouseDrag()
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    Node node = hit.collider.GetComponent<Node>();
                    if (node == m_firstNode)
                    {
                        m_timeHold += Time.deltaTime;
                    }
                    else
                    {
                        m_timeHold = 0f;
                    }
                }
            }

            if (m_timeHold >= m_ClickHoldTime && !m_MovingNode)
            {
                m_MovePlaneDistance = Vector3.Dot(transform.position - m_CameraTransform.position, m_CameraTransform.forward) / m_CameraTransform.forward.sqrMagnitude;
                m_MovingNode = true;
            }
        }

        private void OnMouseUp()
        {
            if (m_firstNode != null)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null)
                    {
                        Node node = hit.collider.GetComponent<Node>();
                        if (node && node != m_firstNode)
                        {
                            m_secondNode = node;
                            //Debug.Log(m_secondNode.name);

                            m_firstNode.neighbours.Add(m_secondNode);
                            NotificationManager.Instance.GenerateSuccess("Connected vertices!");
                        }
                    }
                }
                Reset();
            }
        }
    }
}