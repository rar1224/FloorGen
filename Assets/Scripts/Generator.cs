using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Generator : MonoBehaviour
{
    private Status status;
    public GameObject envelope;

    public Vertex vertexPrefab;
    public Edge edgePrefab;
    public Face facePrefab;
    public GameObject windowPrefab;

    //private List<Face> faces = new List<Face>();
    public List<Vertex> allVertices = new List<Vertex>();
    public List<Edge> allEdges = new List<Edge>();
    public List<Face> allFaces = new List<Face>();
    public List<Room> allRooms = new List<Room>();
    public List<ExternalWall> allWalls = new List<ExternalWall>();

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
            else status++;

        } else if (status == Status.DividingEnvelopeIntoCells)
        {
            // dividing envelope into smaller cells
            
            
            startLoop = DivideToCells(2, 1, true, true);
            status++;
            return;
            

        } else if (status == Status.DividingAllIntoCells)
        {
            
            // dividing rest into smaller cells
            
            if (startLoop.Count != 0)
            {
                DivideCurrentLoop();
            }
            else status++;
            

        } else if (status == Status.FindingFaces)
        {
            foreach(Vertex v in allVertices)
            {
                Face face = FindFace(v);
                if (face != null) allFaces.Add(face);
            }

            FindWalls();

            status++;
        }
        else if (status == Status.PlacingObjects)
        {
            // placing windows and doors
            PlaceWindows(4, 1, 0.5f);
            status++;
        }

        else if (status == Status.MakingRooms)
        {
            /*
            // dividing space into rooms;
            SetupRooms();
            int counter = 1;
            
            while (counter > 0)
            {
                counter = ExpandRooms();
            }
            */
            status++;

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

    public void FindWalls()
    {
        Edge current = allEdges[0];
        Vertex origin = current.Vertex1;
        Vertex next = current.Vertex2;

        List<ExternalWall> walls = new List<ExternalWall>();
        
        ExternalWall wall = new ExternalWall();
        ExternalWall currentWall = wall;

        while (next != origin)
        {
            // is it aligned with current wall
            if (currentWall.IsAligned(current))
            {
                currentWall.edges.Add(current);
            } else
            {
                walls.Add(currentWall);
                currentWall.Calculate();

                currentWall = new ExternalWall();
                currentWall.edges.Add(current);
            }
            current.GetComponent<Renderer>().material.color = Color.blue;
            current = next.FindNextExternalEdge(current);
            next = current.GetOtherVertex(next);
        }

        currentWall.edges.Add(current);
        walls.Add(currentWall);
        currentWall.Calculate();

        current.GetComponent<Renderer>().material.color = Color.blue;

        allWalls = walls;
    }

    public void PlaceWindows(int number, float width, float gap)
    {
        // directions and numbers of windows that can be placed on each
        // all walls for each direction (north, south...) counted together

        ExternalDirection[] externalDirections = new ExternalDirection[4];

        for (int i = 0; i < 4; i++)
        {
            List<ExternalWall> possibleWalls = new List<ExternalWall>();
            int possibleNumber = 0;

            foreach (ExternalWall wall in allWalls)
            {
                if (wall.Orientation == directions[i]) possibleWalls.Add(wall);
            }

            possibleWalls.Sort();

            foreach (ExternalWall wall in possibleWalls)
            {
                int windowsNumber = (int)((wall.Length - gap) / (width + gap));
                possibleNumber += windowsNumber;
            }

            externalDirections[i] = new ExternalDirection(directions[i], possibleWalls, possibleNumber, 1);
        }

        // for each direction, place appropriate number of windows

        foreach(ExternalDirection ext in externalDirections)
        {
            foreach (ExternalWall wall in ext.possibleWalls)
            {
                int windowsNumber = (int)((wall.Length - gap) / (width + gap));
                float correctGap = (wall.Length - windowsNumber * width) / (windowsNumber + 1);

                Debug.Log(ext.direction + " " + windowsNumber);

                for (int i = 0; i < windowsNumber; i++)
                {
                    Edge origin = wall.edges[0];
                    GameObject window = Instantiate(windowPrefab);
                    window.transform.position = origin.Vertex1.transform.position
                        + (Vector3)origin.Direction * ((i + 1) * correctGap + (i + 0.5f) * width);
                    window.transform.localScale.Set(width, 0.2f, 1);
                    window.transform.rotation = origin.transform.rotation;
                }
            }
        }
    }

    public void SetupRooms()
    {
        for (int i = 0; i < 4; i++)
        {
            Face face = null;
            while (face == null)
            {
                face = allFaces[Random.Range(0, allFaces.Count)];
                if (face.room == null) break;
            }
            Room room = new Room(0, 0, Vector2.zero, face, Random.ColorHSV());
            allRooms.Add(room);
        }
    }

    public int ExpandRooms()
    {
        int expanded = 0;

        foreach (Room room in allRooms)
        {
            if(ExpandRoom(room)) expanded++;
        }

        return expanded;
    }

    public bool ExpandRoom(Room room)
    {
        // generate a list of possible expansions for all faces
        List<Expansion> expansions = new List<Expansion>();

        foreach (Face face in room.faces)
        {
            foreach (Vector2 dir in directions)
            {
                float score = 0.5f;

                // check if valid
                Edge edge = face.GetEdgeInDirection(dir);
                Face otherFace = edge.GetOtherFace(face);

                if (otherFace == null || otherFace.room != null) continue;
                else
                {
                    // if expansion already in list, add to its score
                    bool onList = false;

                    foreach (Expansion exp in expansions)
                    {
                        if (exp.nextFace == otherFace)
                        {
                            exp.AddScore(1);
                            onList = true;
                        }
                    }

                    if (!onList)
                    {
                        Expansion exp = new Expansion(otherFace, 0.5f);
                        if (otherFace.IsExteriorAdjacent()) exp.AddScore(1);

                        expansions.Add(exp);
                    }
                }
            }
        }

        // sort by score
        expansions.Sort();

        // extend
        if (expansions.Count != 0)
        {
            Face chosen = expansions[0].nextFace;
            room.faces.Add(chosen);
            chosen.room = room;
            chosen.Recolor();
            return true;
        }
        else
        {
            return false;
        }
    }

    enum Status { DividingOnAngles, DividingEnvelopeIntoCells, DividingAllIntoCells, FindingFaces, PlacingObjects, MakingRooms, Completed }

    public struct Expansion : IComparable<Expansion>
    {
        public Face nextFace;
        public float score;

        public Expansion(Face nextFace, float score)
        {
            this.nextFace = nextFace;
            this.score = score;
        }

        public int CompareTo(Expansion other)
        {
            if (other.score < score) return -1;
            else if (other.score > score) return 1;
            else return 0;
        }

        public void AddScore(float score)
        {
            this.score += score;
        }
    }

    public struct ExternalDirection
    {
        public Vector2 direction;
        public List<ExternalWall> possibleWalls;
        public int possibleNumber;
        public float preference;
        public int setNumber;

        public ExternalDirection(Vector2 direction, List<ExternalWall> possibleWalls, int possibleNumber, float preference)
        {
            this.direction = direction;
            this.possibleWalls = possibleWalls;
            this.possibleNumber = possibleNumber;
            this.preference = preference;
            setNumber = 0;
        }
    }
}
