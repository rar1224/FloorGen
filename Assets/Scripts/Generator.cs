using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public class Generator : MonoBehaviour
{
    private Status status;
    public SaveJSON save;
    public GameObject envelope;
    public bool optimize;
    public bool superOptimize;
    public bool rectangleExpansion;

    public float maxCellHeight;
    public float maxCellWidth;
    public float minSize;
    public float frontDoorWidth;
    public float windowWidth;
    public int windowNumber;
    public float gap;

    public Vertex vertexPrefab;
    public Edge edgePrefab;
    public Face facePrefab;
    public GameObject windowPrefab;
    public GameObject doorPrefab;
    public Door interiorDoorPrefab;

    //private List<Face> faces = new List<Face>();
    public List<Vertex> allVertices = new List<Vertex>();
    public List<Edge> allEdges = new List<Edge>();
    public List<Face> allFaces = new List<Face>();
    public List<Room> allRooms = new List<Room>();
    public List<ExternalWall> allWalls = new List<ExternalWall>();
    public List<Wall> allRoomWalls = new List<Wall>();

    public List<GameObject> allDoors = new List<GameObject>();
    public List<GameObject> allWindows = new List<GameObject>();

    private List<Vertex> startLoop = new List<Vertex>();
    private List<Vertex> nextLoop = new List<Vertex>();
    private int startLoopCount = 0;

    private int maxLoops = 10;
    private int loopCounter = 0;

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


            startLoop = DivideToCells(maxCellHeight, maxCellWidth, true, true, minSize);
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

            FindWalls(windowWidth, gap);

            status++;
        }
        else if (status == Status.PlacingObjects)
        {
            // placing windows and doors

            allDoors.Add(PlaceFrontDoor(frontDoorWidth, windowWidth, gap, optimize));
            allWindows.AddRange(PlaceWindows(7, windowWidth, gap, optimize, superOptimize));

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
            if (loopCounter > maxLoops)
            {
                // too many loops, try resetting door and windows
                RemoveExteriorObjects();
                status = Status.PlacingObjects;
                loopCounter = 0;
            } else
            {
                SetupRoomsFull();
                loopCounter++;
                Debug.Log(loopCounter);
                status++;
            }
            
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

        } 
        else if (status == Status.MakingCorridors)
        {
            FindAllWalls();

            bool corridorsMade = MakeAllCorridors();

            status++;

            if (!corridorsMade)
            {
                ResetRooms();
                status = Status.MakingRooms;
            }
        }
         else if (status == Status.FixingWalls)
        {
            ResetWalls();
            FindAllWalls();
            ResetExteriorObjects();

            status++;
            
            if (optimize)
            {
                if (!superOptimize)
                {
                    if (!CheckValidity(3f))
                    {
                        ResetRooms();
                        status = Status.MakingRooms;
                    }
                    else
                    {
                        DefineRoomsOptimize();
                    }
                } else
                {
                    if (!CheckValidity(0.5f))
                    {
                        ResetRooms();
                        status = Status.MakingRooms;
                    }
                    else
                    {
                        DefineRoomsSuperoptimize();
                    }
                }
                
            } else
            {
                if (!CheckValidity(3f))
                {
                    ResetRooms();
                    status = Status.MakingRooms;
                }
                else
                {
                    DefineRooms();
                }
            }  

        } 
        else if (status == Status.Saving)
        {
            save.SaveIntoJson(allRoomWalls, allRooms, windowWidth, frontDoorWidth);
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
                    if (origin.IsConnectedTo(vertex)) continue;

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

    public List<Vertex> DivideToCells(float maxCellHeight, float maxCellWidth, bool fromTop, bool fromLeft, float minDistance)
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
            } else
            {
                walls.Add(currentWall);
                currentWall.Calculate(windowWidth, gap);

                currentWall = new ExternalWall();
                currentWall.edges.Add(current);
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


    public GameObject PlaceFrontDoor(float doorWidth, float windowWidth, float gap, bool optimize)
    {
        Vector2 direction;

        if (!optimize) direction = directions[Random.Range(0, directions.Length)];
        else direction = directions[Random.Range(0, 2) * 2 + 1];


        // pick longest wall on correct side of building
        List<ExternalWall> possibleWalls = new List<ExternalWall>();

        foreach(ExternalWall ext in allWalls)
        {
            if (ext.Orientation == direction) possibleWalls.Add(ext);
        }

        possibleWalls.Sort();
        ExternalWall wall = possibleWalls[0];

        // place door
        return wall.SetupDoor(doorWidth, windowWidth, gap, doorPrefab);
    }

    public List<GameObject> PlaceWindows(int number, float width, float gap, bool optimize, bool superOptimize)
    {
        // directions and numbers of windows that can be placed on each
        // all walls for each direction (north, south...) counted together

        List<GameObject> windows = new List<GameObject>();

        List<ExternalDirection> externalDirections = new List<ExternalDirection>();

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

            ExternalDirection ext = new ExternalDirection(directions[i], possibleWalls, possibleNumber, 0.2f);

            // optimization
            if (optimize)
            {
                if (directions[i] == Vector2.up || directions[i] == Vector2.down) ext.preference = 1;
            } else
            {
                ext.preference = 1;
                if (superOptimize) ext.preference = 0;
            }
            

            externalDirections.Add(ext);
        }


        // calculate how many windows should be placed on each wall direction

        


        for (int i = 0; i < number; i++)
        {
            externalDirections = externalDirections.OrderBy(x => Random.Range(0, Int32.MaxValue)).ToList();

            ExternalDirection chosen = null;
            float rand = Random.Range(0.0f, 1.0f);

            foreach (ExternalDirection ext in externalDirections)
            {
                if (ext.preference > rand) { chosen = ext; break; }
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
                windows.AddRange(wall.SetupWindows(width, windowPrefab));
                wall.SetupAll();
            }
        }

        return windows;
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

        Room entrance = new Room(1000, entranceFace, Random.ColorHSV());
        entrance.corridor = true;

        //UnityEngine.Debug.Log("room face " + face.transform.position);
        allRooms.Add(entrance);


        int number = 4;

        for (int i = 0; i < number; i++)
        {
            Face face = null;
            int add = 0; 
            while (face == null)
            {
                face = exteriorFaces[Random.Range(0, exteriorFaces.Count)];
                // optimization (validity)
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
        Room entrance = new Room(2, entranceFace, Color.green);
        entrance.corridor = true;
        allRooms.Add(entrance);

        // other rooms
        int index = Random.Range(0, exteriorFaces.Count);

        List<Face> chosenFaces = new List<Face>();

        for (int i = 0; i < number; i++)
        {
            Face face = null;
            int add = 0;
            while (face == null)
            {
                //face = exteriorFaces[Random.Range(0, exteriorFaces.Count)];
                face = exteriorFaces[(index + spread * i + add) % exteriorFaces.Count];
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

        Room r1 = new Room(100, chosenFaces[0], Color.yellow);
        Room r2 = new Room(100, chosenFaces[1], Color.cyan);
        Room r3 = new Room(100, chosenFaces[2], Color.gray);
        Room r4 = new Room(100, chosenFaces[3], Color.magenta);
        allRooms.Add(r1);
        allRooms.Add(r2);
        allRooms.Add(r3);
        allRooms.Add(r4);
    }

    public int ExpandRooms()
    {
        int expanded = 0;

        allRooms.Sort();

        foreach (Room room in allRooms)
        {
            bool e = false;
            if (rectangleExpansion) {
                e = ExpandRoomSideways(room);
                    }
            else e = ExpandRoom(room);

            if(e) expanded++;
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

            if (chosen.Key == null) {
                Debug.Log("chosen null");
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
            List<Wall> walls = room.FindWalls(passedRooms);
            foreach (Wall wall in walls) if (!allRoomWalls.Contains(wall)) allRoomWalls.Add(wall);
            passedRooms.Add(room);
        }

        foreach (Wall wall in allRoomWalls)
        {
            wall.FindAdjacentWalls();
        }

        // Debug
        foreach(Edge edge in allEdges)
        {
            if (edge.wall == null) edge.Recolor(Color.white);
            else edge.Recolor(Color.blue);
        }
    }

    public bool MakeAllCorridors()
    {
        // make the entrance connect to every room

        List<Room> corridors = new List<Room>();
        Room entrance = null;

        foreach (Room room in allRooms) if (room.corridor) { corridors.Add(room); entrance = room; }

        int counter = 0;

        while(counter != 200)
        {
            counter++;
            Room newCorridor = null;
            bool allConnected = true;

            foreach (Room room in allRooms)
            {
                if (room.corridor) continue;

                bool connectedToCorridor = false;

                foreach (Room corridor in corridors)
                {
                    if (room != corridor && room.IsAdjacent(corridor))
                    {
                        connectedToCorridor = true;
                        break;
                    }
                }

                if (!connectedToCorridor)
                {
                    foreach(Room cor in corridors)
                    {
                        newCorridor = MakeCorridor(room, cor);
                        if (newCorridor != null) break;
                    }
                    
                    corridors.Add(newCorridor);

                    ResetWalls();
                    FindAllWalls();

                    allConnected = false;
                    break;
                }
            }

            if (allConnected)
            {
                UnityEngine.Debug.Log("new corridors not needed");
                return true;
            } else if (newCorridor == null)
            {
                UnityEngine.Debug.Log("can't find corridor");
                return false;
            }
        }

        UnityEngine.Debug.Log("can't connect everything");
        return false;

    }

    public Room MakeCorridor(Room room1, Room room2)
    {
        if (room1.IsAdjacent(room2)) return null;

        //List<Room> path = new List<Room>();
        //UnityEngine.Debug.Log(room1.RoomDistance(room2, path, 10));

        List<Wall> shortest = room1.ShortestWallPath(room2, 50);

        List<List<Face>> corridorOptions = new List<List<Face>>();
        bool first = true;



        foreach (Wall wall in shortest)
        {
            if ((wall.rooms.Contains(room1) || wall.rooms.Contains(room2)) &&
                !(shortest.IndexOf(wall) == shortest.Count - 1 && corridorOptions.Count == 0)) continue;

            wall.Recolor(Color.green);

            
            Vector2 dir1 = wall.Orientation;
            Vector2 dir2 = -dir1;

            List<Face> corridor1 = wall.GetFacesInDirection(dir1);
            List<Face> corridor2 = wall.GetFacesInDirection(dir2);

            List<List<Face>> newOptions = new List<List<Face>>();

            if (first)
            {
                if (corridor1 != null) newOptions.Add(corridor1);
                if (corridor2 != null) newOptions.Add(corridor2);
                first = false;
            } else
            {
                foreach (List<Face> option in corridorOptions)
                {
                    if (corridor1 != null)
                    {
                        List<Face> newOption = new List<Face>(option);
                        foreach (Face face in corridor1) if (!newOption.Contains(face)) newOption.Add(face);
                        newOptions.Add(newOption);
                    }

                    if (corridor2 != null)
                    {
                        List<Face> newOption = new List<Face>(option);
                        foreach (Face face in corridor2) if (!newOption.Contains(face)) newOption.Add(face);
                        newOptions.Add(newOption);
                    }
                }
            }

            corridorOptions = newOptions;
        }

        UnityEngine.Debug.Log("Corridor options: " + corridorOptions.Count);
        List<List<Face>> fullCorridors = new List<List<Face>>();

        


        // connect disconnected corridor faces
        foreach (List<Face> option in corridorOptions)
        {
            // find first & last face in the corridor
            Face firstFace = null, lastFace = null;

            foreach (Face face in option)
            {
                if (face.IsCorridorEndFace(room1, option))
                {
                    firstFace = face;
                }

                if (face.IsCorridorEndFace(room2, option))
                {
                    lastFace = face;
                }
            }

            if (firstFace == null || lastFace == null)
            {
                foreach (Face face in option)
                {
                    if (firstFace == null)
                    {
                        Face foundFace = face.GetFaceAdjacentToRoom(room1);
                        if (foundFace != null) firstFace = foundFace;
                    }

                    if (lastFace == null)
                    {
                        Face foundFace = face.GetFaceAdjacentToRoom(room2);
                        if (foundFace != null) lastFace = foundFace;
                    }
                }

                if (!option.Contains(firstFace)) option.Add(firstFace);
                if (!option.Contains(lastFace)) option.Add(lastFace);
            }

            Face current = firstFace;
            List<Face> fullCorridor = new List<Face>();
            List<Face> passedFaces = new List<Face>();

            // find corridor

            int counter = 0;

           
        while(current != lastFace)
        {
                if (counter > 200) { UnityEngine.Debug.Log("corridor not found"); return null; }
                counter++;
                current.AddWithConnectedFaces(fullCorridor);
                passedFaces.Add(current);

            Face next = current.GetNextCorridorFace(option, passedFaces);
            if (next == null) next = current.GetNextCorridorFaceFill(option, passedFaces);

            if (next == null)
                {
                    UnityEngine.Debug.Log("corridor not found");
                    return null;
                }
            current = next;
        }
            
                current.AddWithConnectedFaces(fullCorridor);
            fullCorridors.Add(fullCorridor);
    }


        // pick option
        // based on which option reduces the rooms the least
        if (fullCorridors.Count == 0)
        {
            UnityEngine.Debug.Log("corridor not found");
            return null;
        }

        bool corridorValid = true;

        foreach(Room room in allRooms)
        {
            bool facesLeft = false;

            foreach(Face face in room.faces)
            {
                if (!fullCorridors[0].Contains(face)) { facesLeft = true; break; }
            }

            if (!facesLeft) { corridorValid = false; break; }
        }

        if (!corridorValid)
        {
            UnityEngine.Debug.Log("corridor not valid");
            return null;
        }

        Room newCorridor = new Room(fullCorridors[0], Color.green);
        newCorridor.corridor = true;

        allRooms.Add(newCorridor);
        return newCorridor;
    }

    public void ResetWalls()
    {
        allRoomWalls.Clear();

        foreach (Room room in allRooms)
        {
            room.roomWalls.Clear();
            room.roomEdges.Clear();
        }

        foreach(Edge edge in allEdges)
        {
            edge.wall = null;

        }

    }

    public List<GameObject> PlaceInteriorDoors(float width)
    {
        List<GameObject> doors = new List<GameObject>();
        List<Room> corridors = new List<Room>();

        // make corridors open to each other

        foreach (Room room in allRooms)
        {
            if (room.corridor)
            {
                foreach(Room corridor in corridors)
                {
                    if (room.IsAdjacent(corridor) && !room.IsConnectedWith(corridor))
                    {
                        Door door = room.PlaceInteriorDoor(corridor, width, interiorDoorPrefab);
                        doors.Add(door.gameObject);
                    }
                }
                corridors.Add(room);
            }
        }

        // every non-corridor opens to a corridor if possible

        foreach(Room room in allRooms)
        {
            if (!room.corridor)
            {
                bool isConnected = false;

                foreach(Room corridor in corridors)
                {
                    if (room.IsConnectedWith(corridor))
                    {
                        isConnected = true;
                        break;
                    }
                }

                if (!isConnected)
                {
                    List<Wall> options = new List<Wall>();

                    foreach(Room corridor in corridors)
                    {
                        if (room.IsAdjacent(corridor))
                        {
                            options.AddRange(room.GetWallsAdjacentTo(corridor));
                        }
                    }

                    if (options.Count == 0)
                    {
                        UnityEngine.Debug.Log("no door options for room " + room.color);
                        continue;
                    }

                    Wall chosen = options.FirstOrDefault();

                    foreach(Wall option in options)
                    {
                        option.Calculate();
                        if (chosen.Length < option.Length) chosen = option;
                    }

                    Door door = chosen.PlaceInteriorDoor(width, interiorDoorPrefab);
                    doors.Add(door.gameObject);
                }
            }
        }

        return doors;
    }

    public void RemoveExteriorObjects()
    {
        foreach(GameObject window in allWindows)
        {
            Destroy(window);
        }

        foreach(GameObject door in allDoors)
        {
            Destroy(door);
        }

        foreach(ExternalWall wall in allWalls)
        {
            wall.RemoveExteriorObjects();
        }
    }

    public void ResetExteriorObjects()
    {
        foreach(Edge edge in allEdges)
        {
            if (edge.wall == null) continue;
            List<Collider2D> colliders = new List<Collider2D>();
            edge.gameObject.GetComponent<Collider2D>().OverlapCollider(new ContactFilter2D().NoFilter(), colliders);

            foreach(Collider2D collider in colliders)
            {
                if (collider.tag == "FrontDoor" || collider.tag == "Window")
                {
                    if (!edge.wall.objects.Contains(collider.gameObject)) edge.wall.objects.Add(collider.gameObject);
                    if (collider.tag == "Window") edge.faces[0].hasWindows = true;
                }
            }
        }
    }

    public bool CheckValidity(float minArea)
    {
        int index = 0;
        int counter = 0;
        allRooms.Sort();

        foreach(Room room in allRooms)
        {
            // size of rooms
            if (room.corridor) continue;

            if (room.area < minArea) return false;

            // ratio
            if (room.GetRatio() < 0.4)
            {
                return false;
            }

            bool hasWindows = false;

            // each room needs to have a window

            foreach (Face face in room.faces)
            {
                if (face.hasWindows)
                {
                    hasWindows = true;
                    break;
                }
            }

            if (!hasWindows)
            {
                if (counter == 1 || index > 1) return false;
                else counter++;
            }

            index++;
        }

        return true;
    }

    public void DefineRooms()
    {
        allRooms.Sort();

        int corridorNr = 0;
        int nameIndex = 0;
        List<string> roomNames = new List<string>{"bathroom", "kitchen", "bedroom", "living_room"};

        foreach(Room room in allRooms)
        {
            if (room.corridor)
            {
                room.name = "corridor" + corridorNr++;
            } else
            {
                room.name = roomNames[nameIndex++];
            }

            UnityEngine.Debug.Log(room.color + " " + room.name);
        }
    }

    public void DefineRoomsOptimize()
    {
        allRooms.Sort();
        List<Room> roomsToDefine = new List<Room>();

        foreach (Room room in allRooms)
        {
            if (!room.corridor)
            {
                roomsToDefine.Add(room);
                room.CalculateWallOrientation();
            } else
            {
                room.name = "corridor";
            }
        }

        List<RoomType> types = new List<RoomType> {
            new RoomType("bathroom", new List<Vector2> {Vector2.down, Vector2.right}, 0, 1, 1, false),
            new RoomType("living_room", new List<Vector2> {Vector2.down, Vector2.right}, 1, 3, 2, true),
            new RoomType("bedroom", new List<Vector2> { Vector2.up, Vector2.left }, 1, 3, 3, true),
            new RoomType("kitchen", new List<Vector2> { Vector2.up, Vector2.left }, 0, 2, 3, true)
        };

        List<Room> leftToDefine = new List<Room>();
        leftToDefine.AddRange(roomsToDefine);

        foreach(RoomType type in types)
        {
            Room chosen = leftToDefine[0];
            int highScore = -10;

            foreach(Room room in leftToDefine)
            {
                int score = type.GetScore(room, roomsToDefine);
                if (score > highScore)
                {
                    chosen = room;
                    highScore = score;
                }
            }

            leftToDefine.Remove(chosen);
            chosen.name = type.name;
        }


        foreach (Room room in allRooms) UnityEngine.Debug.Log(room.color + " " + room.name + " " + room.GetArea());
    }

    public void DefineRoomsSuperoptimize()
    {
        allRooms.Sort();
        List<Room> roomsToDefine = new List<Room>();

        foreach (Room room in allRooms)
        {
            if (!room.corridor)
            {
                roomsToDefine.Add(room);
                room.CalculateWallOrientation();
            }
            else
            {
                room.name = "corridor";
            }
        }

        List<RoomType> types = new List<RoomType> {
            new RoomType("bathroom", new List<Vector2> {Vector2.down, Vector2.right}, 0, 0, 1, false),
            new RoomType("living_room", new List<Vector2> {Vector2.down, Vector2.right}, 1, 1, 1, false),
            new RoomType("bedroom", new List<Vector2> { Vector2.up, Vector2.left }, 3, 3, 3, false),
            new RoomType("kitchen", new List<Vector2> { Vector2.up, Vector2.left }, 2, 2, 3, false)
        };

        List<Room> leftToDefine = new List<Room>();
        leftToDefine.AddRange(roomsToDefine);

        foreach (RoomType type in types)
        {
            Room chosen = leftToDefine[0];
            int highScore = -10;

            foreach (Room room in leftToDefine)
            {
                int score = type.GetScore(room, roomsToDefine);
                if (score > highScore)
                {
                    chosen = room;
                    highScore = score;
                }
            }

            leftToDefine.Remove(chosen);
            chosen.name = type.name;
        }


        foreach (Room room in allRooms) UnityEngine.Debug.Log(room.color + " " + room.name + " " + room.GetArea());
    }

    public void ResetRooms()
    {
        ResetWalls();

        foreach(Face face in allFaces)
        {
            face.room = null;
        }

        allRooms.Clear();
    }



    enum Status { DividingOnAngles, DividingEnvelopeIntoCells, DividingAllIntoCells,
        FindingFaces, PlacingObjects, ConnectingSharingCells, MakingRooms, ExpandingRooms, FillingGaps, MakingCorridors, FixingWalls,
        Saving, Completed }

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

        public int CompareTo(ExternalDirection other)
        {
            if (other.preference > preference) return -1;
            else if (other.preference < preference) return 1;
            else return 0;
        }
    }
}
