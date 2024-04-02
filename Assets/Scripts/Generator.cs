using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{
    private Status status;
    public GameObject envelope;

    public Vertex vertexPrefab;
    public Edge edgePrefab;
    public Face facePrefab;

    //private List<Face> faces = new List<Face>();
    public List<Vertex> allVertices = new List<Vertex>();
    public List<Edge> allEdges = new List<Edge>();
    public List<Face> allFaces = new List<Face>();

    private bool ran = false;
    private Face start;

    private List<Vertex> startLoop = new List<Vertex>();
    private List<Vertex> nextLoop = new List<Vertex>();
    private int startLoopCount = 0;

    Vector2[] directions = {Vector2.right, Vector2.down, Vector2.left, Vector2.up};
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
            edge.IsExterior = true;
            edge.UpdatePosition();

            vertex1.edges.Add(edge);
            vertex2.edges.Add(edge);

            edges.Add(edge);
            startLoop.Add(vertex1);

            allEdges.Add(edge);
            allVertices.Add(vertex1);
        }

        //start = new Face(edges);
        //faces.Add(start);
    }

    private void FixedUpdate()
    {
        if (status == Status.DividingOnAngles)
        {
            // preparatory dividing of the envelope on angles

            if (startLoop.Count != 0)
            {
                DivideCurrentLoop();
            }
            else status = Status.DividingEnvelopeIntoCells;

        } else if (status == Status.DividingEnvelopeIntoCells)
        {
            // dividing envelope into smaller cells
            
            
            startLoop = DivideToCells(2, 1, true, true);
            status = Status.DividingAllIntoCells;
            return;
            

        } else if (status == Status.DividingAllIntoCells)
        {
            
            // dividing rest into smaller cells
            
            if (startLoop.Count != 0)
            {
                DivideCurrentLoop();
            }
            else status = Status.FindingFaces;
            

        } else if (status == Status.FindingFaces)
        {
            foreach(Vertex v in allVertices)
            {
                Face face = FindFace(v);
                if (face != null) allFaces.Add(face);
            }
            
            status = Status.MakingRooms;
        } else if (status == Status.MakingRooms)
        {
            SetupRooms();
            status = Status.Completed;
        }
    }

    public void DivideCurrentLoop()
    {
        startLoopCount++;

        if (startLoopCount == startLoop.Count * 2)
        {
            startLoop.Clear();
            foreach (Vertex v in nextLoop) startLoop.Add(v);
            nextLoop.Clear();
            startLoopCount = 0;
        }

        else DivideOnVertex(startLoop[startLoopCount / 2]);
    }


    void DivideOnVertex(Vertex origin)
    {
            List<Vertex> newVertices = new List<Vertex>();

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

                        if (origin.edges.Contains(hitEdge)) continue;
                        else if ((int)(Vector3.Angle(hitEdge.Direction, direction)) != 90) break;

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

    public List<Vertex> DivideToCells(float maxCellHeight, float maxCellWidth, bool fromTop, bool fromLeft, float minDistance = 0.5f)
    {
        List<Vertex> newVertices = new List<Vertex>();
        List<Edge> newEdges = new List<Edge>();

        foreach (Edge edge in allEdges)
        {
            if (edge.IsExterior)
            {
                // for direction of division
                bool tooLong = false;
                Vertex origin = null;
                Vector2 divisionDirection = Vector2.zero;
                bool reversed = false;
                int divisions = 0;

                if ((edge.Direction == Vector2.up || edge.Direction == Vector2.down)
                && edge.GetLength() > maxCellHeight)
                {
                    tooLong = true;
                    if (fromTop ^ edge.Direction == Vector2.down)
                    {
                        origin = edge.Vertex2;
                        divisionDirection = -edge.Direction;
                        reversed = true;
                    }
                    else
                    {
                        origin = edge.Vertex1;
                        divisionDirection = edge.Direction;
                    }

                    divisions = (int)(edge.GetLength() / maxCellHeight);
                }
                else if ((edge.Direction == Vector2.left || edge.Direction == Vector2.right)
                && edge.GetLength() > maxCellWidth)
                {
                    tooLong = true;
                    if (fromLeft ^ edge.Direction == Vector2.right)
                    {
                        origin = edge.Vertex2;
                        divisionDirection = -edge.Direction;
                        reversed = true;
                    }
                    else
                    {
                        origin = edge.Vertex1;
                        divisionDirection = edge.Direction;
                    }

                    divisions = (int)(edge.GetLength() / maxCellWidth);
                }

                if (tooLong)
                {
                    Edge currentEdge = edge;

                    // divide on all new vertices

                    for (int i = 0; i < divisions; i++)
                    {
                        Vertex vertex = null;

                        if (i == divisions - 1)
                        {      
                            // check length of last part
                            if (((divisionDirection == Vector2.up || divisionDirection == Vector2.down) &&
                                    (currentEdge.GetLength() < minDistance + maxCellHeight)) ||
                               ((divisionDirection == Vector2.left || divisionDirection == Vector2.right) &&
                                    (currentEdge.GetLength() < minDistance + maxCellWidth)))
                            {
                                if (currentEdge.GetLength() <= minDistance * 2 + 0.02f) break;
                                else
                                {
                                    vertex = Instantiate(vertexPrefab);

                                    if (divisionDirection == Vector2.up || divisionDirection == Vector2.down)
                                        vertex.transform.position = (Vector2)origin.transform.position
                                       + (i + 1) * maxCellHeight * divisionDirection
                                       - minDistance * divisionDirection;
                                    else
                                        vertex.transform.position = (Vector2)origin.transform.position
                                           + (i + 1) * maxCellWidth * divisionDirection
                                           - minDistance * divisionDirection;

                                    Debug.Log(vertex.transform.position);
                                }
                            } else
                            {
                                vertex = Instantiate(vertexPrefab);

                                if (divisionDirection == Vector2.up || divisionDirection == Vector2.down)
                                    vertex.transform.position = (Vector2)origin.transform.position
                                   + (i + 1) * maxCellHeight * divisionDirection;
                                else
                                    vertex.transform.position = (Vector2)origin.transform.position
                                       + (i + 1) * maxCellWidth * divisionDirection;
                            }
                            
                        } else
                        {
                            vertex = Instantiate(vertexPrefab);

                            if (divisionDirection == Vector2.up || divisionDirection == Vector2.down)
                                vertex.transform.position = (Vector2)origin.transform.position
                               + (i + 1) * maxCellHeight * divisionDirection;
                            else
                                vertex.transform.position = (Vector2)origin.transform.position
                                   + (i + 1) * maxCellWidth * divisionDirection;

                        }


                        if (reversed)
                        {
                            currentEdge = currentEdge.DivideEdgeReverse(vertex);
                        }
                        else
                        {
                            currentEdge = currentEdge.DivideEdge(vertex);
                        }

                        newEdges.Add(currentEdge);
                        newVertices.Add(vertex);

                        //DivideOnVertex(vertex);
                    }
                }
            }
        }

        foreach(Vertex v in newVertices) allVertices.Add(v);
        foreach(Edge e in newEdges) allEdges.Add(e);

        return newVertices;
    }

    public Face FindFace(Vertex origin)
    {
        Vertex current = origin;
        int dirIndex = 0;

        List<Edge> faceEdges = new List<Edge>();
        List<Vertex> faceVertices = new List<Vertex>();

        List<Vector2> directionsList = new List<Vector2>();
        directionsList.AddRange(directions);

        for (int i = 0; i < 4; i++)
        {
            Edge found = null;

            foreach (Edge e in current.edges)
            {
                if (e.GetDirectionFrom(current) == directionsList[dirIndex])
                {
                    faceVertices.Add(current);
                    faceEdges.Add(e);

                    current = e.GetOtherVertex(current);
                    found = e;

                    break;
                }
            }

            if (found == null)
            {
                return null;

            } else
            {
                dirIndex++;
            }
        }

        Face face = Instantiate(facePrefab);
        face.vertices = faceVertices;
        face.SetEdges(faceEdges);
        face.MakeVisible();

        return face;
    }

    public void SetupRooms()
    {
        Room entrance = new Room(0, 0, Vector2.zero, allFaces[Random.Range(0, allFaces.Count - 1)]);
        Room livingRoom = new Room(0, 0, Vector2.zero, allFaces[Random.Range(0, allFaces.Count - 1)]);
        Room bedroom1 = new Room(0, 0, Vector2.zero, allFaces[Random.Range(0, allFaces.Count - 1)]);
        Room bedroom2 = new Room(0, 0, Vector2.zero, allFaces[Random.Range(0, allFaces.Count - 1)]);
    }

    enum Status { DividingOnAngles, DividingEnvelopeIntoCells, DividingAllIntoCells, FindingFaces, MakingRooms, Completed }
}
