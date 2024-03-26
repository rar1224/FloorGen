using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{
    public GameObject envelope;

    public Vertex vertexPrefab;
    public Edge edgePrefab;

    //private List<Face> faces = new List<Face>();
    public List<Vertex> allVertices = new List<Vertex>();
    public List<Edge> allEdges = new List<Edge>();

    private bool ran = false;
    private Face start;

    private List<Vertex> startLoop = new List<Vertex>();
    private List<Vertex> nextLoop = new List<Vertex>();
    private int startLoopCount = 0;

    Vector2[] directions = {Vector2.right, Vector2.left, Vector2.down, Vector2.up};
    // Start is called before the first frame update
    void Start()
    {
        List<Edge> edges = new List<Edge>();

        for (int i = 0; i < envelope.transform.childCount; i++)
        {
            int j = i + 1;
            if (j == envelope.transform.childCount) j = 0;

            Vertex vertex1 = envelope.transform.GetChild(i).GetComponent<Vertex>();
            Vertex vertex2 = envelope.transform.GetChild(j).GetComponent<Vertex>();

            Edge edge = Instantiate(edgePrefab);
            edge.Vertex1 = vertex1;
            edge.Vertex2 = vertex2;
            edge.UpdatePosition();

            vertex1.edges.Add(edge);
            vertex2.edges.Add(edge);

            edges.Add(edge);
            startLoop.Add(vertex1);

            allEdges.Add(edge);
            allVertices.Add(vertex1);
        }

        start = new Face(edges);
        //faces.Add(start);
    }

    private void FixedUpdate()
    {
        if (startLoop.Count != 0)
        {
            startLoopCount++;

            if (startLoopCount == startLoop.Count * 2)
            {
                startLoop.Clear();
                foreach (Vertex v in nextLoop) startLoop.Add(v);
                nextLoop.Clear();
                startLoopCount = 0;
            } else DivideOnVertex(startLoop[startLoopCount / 2]);
         
        }
    }


    void DivideOnVertex(Vertex origin)
    {
            List<Vertex> newVertices = new List<Vertex>();

            Debug.Log(origin.transform.position);

            foreach (Vector2 direction in directions)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin.transform.position, direction);

                foreach (RaycastHit2D hit in hits)
                {
                if (hit.collider.gameObject == origin.gameObject) continue;

                if (hit.collider.gameObject.tag == "Vertex")
                {
                    // connect if not yet connected
                    Vertex hitVertex = hit.collider.gameObject.GetComponent<Vertex>();

                    if (!origin.IsConnectedTo(hitVertex)) newVertices.Add(hitVertex);

                    break;
                }

                    if (hit.collider.gameObject.tag == "Edge")
                    {
                        Edge hitEdge = hit.collider.gameObject.GetComponent<Edge>();
                        if (origin.edges.Contains(hitEdge) || Vector3.Angle(hitEdge.Direction, direction) != 90) break;

                        // divide & connect

                        // divide
                        float edgeWidth = hitEdge.transform.localScale.y;
                        Vector3 position = hit.point + edgeWidth / 2 * direction.normalized;
                        Vertex vertex = Instantiate(vertexPrefab);
                        vertex.transform.position = position;
                        //vertex.transform.parent = envelope.transform;
                        vertex.transform.SetSiblingIndex(hitEdge.Vertex1.transform.GetSiblingIndex() + 1);
                        Edge newEdge = hitEdge.DivideEdge(vertex);
                        newVertices.Add(vertex);

                        allEdges.Add(newEdge);
                        allVertices.Add(vertex);

                        break;
                    }
                }
            }

            // connect



            if (newVertices.Count != 0)
            {
                foreach (Vertex vertex in newVertices)
                {
                    Edge connect = Instantiate(edgePrefab);
                    connect.Vertex1 = origin;
                    connect.Vertex2 = vertex;
                    connect.UpdatePosition();

                    origin.edges.Add(connect);
                    vertex.edges.Add(connect);

                    nextLoop.Add(vertex);

                    allEdges.Add(connect);
                }

            }
    }
}
