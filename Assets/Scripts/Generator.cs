using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    public GameObject doorPrefab;

    //private List<Face> faces = new List<Face>();
    public List<Vertex> allVertices = new List<Vertex>();
    public List<Edge> allEdges = new List<Edge>();
    public List<Face> allFaces = new List<Face>();
    public List<Room> allRooms = new List<Room>();
    public List<ExternalWall> allWalls = new List<ExternalWall>();
    public List<Wall> allRoomWalls = new List<Wall>();

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

        }
        else if (status == Status.DividingEnvelopeIntoCells)
        {
            // dividing envelope into smaller cells


            startLoop = DivideToCells(2f, 2f, true, true);
            status++;
            return;


        }
        else if (status == Status.DividingAllIntoCells)
        {

            // dividing rest into smaller cells

            if (startLoop.Count != 0)
            {
                DivideCurrentLoop();
            }
            else status++;


        }
        else if (status == Status.FindingFaces)
        {
            foreach (Vertex v in allVertices)
            {
                Face face = FindFace(v);
                if (face != null) allFaces.Add(face);
            }

            FindWalls(1, 0.5f);

            status++;
        }
        else if (status == Status.PlacingObjects)
        {
            // placing windows and doors

            PlaceFrontDoor(directions[0], 2f, 1, 0.5f);
            PlaceWindows(6, 1, 0.5f);

            status++;
        }
        else if (status == Status.ConnectingSharingCells)
        {
            foreach (ExternalWall wall in allWalls)
            {
                wall.ConnectSharingCells();
            }

            status++;
        }

        else if (status == Status.MakingRooms)
        {
            SetupRoomsFull();
            //SetupRoomsRandom();
            status++;
        }
        else if (status == Status.ExpandingRooms) {

            //Application.targetFrameRate = 1;
            // dividing space into rooms;

            
            int counter = ExpandRooms();

            if (counter == 0) status++;
            
        } 
        else if (status == Status.FillingGaps)
        {
            FillIslands();
            status++;

        } else if (status == Status.MakingCorridors)
        {
            FindAllWalls();

            UnityEngine.Debug.Log("Room walls: " + allRooms[4].roomWalls.Count);

            MakeCorridor(allRooms[0], allRooms[allRooms.Count - 1]);
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

                                    //UnityEngine.Debug.Log(vertex.transform.position);
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

    public void FindWalls(float windowWidth, float gap)
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
                current.wall = currentWall;
            } else
            {
                walls.Add(currentWall);
                currentWall.Calculate(windowWidth, gap);

                currentWall = new ExternalWall();
                currentWall.edges.Add(current);
                current.wall = currentWall;
            }
            current.GetComponent<Renderer>().material.color = Color.blue;
            current = next.FindNextExternalEdge(current);
            next = current.GetOtherVertex(next);
        }

        currentWall.edges.Add(current);
        current.wall = currentWall;

        walls.Add(currentWall);
        currentWall.Calculate(windowWidth, gap);

        current.GetComponent<Renderer>().material.color = Color.blue;

        allWalls = walls;
    }

    public void PlaceFrontDoor(Vector2 direction, float doorWidth, float windowWidth, float gap)
    {
        // pick longest wall on correct side of building
        List<ExternalWall> possibleWalls = new List<ExternalWall>();

        foreach(ExternalWall ext in allWalls)
        {
            if (ext.Orientation == direction) possibleWalls.Add(ext);
        }

        possibleWalls.Sort();
        ExternalWall wall = possibleWalls[0];

        // place door
        wall.SetupDoor(doorWidth, windowWidth, gap, doorPrefab);
    }

    public void PlaceWindows(int number, float width, float gap)
    {
        // directions and numbers of windows that can be placed on each
        // all walls for each direction (north, south...) counted together

        List<ExternalDirection> externalDirections = new List<ExternalDirection>();
        float maxRange = 0;

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

            ExternalDirection ext = new ExternalDirection(directions[i], possibleWalls, possibleNumber, 1);
            externalDirections.Add(ext);
            maxRange += ext.preference;
        }


        // calculate how many windows should be placed on each wall direction
        externalDirections.Sort();
        

        for(int i = 0; i < number; i++)
        {
            ExternalDirection chosen = null;
            float rand = Random.Range(0, maxRange);

            foreach (ExternalDirection ext in externalDirections)
            {
                if (ext.preference > rand) { chosen = ext; break; }
                else rand -= ext.preference;
            }

            if (chosen.setNumber < chosen.possibleNumber) chosen.setNumber++;
            else i--;
        }

        externalDirections.Sort();
        

        // for each direction, place appropriate number of windows

        foreach(ExternalDirection ext in externalDirections)
        {
            // divide set number of windows amongst all external walls on this side of building

            for (int i = 0; i < ext.setNumber; i++)
            {
                foreach(ExternalWall wall in ext.possibleWalls)
                {
                    if (wall.windowsNumber < wall.MaxWindowsNumber) { wall.windowsNumber++; break; }
                }
            }

            
        }

        // setup windows

        foreach (ExternalWall wall in allWalls)
        {
            if (wall.windowsNumber == 0 && wall.objects.Count == 0) continue;
            else
            {
                wall.SetupWindows(width, windowPrefab);
                wall.SetupAll();
            }
        }
    }

    public void SetupRoomsRandom()
    {
        List<Face> exteriorFaces = new List<Face>();
        Face entranceFace = null;

        foreach (ExternalWall wall in allWalls)
        {
            foreach(Edge edge in wall.edges)
            {
                if (!exteriorFaces.Contains(edge.faces[0]))
                {
                    exteriorFaces.Add(edge.faces[0]);
                    if (edge.faces[0].hasFrontDoor) entranceFace = edge.faces[0];
                }
            }
        }

        int number = 1;
        int spread = exteriorFaces.Count / number;

        for (int i = 0; i < number; i++)
        {
            Face face = null;
            int add = 0; 
            while (face == null)
            {
                face = exteriorFaces[spread * i + add];
                if (face.room != null) { add++; face = null; }
            }

            Room room = new Room(1000, face, Random.ColorHSV());

            //UnityEngine.Debug.Log("room face " + face.transform.position);
            allRooms.Add(room);
        }
    }

    public void SetupRoomsFull()
    {
        List<Face> exteriorFaces = new List<Face>();
        Face entranceFace = null;

        foreach (ExternalWall wall in allWalls)
        {
            foreach (Edge edge in wall.edges)
            {
                if (!exteriorFaces.Contains(edge.faces[0]))
                {
                    exteriorFaces.Add(edge.faces[0]);
                    if (edge.faces[0].hasFrontDoor) entranceFace = edge.faces[0];
                }
            }
        }

        int number = 5;
        int spread = exteriorFaces.Count / number;

        
        // entrance room
        Room entrance = new Room(10, entranceFace, new Color(0.43f, 0.82f, 0.16f));
        allRooms.Add(entrance);

        // other rooms

        List<Face> chosenFaces = new List<Face>();

        for (int i = 0; i < number; i++)
        {
            Face face = null;
            int add = 0;
            while (face == null)
            {
                //face = exteriorFaces[Random.Range(0, exteriorFaces.Count)];
                face = exteriorFaces[spread * i + add];
                if (face.room != null || chosenFaces.Contains(face)) { add++; face = null; }
                else
                {
                    foreach (Face face1 in face.connectedFaces)
                    {
                        if (chosenFaces.Contains(face1)) { add++; face = null; break; }
                    }
                }
            }

            chosenFaces.Add(face);
        }

        Room livingRoom = new Room(100, chosenFaces[0], Color.yellow);
        Room bedroom = new Room(100, chosenFaces[1], Color.cyan);
        Room bathroom = new Room(100, chosenFaces[2], Color.gray);
        Room kitchen = new Room(100, chosenFaces[3], Color.magenta);
        allRooms.Add(livingRoom);
        allRooms.Add(bedroom);
        allRooms.Add(bathroom);
        allRooms.Add(kitchen);
    }

    public int ExpandRooms()
    {
        int expanded = 0;

        foreach (Room room in allRooms)
        {
            if(ExpandRoomSideways(room)) expanded++;
        }

        return expanded;
    }

    public bool ExpandRoomSideways(Room room)
    {
        if (room.finished) return false;
        List<Expansion> expansions = new List<Expansion>();

        int repeatCounter = 1;

        // one expansion in each direction

        for (int i = 0; i < repeatCounter; i++)
        {

            foreach (Vector2 dir in directions)
            {
                List<Face> nextFaces = new List<Face>();
                bool dirValid = true;

                foreach (Face face in room.faces)
                {
                    Face nextFace = face.GetNextFace(dir);
                    if (nextFace != null) nextFace.AddToNextFaces(nextFaces);
                }

                /*
                for (int k = 0; k < i; k++)
                {
                    foreach (Face face in room.faces)
                    {
                        Face nextFace = face.GetNextFace(dir);

                        for (int j = 0; j < k + 1; j++)
                        {
                            if (nextFace != null) nextFace = nextFace.GetNextFace(dir);
                            else break;
                        }

                        if (nextFace == null || (nextFace.room != null && nextFace.room != room)) {dirValid = false; break; }
                        else nextFace.AddToNextFaces(nextFaces);
                    }
                }
                */

                if (!dirValid) continue;

                if (nextFaces.Count > 1)
                {
                    // check if not disrupting rectangular rooms
                    bool valid = true;

                    foreach (Face face in nextFaces)
                    {
                        Face previous = face.GetAdjacentFace(-dir);
                        Face next = face.GetAdjacentFace(dir);

                        // three conditions, needs to pass at least one

                        if (previous != null && previous.room == room)
                        {
                            continue;
                        }
                        else if (previous != null && nextFaces.Contains(previous))
                        {
                            continue;
                        }
                        else if (next != null && nextFaces.Contains(next))
                        {
                            continue;
                        }
                        else
                        {
                            valid = false; break;
                        }
                    }

                    if (!valid) continue;
                }

                if (nextFaces.Count > 0)
                {
                    // stops from disrupting already rectangular rooms
                    if (room.IsRectangular() && room.GetElementsNumber(dir) * (i + 1) != nextFaces.Count) continue;
                    Expansion exp = new Expansion(nextFaces, nextFaces.Count, dir);
                    exp.AddScore(nextFaces.Count);
                    if (room.lastDirection == dir) exp.AddScore(-5);
                    expansions.Add(exp);
                }
            }
        }

        

        expansions.Sort();

        // extend

       

        if (expansions.Count > 0)
        {
            /*
            UnityEngine.Debug.Log("Expand: " + room.color + ": " + expansions.Count +
           " options, chosen option with " + expansions[0].multipleFaces.Count + " next faces " + expansions[0].direction);
            */
            foreach (Face face in expansions[0].multipleFaces)
                {
                    face.SetRoom(room, expansions[0].direction);
                }
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool ExpandRoomRectangular(Room room)
    {
        if (room.finished) return false;
        List<Expansion> expansions = new List<Expansion>();

        foreach (Vector2 dir in directions)
        {
            foreach (Face face in room.faces)
            {
                // check if valid
                Edge edge = face.GetEdgeInDirection(dir);
                Face otherFace = edge.GetOtherFace(face);
                if (otherFace == null || otherFace.room != null) continue;

                bool onList = false;
                bool foundParallel = false;
                Expansion found = null;

                foreach (Expansion exp in expansions)
                {
                    // check if expansion already on the list
                    if (exp.nextFace != null && exp.nextFace == otherFace) { onList = true; found = exp; break; }
                    else if (exp.multipleFaces != null && exp.multipleFaces.Contains(otherFace)) { onList = true; found = exp; break; }

                    // check if expansion contains parallel cells
                    if (exp.nextFace != null && exp.nextFace.IsParallelTo(otherFace, dir)) { foundParallel = true; found = exp; break; }
                    else if (exp.multipleFaces != null && exp.multipleFaces.Contains(otherFace)) { foundParallel = true; found = exp; break; }
                }

                if (onList)
                {
                    found.AddScore(2);
                }
                else if (foundParallel)
                {
                    found.AddToList(otherFace);
                    found.AddScore(1);
                }
                else
                {
                    Expansion exp = new Expansion(otherFace, 0.5f, dir);
                    expansions.Add(exp);
                }
            }
        }

        // sort by score
        expansions.Sort();

        // extend
        if (expansions.Count != 0)
        {
            if (expansions[0].nextFace != null)
            {
                expansions[0].nextFace.SetRoom(room, expansions[0].direction);
                return true;
            }
            else
            {
                foreach (Face face in expansions[0].multipleFaces)
                {
                    face.SetRoom(room, expansions[0].direction);
                }
                return true;
            }
        }
        else
        {
            return false;
        }
    }
    public bool ExpandRoom(Room room)
    {
        if (room.finished) return false;
        // generate a list of possible expansions for all faces
        List<Expansion> expansions = new List<Expansion>();

        foreach (Face face in room.faces)
        {
            foreach (Vector2 dir in directions)
            {
                // check if valid
                Edge edge = face.GetEdgeInDirection(dir);
                Face otherFace = edge.GetOtherFace(face);

                if (otherFace == null || otherFace.room != null) continue;
                else
                {
                    bool onList = false;
                    // check if any expansions fill into a rectangle

                    // if expansion already in list, add to its score
                    foreach (Expansion exp in expansions)
                    {
                        if (exp.nextFace == otherFace || exp.nextFace.IsConnectedTo(otherFace) || otherFace.IsConnectedTo(exp.nextFace))
                        {
                            exp.AddScore(1);
                            onList = true;
                        }
                    }

                    if (!onList)
                    {
                        Expansion exp = new Expansion(otherFace, 0.5f, dir);
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
            expansions[0].nextFace.SetRoom(room, expansions[0].direction);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool FillIslands()
    {
        // reverse expansion, but gaps close together work as islands
        // divide space into islands
        List<List<Face>> islands = new List<List<Face>>();

        foreach(Face face in allFaces)
        {
            if (face.room == null)
            {
                bool isOnList = false;

                foreach(List<Face> list in islands)
                {
                    if (list.Contains(face))
                    {
                        isOnList = true; break;
                    }
                }

                if (!isOnList)
                {
                    List<Face> faces = new List<Face>();
                    face.AddAdjacentEmptyFaces(faces);
                    islands.Add(faces);
                }
            }
        }

        string counts = "";

        foreach(List<Face> list in islands)
        {
            Dictionary<Room, int> adjacentRooms = new Dictionary<Room, int>();

            foreach(Face face in list)
            {
                foreach (Face face1 in face.GetAllAdjacentFaces())
                {
                    if (face1.room != null)
                    {
                        if (adjacentRooms.ContainsKey(face1.room))
                        {
                            adjacentRooms[face1.room] += 1;

                        } else
                        {
                            adjacentRooms.Add(face1.room, 1);
                        }
                        
                    }
                }
            }

            //UnityEngine.Debug.Log(adjacentRooms.Count);

            KeyValuePair<Room, int> chosen = adjacentRooms.FirstOrDefault();

            foreach(KeyValuePair<Room, int> keyValuePair in adjacentRooms)
            {
                if (keyValuePair.Value > chosen.Value)
                {
                    chosen = keyValuePair;
                } else if ((keyValuePair.Value == chosen.Value) && keyValuePair.Key.GetArea() < chosen.Key.GetArea())
                {
                    chosen = keyValuePair;
                }
            }

            foreach(Face face in list)
            {
                face.SetRoom(chosen.Key, Vector2.zero);
            }
        }

        
        return true;
    }

    public bool FillGaps()
    {
        // reverse expansion - each cell chooses which room it belongs to
        // connected cells work together

        // check if all are filled, will be able to run again if not
        bool allFilled = true;

        foreach(Face face in allFaces)
        {
            if (face.room == null)
            {
                face.Recolor(Color.white);

                List<Expansion> expansions = new List<Expansion>();

                foreach(Vector2 dir in directions)
                {
                    Face nextFace = face.GetAdjacentFace(dir);

                    if (nextFace != null && nextFace.room != null)
                    {
                        bool onList = false;

                        foreach(Expansion exp in expansions)
                        {
                            if (exp.room == nextFace.room) onList = true;
                        }

                        if (onList) continue;

                        List<Face> currentFaces = new List<Face> { face };
                        if (face.connectedFaces.Count > 0) currentFaces.AddRange(face.connectedFaces);

                        // calculate score - will it make the room more or less square
                        float score = nextFace.room.CalculateNewSquareScore(currentFaces);

                        Expansion expansion = new Expansion(nextFace.room, score, dir);
                        expansions.Add(expansion);

                    }
                }

                if (expansions.Count > 0)
                {
                    expansions.Sort();

                    face.SetRoom(expansions[0].room, expansions[0].direction);
                } else
                {
                    allFilled = false;
                }
            }
        }

        return allFilled;
    }

    public void FindAllWalls()
    {
        List<Room> passedRooms = new List<Room>();

        foreach (Room room in allRooms)
        {
            allRoomWalls.AddRange(room.FindWalls(passedRooms));
            passedRooms.Add(room);
        }

        foreach (Wall wall in allRoomWalls)
        {
            wall.FindAdjacentWalls();
        }
    }

    public void MakeCorridor(Room room1, Room room2)
    {
        //List<Room> path = new List<Room>();
        //UnityEngine.Debug.Log(room1.RoomDistance(room2, path, 10));

        List<Wall> path = new List<Wall>();
        List<Wall> shortest = room1.roomWalls[0].WallDistance(room2, path, 50);
        UnityEngine.Debug.Log("Path length: " + shortest.Count);

        foreach (Wall wall in shortest)
        {
            if (wall.rooms.Contains(room1) || wall.rooms.Contains(room2)) continue;

            wall.Recolor(Color.green);

            foreach (Edge edge in wall.edges)
            {
                foreach (Face face in edge.faces)
                {
                    face.Recolor(Color.black);
                }
            }
        }
    }

    
    public void FindWallsBetweenRooms()
    {
        List<Edge> roomEdges = new List<Edge>();

        foreach(Room room in allRooms)
        {
            //room.FindWalls(roomEdges);
        }
    }

    public void BFS(Vertex start, Dictionary<Vertex, Vertex> parents,
        Dictionary<Vertex, int> distances)
    {
        // order of vertices
        Queue<Vertex> queue = new Queue<Vertex>();
        distances.Add(start, 0);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vertex v = queue.Dequeue();

            foreach (Edge edge in v.edges)
            {
                Vertex other = edge.GetOtherVertex(v);

                if (!distances.ContainsKey(other) || distances[other] == int.MaxValue)
                {
                    // current node as parent of neighboring node
                    if (parents.ContainsKey(other)) parents[other] = v;
                    else parents.Add(other, v);

                    if (distances.ContainsKey(other)) distances[other] = distances[v] + 1;
                    else distances.Add(other, distances[v] + 1);

                    queue.Enqueue(other);
                }
            }
        }
    }

    public void ShortestPath(Vertex start, Vertex end, int count)
    {
        Dictionary<Vertex, Vertex> parents = new Dictionary<Vertex, Vertex>(count);
        Dictionary<Vertex, int> distances = new Dictionary<Vertex, int>(count);

        BFS(start, parents, distances);

        if (!distances.ContainsKey(end)) UnityEngine.Debug.Log("not found");
        else if (distances[end] == int.MaxValue) UnityEngine.Debug.Log("not found");
        else
        {
            List<Vertex> path = new List<Vertex>();
            Vertex current = end;
            path.Add(end);
            while (!parents.ContainsKey(current))
            {
                path.Add(parents[current]);
                current = parents[current];
            }

            foreach (Vertex v in path)
            {
                v.Recolor(Color.blue);
            }

//UnityEngine.Debug.Log("Path: " + path.Count);
        }

        
    }


    enum Status { DividingOnAngles, DividingEnvelopeIntoCells, DividingAllIntoCells,
        FindingFaces, PlacingObjects, ConnectingSharingCells, MakingRooms, ExpandingRooms, FillingGaps, MakingCorridors, Completed }

    public class Expansion : IComparable<Expansion>
    {
        public Face nextFace;
        public float score;
        public Vector2 direction;

        public List<Face> multipleFaces;
        public Room room;

        public Expansion(Face nextFace, float score, Vector2 direction)
        {
            this.nextFace = nextFace;
            this.score = score;
            this.direction = direction;
        }

        public Expansion(List<Face> multipleFaces, float score, Vector2 direction)
        {
            this.multipleFaces = multipleFaces;
            this.score = score;
            this.direction = direction;
        }

        public Expansion(Room room, float score, Vector2 direction)
        {
            this.room = room;
            this.score = score;
            this.direction = direction;
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

        public void AddToList(Face face)
        {
            if (multipleFaces != null) multipleFaces.Add(face);
            else
            {
                multipleFaces = new List<Face>{nextFace, face};
                nextFace = null;
            }
        }
    }

    public class ExternalDirection : IComparable<ExternalDirection>
    {
        public Vector2 direction;
        public List<ExternalWall> possibleWalls;
        public int possibleNumber;
        public float weight;
        public float preference;
        public int setNumber;

        public ExternalDirection(Vector2 direction, List<ExternalWall> possibleWalls, int possibleNumber, float weight)
        {
            this.direction = direction;
            this.possibleWalls = possibleWalls;
            this.possibleNumber = possibleNumber;
            this.weight = weight;
            preference = weight * possibleNumber;
            setNumber = 0;
        }

        public int CompareTo(ExternalDirection other)
        {
            if (other.preference > preference) return -1;
            else if (other.preference < preference) return 1;
            else return 0;
        }
    }
}
