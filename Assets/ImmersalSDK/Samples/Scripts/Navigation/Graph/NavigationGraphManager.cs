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

namespace Immersal.Samples.Navigation
{
    public class Node
    {
        public Vector3 position;
        public List<Node> neighbours = new List<Node>();
        public string nodeName;

        public Node(Vector3 position)
        {
            this.position = position;
        }

        public Node()
        {

        }

        public Node(Vector3 position, List<Node> neighbours)
        {
            this.position = position;
            this.neighbours = neighbours;
        }

        public float Cost
        {
            get; set;
        }

        public Node Parent
        {
            get; set;
        }
    }

    public class NavigationGraphManager : MonoBehaviour
    {
        public class LineSegment
        {
            public Vector3 startPosition;
            public Vector3 endPosition;

            public LineSegment(Vector3 start, Vector3 end)
            {
                this.startPosition = start;
                this.endPosition = end;
            }
        }

        public class Edge
        {
            public Node start;
            public Node end;

            public Edge(Node start, Node end)
            {
                this.start = start;
                this.end = end;
            }
        }

        [SerializeField]
        private float startYOffset = -0.25f;

        private List<Waypoint> m_Waypoints = new List<Waypoint>();
        private List<IsNavigationTarget> m_NavigationTargets = new List<IsNavigationTarget>();

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;

        private Immersal.AR.ARSpace m_ArSpace = null;

        #region singleton pattern
        private static NavigationGraphManager instance = null;
        public static NavigationGraphManager Instance
        {
            get
            {
#if UNITY_EDITOR
                if (instance == null && !Application.isPlaying)
                {
                    instance = FindObjectOfType<NavigationGraphManager>();
                }
#endif
                if (instance == null)
                {
                    Debug.LogError("No NavigationGraphManager instance found. Ensure one exists in the scene.");
                }
                return instance;
            }
        }
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            if (instance != this)
            {
                Debug.LogError("There must be only one NavigationGraphManager in a scene.");
                UnityEngine.Object.DestroyImmediate(this);
                return;
            }
        }
        #endregion

        void Start()
        {
            InitializeMeshRenderer();

            m_ArSpace = FindObjectOfType<Immersal.AR.ARSpace>();
        }

        void Update()
        {
            if (Immersal.Samples.Navigation.NavigationManager.Instance.inEditMode)
            {
                m_MeshRenderer.enabled = true;
                DrawConnections(0.075f, m_Mesh);
            }
            else
            {
                m_MeshRenderer.enabled = false;
            }
        }

        private void InitializeMeshRenderer()
        {
            if (m_Mesh == null)
                m_Mesh = new Mesh();

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            if (m_MeshFilter == null)
                m_MeshFilter = gameObject.AddComponent<MeshFilter>();

            if (m_MeshRenderer == null)
                m_MeshRenderer = gameObject.AddComponent<MeshRenderer>();

            m_MeshFilter.mesh = m_Mesh;
        }

        private void DrawConnections(float LineSegmentWidth, Mesh mesh)
        {
            List<LineSegment> lineSegments = new List<LineSegment>();

            foreach (Waypoint wp in m_Waypoints)
            {
                foreach (Waypoint neighbour in wp.neighbours)
                {
                    if (neighbour != null)
                    {
                        LineSegment ls = new LineSegment(wp.position, neighbour.position);
                        lineSegments.Add(ls);
                    }
                }
            }

            if (lineSegments.Count < 1)
            {
                mesh.Clear();
                return;
            }

            int segmentCount = lineSegments.Count;
            int offset = 0;

            Vector3[] vertices = new Vector3[segmentCount * 4];
            Vector2[] uvs = new Vector2[segmentCount * 4];
            int[] triangleIndices = new int[segmentCount * 6];

            foreach (LineSegment ls in lineSegments)
            {
                {
                    Vector3 startPosition = transform.worldToLocalMatrix.MultiplyPoint(ls.startPosition);
                    Vector3 endPosition = transform.worldToLocalMatrix.MultiplyPoint(ls.endPosition);

                    Vector3 billboardUp = Camera.main.transform.forward;
                    //billboardUp = new Vector3(billboardUp.x, 0f, billboardUp.z);

                    Quaternion edgeOrientation = Quaternion.LookRotation(endPosition - startPosition, billboardUp);

                    Matrix4x4 startTransform = Matrix4x4.TRS(startPosition, edgeOrientation, Vector3.one);
                    Matrix4x4 endTransform = Matrix4x4.TRS(endPosition, edgeOrientation, Vector3.one);


                    Vector3[] shape = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };
                    float[] shapeU = new float[] { 0f, 1f };

                    int verticesOffset = offset * 4;
                    int triangleIndicesOffset = offset * 6;

                    vertices[verticesOffset + 0] = startTransform.MultiplyPoint(shape[0] * LineSegmentWidth);
                    vertices[verticesOffset + 1] = startTransform.MultiplyPoint(shape[1] * LineSegmentWidth);
                    vertices[verticesOffset + 2] = endTransform.MultiplyPoint(shape[1] * LineSegmentWidth);
                    vertices[verticesOffset + 3] = endTransform.MultiplyPoint(shape[0] * LineSegmentWidth);

                    uvs[verticesOffset + 0] = new Vector2(0f, 1f);
                    uvs[verticesOffset + 1] = new Vector2(0f, 0f);
                    uvs[verticesOffset + 2] = new Vector2(1f, 0f);
                    uvs[verticesOffset + 3] = new Vector2(1f, 1f);

                    triangleIndices[triangleIndicesOffset + 0] = verticesOffset + 0;
                    triangleIndices[triangleIndicesOffset + 1] = verticesOffset + 1;
                    triangleIndices[triangleIndicesOffset + 2] = verticesOffset + 2;
                    triangleIndices[triangleIndicesOffset + 3] = verticesOffset + 0;
                    triangleIndices[triangleIndicesOffset + 4] = verticesOffset + 2;
                    triangleIndices[triangleIndicesOffset + 5] = verticesOffset + 3;

                    offset++;
                }

                mesh.Clear();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangleIndices;
            }
        }

        public void AddWaypoint(Waypoint wp)
        {
            if (!m_Waypoints.Contains(wp))
            {
                m_Waypoints.Add(wp);
            }
        }

        public void RemoveWaypoint(Waypoint wp)
        {
            if (m_Waypoints.Contains(wp))
            {
                m_Waypoints.Remove(wp);
            }
        }

        public void DeleteAllWaypoints()
        {
            foreach (Waypoint wp in m_Waypoints)
            {
                Destroy(wp.gameObject);
            }

            m_Waypoints.Clear();
        }

        public void AddTarget(IsNavigationTarget target)
        {
            if (!m_NavigationTargets.Contains(target))
            {
                m_NavigationTargets.Add(target);
            }
        }

        public void RemoveTarget(IsNavigationTarget target)
        {
            if (m_NavigationTargets.Contains(target))
            {
                m_NavigationTargets.Remove(target);
            }
        }

        public void DeleteAllNavigationTargets()
        {
            foreach (IsNavigationTarget target in m_NavigationTargets)
            {
                Destroy(target.gameObject);
            }

            m_NavigationTargets.Clear();
        }

        public List<Vector3> FindPath(Vector3 startPosition, Vector3 endPosition)
        {
            List<Vector3> pathPositions = new List<Vector3>();

            if(m_Waypoints.Count < 1)
            {
                return pathPositions;
            }

            //
            // Convert a list of Waypoints to a dictionary of Nodes and neighbours and build the graph.
            //

            Dictionary<Waypoint, Node> link = new Dictionary<Waypoint, Node>();
            Dictionary<Node, List<Node>> graph = new Dictionary<Node, List<Node>>();

            foreach (Waypoint wp in m_Waypoints)
            {
                Node n = new Node(wp.position);
                n.nodeName = wp.name;
                link[wp] = n;
            }

            foreach (Waypoint wp in link.Keys)
            {
                List<Node> nodes = new List<Node>();

                foreach (Waypoint neighbour in wp.neighbours)
                {
                    nodes.Add(link[neighbour]);
                }

                graph[link[wp]] = nodes;
            }

            //
            // Add in and out Nodes for Main Camera and Navigation Target (start position and end position)
            //

            Node startNode = new Node(startPosition + new Vector3(0f, startYOffset, 0f));
            startNode.nodeName = "GRAPH - START NODE";
            Node inNode = new Node();
            inNode.nodeName = "GRAPH - IN NODE";
            EdgeToNearestPointInGraph(ref startNode, ref inNode, ref graph);

            Node endNode = new Node(endPosition);
            endNode.nodeName = "GRAPH - END NODE";
            Node outNode = new Node();
            outNode.nodeName = "GRAPH - OUT NODE";
            EdgeToNearestPointInGraph(ref endNode, ref outNode, ref graph);

            pathPositions = GetPathPositions(startNode, endNode, graph);

            if (pathPositions.Count < 2)
            {
                Debug.LogWarning("NAVIGATION GRAPH MANAGER: Path could not be found");
                return pathPositions;
            }

            return pathPositions;
        }

        private void GetClosestNode(ref Node searchNode, ref Dictionary<Node, List<Node>> graph)
        {
            float min = Mathf.Infinity;
            Node nearestNode = null;

            foreach (Node node in graph.Keys)
            {
                float dist = (node.position - searchNode.position).magnitude;

                if (dist < min)
                {
                    min = dist;
                    nearestNode = node;
                }
            }

            searchNode.neighbours.Add(nearestNode);
            graph[nearestNode].Add(searchNode);
        }

        private void EdgeToNearestPointInGraph(ref Node searchNode, ref Node insertNode, ref Dictionary<Node, List<Node>> graph)
        {
            Vector3 nodePos = Vector3.zero;
            float min = Mathf.Infinity;

            List<Edge> edges = new List<Edge>();
            foreach (KeyValuePair<Node, List<Node>> k in graph)
            {
                foreach (Node neighbour in k.Value)
                {
                    Edge edge = new Edge(k.Key, neighbour);
                    edges.Add(edge);
                }
            }

            Edge nearestEdge = null;
            foreach (Edge edge in edges)
            {
                Vector3 pos;
                Vector3 p = searchNode.position;
                Vector3 a = edge.start.position;
                Vector3 b = edge.end.position;

                Vector3 ap = new Vector3(p.x - a.x, p.y - a.y, p.z - a.z);
                Vector3 ab = new Vector3(b.x - a.x, b.y - a.y, b.z - a.z);

                float d2 = Vector3.SqrMagnitude(ab);
                float t = ((p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y) + (p.z - a.z) * (b.z - a.z)) / d2;

                if (t < 0f || t > 1f)
                {
                    if (Vector3.SqrMagnitude(a - p) < Vector3.SqrMagnitude(b - p))
                    {
                        pos = a;
                    }
                    else
                    {
                        pos = b;
                    }
                }
                else
                {
                    pos = a + Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab) * ab;
                }

                float currentDistance = Vector3.SqrMagnitude(pos - p);
                if (currentDistance < min)
                {
                    min = currentDistance;
                    nodePos = pos;
                    nearestEdge = edge;
                }
            }

            insertNode.position = nodePos;

            insertNode.neighbours.Add(nearestEdge.start);
            insertNode.neighbours.Add(nearestEdge.end);

            graph[nearestEdge.start].Add(insertNode);
            graph[nearestEdge.end].Add(insertNode);

            graph[nearestEdge.start].Remove(nearestEdge.end);
            graph[nearestEdge.end].Remove(nearestEdge.start);

            searchNode.neighbours.Add(insertNode);
            insertNode.neighbours.Add(searchNode);

            graph[insertNode] = insertNode.neighbours;
            graph[searchNode] = searchNode.neighbours;
        }

        public List<Vector3> GetPathPositions(Node startNode, Node endNode, Dictionary<Node, List<Node>> graph)
        {
            List<Vector3> pathPositions = new List<Vector3>();
            List<Node> openList = new List<Node>();
            List<Node> closedList = new List<Node>();
            
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                Node currNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].Cost < currNode.Cost)
                    {
                        currNode = openList[i];
                    }
                }

                openList.Remove(currNode);
                closedList.Add(currNode);

                if (currNode == endNode)
                {
                    pathPositions = NodesToPathPositions(startNode, endNode);
                    return pathPositions;
                }

                foreach (Node n in graph[currNode])
                {
                    if (!closedList.Contains(n))
                    {
                        float totalCost = currNode.Cost + n.Cost;
                        if (totalCost < n.Cost || !openList.Contains(n))
                        {
                            n.Cost = totalCost;
                            n.Parent = currNode;

                            if (!openList.Contains(n))
                            {
                                openList.Add(n);
                            }
                        }
                    }
                }
            }

            return pathPositions;
        }

        private List<Vector3> NodesToPathPositions(Node startNode, Node endNode)
        {
            List<Vector3> pathPositions = new List<Vector3>();
            Node currNode = endNode;

            while (currNode != startNode)
            {
                pathPositions.Add(currNode.position);
                currNode = currNode.Parent;
            }
            
            pathPositions.Add(startNode.position);
            pathPositions.Reverse();

            return pathPositions;
        }
    }
}