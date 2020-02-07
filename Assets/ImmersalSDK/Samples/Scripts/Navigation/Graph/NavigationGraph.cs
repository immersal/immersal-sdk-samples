/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.AI;
using Immersal.AR;
using TMPro;

namespace Immersal.Samples.Navigation
{
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

    [System.Serializable]
    public class NodeHierarchy
    {
        public Node parent;
        public List<Node> children;
        public List<string> childrenNames;
        public string parentName;
        public Vector3 parentPosition;
    }

    public class NavigationGraph : MonoBehaviour
    {
        [SerializeField]
        GameObject m_NodePrefab = null;

        private List<Node> m_Nodes = new List<Node>();

        private Node m_InNode = null;
        private Node m_OutNode = null;

        private Mesh m_Mesh;
        private MeshFilter m_MeshFilter;
        private MeshRenderer m_MeshRenderer;

        private ARSpace m_ArSpace = null;

        [System.Serializable]
        public struct Savefile
        {
            public List<NodeHierarchy> nodes;
        }

        public void SaveNodes(List<Node> nodes)
        {
            Savefile savefile = new Savefile();

            List<NodeHierarchy> nodeHierarchies = new List<NodeHierarchy>();
            List<string> names = new List<string>();
            List<Vector3> positions = new List<Vector3>();

            foreach (Node node in nodes)
            {
                if (node.includeInSave)
                {
                    NodeHierarchy nh = new NodeHierarchy();
                    nh.parent = node;
                    nh.children = node.neighbours;

                    List<string> neighbourNames = new List<string>();

                    foreach (Node n in node.neighbours)
                    {
                        if (n != null)
                            neighbourNames.Add(n.nodeName);
                    }

                    nh.childrenNames = neighbourNames;
                    nh.parentName = node.nodeName;
                    nh.parentPosition = node.position;
                    nodeHierarchies.Add(nh);
                    names.Add(node.nodeName);
                    positions.Add(node.position);
                }
            }

            savefile.nodes = nodeHierarchies;

            string jsonstring = JsonUtility.ToJson(savefile, true);
            string dataPath = Path.Combine(Application.persistentDataPath, "nodes.json");
            File.WriteAllText(dataPath, jsonstring);
            Debug.Log(Application.persistentDataPath);
        }

        public void LoadContents()
        {
            string dataPath = Path.Combine(Application.persistentDataPath, "nodes.json");

            try
            {
                Savefile loadFile = JsonUtility.FromJson<Savefile>(File.ReadAllText(dataPath));

                List<Node> newNodes = new List<Node>();

                foreach (NodeHierarchy nh in loadFile.nodes)
                {
                    GameObject go = Instantiate(m_NodePrefab, m_ArSpace.transform);
                    Node n = go.GetComponent<Node>();

                    go.transform.position = nh.parentPosition;
                    n.position = nh.parentPosition;

                    go.name = nh.parentName;
                    n.nodeName = nh.parentName;

                    newNodes.Add(n);
                }

                for(int i=0; i<newNodes.Count; i++)
                {
                    List<Node> c = new List<Node>();

                    foreach(string s in loadFile.nodes[i].childrenNames)
                    {
                        GameObject go = GameObject.Find(s);
                        if (go)
                        {
                            Node n = go.GetComponent<Node>();
                            if(n != null)
                            {
                                c.Add(n);
                            }
                        }
                    }
                    newNodes[i].neighbours = c;
                }
            }
            catch (FileNotFoundException e)
            {
                Debug.LogError(dataPath + " not found\nNo objects loaded: " + e.Message);
            }
        }

        private void Awake()
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

        private void Start()
        {
            if (m_InNode == null)
            {
                GameObject go = new GameObject("graph in node");
                m_InNode = go.AddComponent<Node>();
                m_InNode.neighbours = new List<Node>();
                m_InNode.nodeName = go.name;
                m_InNode.includeInSave = false;
            }

            if (m_OutNode == null)
            {
                GameObject go = new GameObject("graph out node");
                m_OutNode = go.AddComponent<Node>();
                m_OutNode.neighbours = new List<Node>();
                m_OutNode.nodeName = go.name;
                m_OutNode.includeInSave = false;
            }

            if (!NavigationManager.Instance.inEditMode)
            {
                m_MeshRenderer.enabled = false;
            }

            m_ArSpace = FindObjectOfType<ARSpace>();

            LoadContents();
        }

        private void Update()
        {
            if (NavigationManager.Instance.inEditMode)
            {
                m_MeshRenderer.enabled = true;
                GenerateConnectionMeshes();
            }
            else
            {
                m_MeshRenderer.enabled = false;
            }
        }

        public void AddNode(Node node)
        {
            if (!m_Nodes.Contains(node))
            {
                m_Nodes.Add(node);
            }
        }

        public void RemoveNode(Node node)
        {
            if (m_Nodes.Contains(node))
            {
                m_Nodes.Remove(node);
            }
        }

        private Dictionary<Node, List<Node>> BuildGraph(Vector3 startPosition, Vector3 endPosition, List<Node> nodes)
        {
            m_InNode.neighbours = new List<Node>();
            m_OutNode.neighbours = new List<Node>();

            Dictionary<Node, List<Node>> graph = new Dictionary<Node, List<Node>>();

            if (nodes.Count < 2)
                return graph;

            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Node neighbour in node.neighbours)
                {
                    if (neighbour != null)
                    {
                        Edge edge = new Edge(node, neighbour);
                        edges.Add(edge);
                    }
                }
            }

            if (edges.Count == 0)
                return graph;

            List<Node> newNodes = new List<Node>();
            newNodes.AddRange(nodes);

            NodeToClosestEdge(startPosition, edges, m_InNode);
            NodeToClosestEdge(endPosition, edges, m_OutNode);

            newNodes.Add(m_InNode);
            newNodes.Add(m_OutNode);

            List<Edge> undirectionalEdges = new List<Edge>();

            foreach (Node node in newNodes)
            {
                foreach (Node neighbour in node.neighbours)
                {
                    if (neighbour != null)
                    {
                        Edge edge = new Edge(node, neighbour);
                        Edge reverse = new Edge(neighbour, node);
                        undirectionalEdges.Add(edge);
                        undirectionalEdges.Add(reverse);
                    }
                }
            }

            foreach (Edge edge in undirectionalEdges)
            {
                Node key = edge.start;
                Node value = edge.end;

                if (!graph.ContainsKey(key))
                {
                    graph[key] = new List<Node>();
                    graph[key].Add(value);
                }
                if (!graph[key].Contains(value))
                {
                    graph[key].Add(value);
                }
            }

            //DebugGraph(graph);
            return graph;
        }

        private void NodeToClosestEdge(Vector3 searchPosition, List<Edge> edges, Node node)
        {
            Vector3 positionInGraph = new Vector3();
            float shortestDistance = Mathf.Infinity;

            if (node.neighbours != null)
            {
                node.neighbours.Clear();
            }

            Edge closestEdge = null;

            foreach (Edge edge in edges)
            {
                Vector3 pos;
                Vector3 p = searchPosition;
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
                if (currentDistance < shortestDistance)
                {
                    shortestDistance = currentDistance;
                    positionInGraph = pos;
                    closestEdge = edge;
                }
            }

            node.transform.position = positionInGraph;
            node.position = positionInGraph;
            node.neighbours.Add(closestEdge.start);
            node.neighbours.Add(closestEdge.end);
        }

        public void DebugGraph(Dictionary<Node, List<Node>> graph)
        {
            foreach (KeyValuePair<Node, List<Node>> g in graph)
            {
                string debug = "KEY: ";
                debug += g.Key.name + "    VALUES: ";

                foreach (Node v in g.Value)
                {
                    debug += v.name + ", ";
                }

                Debug.Log(debug);
            }
        }

        public List<Vector3> FindPath(Vector3 startPosition, Vector3 endPosition)
        {
            SaveNodes(m_Nodes);
            List<Vector3> pathPositions = new List<Vector3>();

            Dictionary<Node, List<Node>> graph = BuildGraph(startPosition, endPosition, m_Nodes);
            if (graph.Count < 2)
            {
                Debug.LogWarning("Graph could not be built.");
                return pathPositions;
            }

            pathPositions = AStar(m_InNode, m_OutNode, graph);
            if (pathPositions.Count < 2)
            {
                Debug.LogWarning("Path could not be found.");
                return pathPositions;
            }

            pathPositions.Insert(0, startPosition);
            pathPositions.Add(endPosition);

            //flatten path
            List<Vector3> collapsedCorners = new List<Vector3>();
            for (int i = 0; i < pathPositions.Count; i++)
            {
                pathPositions[i] = new Vector3(pathPositions[i].x, startPosition.y - 0.8f, pathPositions[i].z);
            }
            return pathPositions;
        }

        public List<Vector3> AStar(Node startNode, Node endNode, Dictionary<Node, List<Node>> graph)
        {
            List<Vector3> pathPositions = new List<Vector3>();
            List<Node> openSet = new List<Node>();
            openSet.Add(startNode);

            List<Node> closedSet = new List<Node>();

            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost
                        || (openSet[i].FCost.Equals(currentNode.FCost)
                            && openSet[i].HCost < currentNode.HCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == endNode)
                {
                    //Debug.Log("A*: Found path to target");
                    pathPositions = RetracePath(startNode, endNode);
                    return pathPositions;
                }

                foreach (Node connection in graph[currentNode])
                {
                    if (!closedSet.Contains(connection))
                    {
                        float costToConnection = currentNode.GCost + connection.Cost;

                        if (costToConnection < connection.GCost || !openSet.Contains(connection))
                        {
                            connection.GCost = costToConnection;
                            connection.Parent = currentNode;

                            if (!openSet.Contains(connection))
                            {
                                openSet.Add(connection);
                            }
                        }
                    }
                }
            }
            //Debug.Log("A*: No path found to target");
            return pathPositions;
        }

        private static List<Vector3> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            path.Add(startNode);
            path.Reverse();

            List<Vector3> pathPositions = new List<Vector3>();
            foreach (Node node in path)
            {
                //Debug.Log(node.name);
                pathPositions.Add(node.position);
            }

            return pathPositions;
        }

        public void CreateNode()
        {
            if (m_NodePrefab)
            {
                Vector3 position = Camera.main.transform.position + Camera.main.transform.forward;
                Quaternion rotation = Quaternion.identity;
                GameObject newNode = Instantiate(m_NodePrefab, position, rotation, m_ArSpace.transform);

                System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
                int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

                int id = newNode.GetInstanceID();
                string nodeName = "node" + id.ToString() + "_" + cur_time.ToString();

                newNode.name = nodeName;
                newNode.GetComponent<Node>().nodeName = nodeName;
            }
        }

        private void GenerateConnectionMeshes()
        {
            List<Edge> edges = new List<Edge>();

            foreach (Node node in m_Nodes)
            {
                foreach (Node neighbour in node.neighbours)
                {
                    if (neighbour != null)
                    {
                        Edge edge = new Edge(node, neighbour);
                        edges.Add(edge);
                    }
                }
            }

            if (edges.Count < 1)
                return;

            // init
            float pathWidth = 0.06f;

            int edgeCount = edges.Count;
            int offset = 0;

            Vector3[] vertices = new Vector3[edgeCount * 4];
            Vector2[] uvs = new Vector2[edgeCount * 4];
            int[] triangleIndices = new int[edgeCount * 6];

            foreach (Edge edge in edges)
            {
                Vector3 startPosition = (edge.start.position);
                Vector3 endPosition = (edge.end.position);

                Vector3 billboardUp = Camera.main.transform.forward;
                //billboardUp = new Vector3(billboardUp.x, 0f, billboardUp.z);

                Quaternion edgeOrientation = Quaternion.LookRotation(endPosition - startPosition, billboardUp);

                Matrix4x4 startTransform = Matrix4x4.TRS(startPosition, edgeOrientation, Vector3.one);
                Matrix4x4 endTransform = Matrix4x4.TRS(endPosition, edgeOrientation, Vector3.one);


                Vector3[] shape = new Vector3[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f) };
                float[] shapeU = new float[] { 0f, 1f };

                int verticesOffset = offset * 4;
                int triangleIndicesOffset = offset * 6;

                vertices[verticesOffset + 0] = startTransform.MultiplyPoint(shape[0] * pathWidth);
                vertices[verticesOffset + 1] = startTransform.MultiplyPoint(shape[1] * pathWidth);
                vertices[verticesOffset + 2] = endTransform.MultiplyPoint(shape[1] * pathWidth);
                vertices[verticesOffset + 3] = endTransform.MultiplyPoint(shape[0] * pathWidth);

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

            m_Mesh.Clear();
            m_Mesh.vertices = vertices;
            m_Mesh.uv = uvs;
            m_Mesh.triangles = triangleIndices;
        }
    }
}