using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{
    public GameObject envelope;

    public Vertex vertexPrefab;
    public Edge edgePrefab;

    private List<Face> faces = new List<Face>();

    private bool ran = false;
    private Face start;
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
        }

        foreach (Edge edge in edges)
        {
            edge.transform.parent = envelope.transform;
        }

        start = new Face(edges);
        faces.Add(start);
    }

    private void Update()
    {
        if (!ran) DivideOnAngles(start);
        ran = true;
    }

    public void DivideOnAngles(Face face)
    {
        for (int i = 0; i < face.edges.Count; i++)
        {
            int j = i + 1;
            if (j == face.edges.Count) j = 0;

            if (Vector3.SignedAngle(face.edges[i].Direction, face.edges[j].Direction, Vector3.forward) <= 0) {

                if (face.edges[i].FindCommonVertex(face.edges[j]) == null)
                {
                    continue;
                }

                Vertex common = face.edges[i].FindCommonVertex(face.edges[j]).GetComponent<Vertex>();

                Debug.Log(common.gameObject.name);

                List<Vertex> newVertices = new List<Vertex>();

                // shoot line from vertex & check if connects
                foreach (Edge edge in common.edges)
                {
                    Vector2 direction = common.transform.position - edge.transform.position;
                    RaycastHit2D[] hits = Physics2D.RaycastAll(common.transform.position, direction);

                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider.gameObject == common.gameObject) continue;
                        if (hit.collider.gameObject.tag == "Vertex") break;

                        if (hit.collider.gameObject.tag == "Edge")
                        {
                            Edge hitEdge = hit.collider.gameObject.GetComponent<Edge>();
                            if (common.edges.Contains(hitEdge)) continue;

                            // divide & connect

                            // divide
                            float edgeWidth = hitEdge.transform.localScale.y;
                            Vector3 position = hit.point + edgeWidth/2 * direction.normalized;
                            Vertex vertex = Instantiate(vertexPrefab);
                            vertex.transform.position = position;
                            vertex.transform.parent = envelope.transform;
                            vertex.transform.SetSiblingIndex(hitEdge.Vertex1.transform.GetSiblingIndex() + 1);
                            hitEdge.DivideEdge(vertex);

                            Debug.Log(common.transform.GetSiblingIndex()  + " " + vertex.transform.GetSiblingIndex() +
                                hitEdge.name + hitEdge.transform.GetSiblingIndex());

                            newVertices.Add(vertex);

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
                        connect.Vertex1 = common;
                        connect.Vertex2 = vertex;
                        connect.UpdatePosition();

                        common.edges.Add(connect);
                        vertex.edges.Add(connect);
                    }
                    
                }
            }
        }
    }
}
