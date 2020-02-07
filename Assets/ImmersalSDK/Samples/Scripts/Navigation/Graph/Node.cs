/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples.Navigation
{
    public class Node : MonoBehaviour
    {
        public List<Node> neighbours = new List<Node>();
        [HideInInspector]
        public Vector3 position;
        [HideInInspector]
        public string nodeName;
        public bool includeInSave = true;

        public float FCost { get { return GCost + HCost; } }
        public float HCost { get; set; }
        public float GCost { get; set; }
        public float Cost { get; set; }
        public Node Parent { get; set; }

        [SerializeField]
        private bool drawDebug = false;
        private NavigationManager m_NavigationManager = null;
        private NavigationGraph m_NavigationGraph = null;
        private MeshRenderer m_MeshRenderer = null;

        private void Start()
        {
            m_NavigationManager = FindObjectOfType<NavigationManager>();
            m_NavigationGraph = m_NavigationManager.GetComponent<NavigationGraph>();
            m_MeshRenderer = gameObject.GetComponent<MeshRenderer>();

            position = transform.position;
            nodeName = gameObject.name;

            if (m_NavigationGraph != null)
            {
                m_NavigationGraph.AddNode(this);
            }
        }
        private void Update()
        {
            position = transform.position;
            if(m_MeshRenderer)
                m_MeshRenderer.enabled = m_NavigationManager.inEditMode;
        }

        private void OnDestroy()
        {
            if (m_NavigationGraph != null)
            {
                m_NavigationGraph.RemoveNode(this);
            }
        }

        private void OnDrawGizmos()
        {
            if (drawDebug)
            {
                Gizmos.color = Color.blue;
                foreach (Node node in neighbours)
                {
                    if(node != null)
                        Gizmos.DrawLine(node.transform.position, transform.position);
                }
            }
        }
    }
}