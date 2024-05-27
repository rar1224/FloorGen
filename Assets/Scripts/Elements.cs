using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class Room : IComparable<Room>
{
    public float exteriorPreference;
    public Vector2 preferredDirection;
    public float minArea;
    public Color color;
    public bool corridor = false;
    public float area;
    public float wallLength;

    public bool isRectangular = false;
    public bool finished = false;
    public Vector2 lastDirection = Vector2.zero;
    public string name;

    public List<Face> faces;
    public int currentRows = 0;
    public int currentCols = 0;

    public List<Wall> roomWalls = new List<Wall>();
    public List<Edge> roomEdges = new List<Edge>();

    public Dictionary<Vector2, int> outOrientations = new Dictionary<Vector2, int>();

    public Room(float minArea, Face face, Color color)
    {
        this.minArea = minArea;

        this.color = color;

        faces = new List<Face>();
        face.SetRoom(this, Vector2.zero);

        GetArea();

        this.roomEdges = new List<Edge>();
    }

    public Room(List<Face> faces, Color color)
    {
        this.faces = faces;
        this.color = color;
        
        foreach(Face face in faces)
        {
            face.room.faces.Remove(face);
            face.room = this;
            face.Recolor(color);
        }

        GetArea();
    }

    public void AddFace(Face face, Vector2 dir)
    {
        lastDirection = dir;
        if (faces.Contains(face)) return;

        faces.Add(face);
        face.room = this;
        face.Recolor(color);

        // Debug
        //if (faces.Count > 100) finished = true;

        if (GetArea() >= minArea) finished = true;
        isRectangular = IsRectangular();
        //if (!isRectangular) finished = true;
    }
    public int CompareTo(Room other)
    {
        if (other.area > area) return -1;
        else if (other.area < area) return 1;
        else return 0;
    }

    public bool IsRectangular()
    {
        // check rows
        Dictionary<float, int> rows = new Dictionary<float, int>();
        Dictionary<float, int> columns = new Dictionary<float, int>();

        foreach (Face face in faces)
        {
            bool newRow = true, newCol = true;

            for (int i = 0; i < rows.Count; i++)
            {
                if (Mathf.Abs(rows.ElementAt(i).Key - face.transform.position.y) < 0.001)
                {
                    rows[rows.ElementAt(i).Key]++;
                    newRow = false;
                    break;
                }
            }
            if (newRow) rows.Add(face.transform.position.y, 1);

            for (int i = 0; i < columns.Count; i++)
            {
                if (Mathf.Abs(columns.ElementAt(i).Key - face.transform.position.x) < 0.001)
                {
                    columns[columns.ElementAt(i).Key]++;
                    newCol = false;
                    break;
                }
            }
            if (newCol) columns.Add(face.transform.position.x, 1);
        }

        currentRows = rows.Count;
        currentCols = columns.Count;

        for (int i = 0; i < rows.Count - 1; i++) { if (rows.ElementAt(i).Value != rows.ElementAt(i + 1).Value) return false; }
        for (int i = 0; i < columns.Count - 1; i++) { if (columns.ElementAt(i).Value != columns.ElementAt(i + 1).Value) return false; }

        return true;
    }

    public int GetElementsNumber(Vector2 direction)
    {
        if (direction == Vector2.left || direction == Vector2.right) return currentRows;
        else return currentCols;
    }

    public float GetRatio()
    {
        IsRectangular();

        if (currentCols > currentRows) return (float) currentRows / (float) currentCols;
        else return (float) currentCols / (float) currentRows;
    }

    public float CalculateNewSquareScore(List<Face> nextFaces)
    {
        IsRectangular();

        List<Face> allPossibleFaces = new List<Face>();
        allPossibleFaces.AddRange(faces);
        allPossibleFaces.AddRange(nextFaces);

        // check rows
        Dictionary<float, int> rows = new Dictionary<float, int>();
        Dictionary<float, int> columns = new Dictionary<float, int>();

        foreach (Face face in allPossibleFaces)
        {
            bool newRow = true, newCol = true;

            for (int i = 0; i < rows.Count; i++)
            {
                if (Mathf.Abs(rows.ElementAt(i).Key - face.transform.position.y) < 0.001)
                {
                    rows[rows.ElementAt(i).Key]++;
                    newRow = false;
                    break;
                }
            }
            if (newRow) rows.Add(face.transform.position.y, 1);

            for (int i = 0; i < columns.Count; i++)
            {
                if (Mathf.Abs(columns.ElementAt(i).Key - face.transform.position.x) < 0.001)
                {
                    columns[columns.ElementAt(i).Key]++;
                    newCol = false;
                    break;
                }
            }
            if (newCol) columns.Add(face.transform.position.x, 1);
        }

        // caluclate score

        float currentRatio = Math.Abs(currentCols / currentRows - 1);
        float possibleRatio = Math.Abs(columns.Count / rows.Count - 1);

        return currentRatio - possibleRatio;
    }

    public List<Wall> FindWalls(List<Room> passedRooms)
    {
        if (faces.Count == 0) return new List<Wall>();

        foreach (Face face in faces)
        {
            foreach (Edge edge in face.edges)
            {
                if (edge.IsBetweenRooms() && !roomEdges.Contains(edge))
                {
                    edge.Recolor(Color.red);
                    roomEdges.Add(edge);
                }
            }
        }

        Vertex origin = roomEdges[0].Vertex1;
        Vertex next = roomEdges[0].Vertex2;

        Edge currentEdge = roomEdges[0];
        Room currentOtherRoom = currentEdge.GetOtherRoom(this);

        Wall wall = new Wall(this, currentOtherRoom, currentEdge.Direction);
        Wall currentWall = wall;

        currentWall.edges.Add(currentEdge);
        currentEdge.wall = currentWall;

        int counter = 0;

        bool paused = false;

        while (next != origin)
        {
            if (counter > 200)
            {
                Debug.Log("can't find walls");
                return null;
            }

            counter++;
            foreach (Edge edge in next.edges)
            {
                if (edge != currentEdge && roomEdges.Contains(edge))
                {
                    currentEdge = edge;
                    next = currentEdge.GetOtherVertex(next);

                    Room otherRoom = currentEdge.GetOtherRoom(this);
                    if (otherRoom != null && passedRooms.Contains(otherRoom))
                    {
                        
                        if (!paused)
                        {
                            AddWall(currentWall, currentOtherRoom);
                            currentEdge.Recolor(Color.red);
                        }
                        
                        currentOtherRoom = otherRoom;
                        paused = true;
                        break;
                    }

                    // still the same wall
                    if (currentWall.IsAligned(edge)
                        && otherRoom == currentOtherRoom)
                    {
                        currentWall.edges.Add(currentEdge);
                        currentEdge.wall = currentWall;

                    } else
                    {
                        if (!paused)
                        {
                            // wall finished
                            AddWall(currentWall, currentOtherRoom);
                            currentEdge.Recolor(Color.red);
                        }

                        paused = false;
                        // start new wall

                        //if (next == origin) break;

                        currentOtherRoom = otherRoom;
                        Wall newWall = new Wall(this, currentOtherRoom, currentEdge.Direction);
                        newWall.edges.Add(currentEdge);
                        currentEdge.wall = newWall;

                        currentWall = newWall;
                    }
                    break;
                }
            }
        }


        //currentWall.edges.Add(currentEdge);
        if (!roomWalls.Contains(currentWall)) AddWall(currentWall, currentOtherRoom);
        //Debug.Log(roomWalls.Count);

        // combine walls that share origin if there is more than 1

        List<Wall> shareOrigin = new List<Wall>();
        foreach (Wall w in roomWalls)
        {
            if (w.FindEnds().Contains(origin)) shareOrigin.Add(w);
        }

        if (shareOrigin.Count > 1 && shareOrigin[0].IsAligned(shareOrigin[1].edges[0]) && shareOrigin[1].GetOtherRoom(this) == shareOrigin[0].GetOtherRoom(this))
        {
            // combine walls into 1
            foreach (Edge edge in shareOrigin[1].edges) edge.wall = shareOrigin[0];
            shareOrigin[0].edges.AddRange(shareOrigin[1].edges);
                
            RemoveWall(shareOrigin[1], shareOrigin[1].GetOtherRoom(this));
        }

        wallLength = 0f;

        foreach(Wall w in roomWalls)
        {
            w.Calculate();
            wallLength += w.Length;
        }

        return roomWalls;

        /*
        foreach(Face face in faces)
        {
            foreach(Edge edge in face.edges)
            {
                if (edge.IsBetweenRooms() && !edges.Contains(edge))
                {
                    edges.Add(edge);
                    roomEdges.Add(edge);
                    Face otherFace = edge.GetOtherFace(face);
                    
                    if (otherFace != null && otherFace.room != null ) otherFace.room.roomEdges.Add(edge);

                    edge.IsInteriorWall = true;
                    edge.Recolor(Color.yellow);
                }
            }
        }
        */
    }

    public void AddWall(Wall wall, Room otherRoom)
    {
        roomWalls.Add(wall);
        if (otherRoom != null) otherRoom.roomWalls.Add(wall);
    }

    public void RemoveWall(Wall wall, Room otherRoom)
    {
        roomWalls.Remove(wall);
        if (otherRoom != null) otherRoom.roomWalls.Remove(wall);
    }

    public bool IsAdjacent(Room room)
    {
        foreach (Wall wall in roomWalls)
        {
            if (wall.rooms.Contains(room)) return true;
        }

        return false;
    }

    public List<Room> RoomDistance(Room room, List<Room> path, int maxDistance)
    {
        if (IsAdjacent(room))
        {
            path.Add(this);
            return path;
        }
        else
        {
            path.Add(this);

            List<Room> shortestPath = path;
            int minimum = maxDistance;

            if (shortestPath.Count > maxDistance)
            {
                Debug.Log("path not found");
                return null;
            }

            foreach (Wall wall in roomWalls)
            {
                Room otherRoom = wall.GetOtherRoom(this);

                if (otherRoom == null || path.Contains(otherRoom)) continue;
                else
                {
                    List<Room> currentPath = new List<Room>();
                    currentPath.AddRange(path);

                    List<Room> nextPath = otherRoom.RoomDistance(room, currentPath, maxDistance);
                    int distance = nextPath.Count;
                    if (distance < minimum) { minimum = distance; shortestPath = nextPath; }
                }
            }

            return shortestPath;
        }
    }   

    public float GetArea()
    {
        area = 0;

        foreach(Face face in faces)
        {
            area += face.GetArea();
        }

        return area;
    }

    public bool IsConnectedWith(Room room)
    {
        foreach(Wall wall in roomWalls)
        {
            if (wall.door != null && wall.door.rooms.Contains(room)) return true;
        }

        return false;
    }

    public Door PlaceInteriorDoor(Room room, float width, Door doorPrefab)
    {
        foreach (Wall wall in roomWalls)
        {
            if (wall.door == null && wall.rooms.Contains(room)) { return wall.PlaceInteriorDoor(width, doorPrefab); }
        }

        return null;
    }


    public List<Wall> GetWallsAdjacentTo(Room room)
    {
        List<Wall> walls = new List<Wall>();

        foreach(Wall wall in roomWalls)
        {
            if (wall.rooms.Contains(room)) walls.Add(wall);
        }

        return walls;
    }

    public void CalculateWallOrientation()
    {
        Dictionary<Vector2, int> directions = new Dictionary<Vector2, int> { { Vector2.right, 0 }, { Vector2.down, 0 }, { Vector2.left, 0 }, { Vector2.up, 0 } };

        foreach (Wall wall in roomWalls)
        {
            if (wall.GetOtherRoom(this) == null)
            {
                foreach (KeyValuePair<Vector2, int> keyValuePair in directions)
                {
                    if (keyValuePair.Key == wall.GetExteriorDirection())
                    {
                        directions[keyValuePair.Key] += wall.edges.Count;
                        break;
                    }
                }
            }
        }

        int count = 0;

        outOrientations = directions.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => count++);

        outOrientations.Remove(outOrientations.Keys.FirstOrDefault());
        outOrientations.Remove(outOrientations.Keys.FirstOrDefault());
    }

    public bool HasWindows()
    {
        foreach(Face face in faces)
        {
            if (face.hasWindows) return true;
        }

        return false;
    }

    public float GetExteriorWallLength()
    {
        float length = 0;

        foreach (Wall wall in roomWalls)
        {
            if (wall.edges[0].IsExterior) length += wall.Length;
        }

        return length;
    }

    public void Recolor(Color color)
    {
        foreach (Face face in faces) face.Recolor(color);
    }

}

public class Wall : IComparable<Wall>
{
    public List<Edge> edges;
    protected float length = 0;
    protected Vector2 orientation;

    public List<Room> rooms;
    public List<Wall> adjacentWalls = new List<Wall>();
    public Door door = null;

    public List<GameObject> objects = new List<GameObject>();

    public Vector2 Orientation { get => orientation; }
    public float Length { get => length; }

    public Wall()
    {
        this.edges = new List<Edge>();
    }

    public Wall(Room room1, Room room2, Vector2 orientation)
    {
        this.edges = new List<Edge>();
        rooms = new List<Room> { room1, room2 };
        this.orientation = orientation;
    }

    public Wall(Edge edge)
    {
        this.edges = new List<Edge>();
        edges.Add(edge);
        rooms = new List<Room> { edge.faces[0].room, edge.faces[1].room };
    }
    public int CompareTo(Wall other)
    {
        if (other.length < length) return -1;
        else if (other.length > length) return 1;
        else return 0;
    }

    public bool CheckFit(Edge edge)
    {
        Edge check = edges[0];

        if (!edges.Contains(edge) && (rooms.Contains(check.faces[0].room) && rooms.Contains(check.faces[1].room))
            && (edge.Direction == orientation || edge.Direction == -orientation))
        {
            foreach(Edge edge1 in edges)
            {
                if (edge1.FindCommonVertex(edge) != null)
                {
                    edges.Add(edge);
                    return true;
                }
            }
            return false;
            
        } else
        {
            return false;
        }
    }

    public Vector2 GetExteriorDirection()
    {
        return (edges[0].transform.position - edges[0].faces[0].transform.position).normalized;
    }

    public void Calculate()
    {
        float len = 0;
        // length of wall
        foreach (Edge edge in edges)
        {
            len += edge.GetLength();
        }

        length = len;

        orientation = (edges[0].transform.position - edges[0].faces[0].transform.position).normalized;
    }

    public bool IsAligned(Edge edge)
    {
        if (edges.Count == 0) return true;
        else if (edges[0].Direction == edge.Direction || edges[0].Direction == -edge.Direction) return true;
        else return false;
    }

    public Room GetOtherRoom(Room room)
    {
        return edges[0].GetOtherRoom(room);
    }

    public void FindAdjacentWalls()
    {
        foreach(Edge edge in edges)
        {
            foreach(Edge edge1 in edge.Vertex1.edges)
            {
                if (edge1.wall != this && !adjacentWalls.Contains(edge1.wall))
                {
                    adjacentWalls.Add(edge1.wall);
                }
            }

            foreach (Edge edge1 in edge.Vertex2.edges)
            {
                if (edge1.wall != this && !adjacentWalls.Contains(edge1.wall))
                {
                    adjacentWalls.Add(edge1.wall);
                }
            }
        }


    }


    public List<Wall> WallDistance(Room room, List<Wall> path, int maxDistance)
    {
        if (rooms.Contains(room))
        {
            path.Add(this);
            return path;
        }
        else
        {
            path.Add(this);

            List<Wall> shortestPath = path;
            int minimum = maxDistance;

            if (shortestPath.Count > maxDistance)
            {
                Debug.Log("path not found");
                return null;
            }

            foreach (Wall wall in adjacentWalls)
            {
                if (wall == null || path.Contains(wall)) continue;
                else
                {
                    List<Wall> currentPath = new List<Wall>();
                    currentPath.AddRange(path);

                    List<Wall> nextPath = wall.WallDistance(room, currentPath, maxDistance);
                    int distance = nextPath.Count;
                    if (distance < minimum) { minimum = distance; shortestPath = nextPath; }
                }
            }

            return shortestPath;
        }
    }

    public List<Face> GetFacesInDirection(Vector2 direction)
    {
        List<Face> faces = new List<Face>();

        foreach (Edge edge in edges)
        {
            Face face = edge.GetFaceInDirection(direction);
            if (face == null) return null;
            else faces.Add(face);
        }

        return faces;
    }

    public List<Vertex> FindEnds()
    {
        int count = edges.Count;
        List<Vertex> ends = new List<Vertex>();

        foreach (Edge edge in edges)
        {
            if (edge.Vertex1.IsEdgeOfWall(this) && !ends.Contains(edge.Vertex1)) ends.Add(edge.Vertex1);
            if (edge.Vertex2.IsEdgeOfWall(this) && !ends.Contains(edge.Vertex2)) ends.Add(edge.Vertex2);
        }

        return ends;
    }

    public Door PlaceInteriorDoor(float width, Door doorPrefab)
    {
        door = UnityEngine.Object.Instantiate(doorPrefab);
        door.wall = this;
        door.rooms = rooms;

        Calculate();
        Edge origin = edges[0];

        List<Vertex> ends = FindEnds();
        Vertex v = ends[0];
        Vector3 dir = (ends[1].transform.position - ends[0].transform.position).normalized;
        door.transform.localScale = new Vector3(width, 0.2f, 1);
        door.transform.rotation = origin.transform.rotation;
        door.transform.position = v.transform.position + dir * (length/2);

        objects.Add(door.gameObject);

        return door;
    }



    public void Recolor(Color color)
    {
        foreach(Edge edge in edges)
        {
            edge.Recolor(color);
        }
    }
}

public class ExternalWall : Wall
{ 
    private int maxWindowsNumber = 0;
    public int windowsNumber = 0;
    private bool hasDoor = false;
    private float gap = 0;



    public int MaxWindowsNumber { get => maxWindowsNumber; set => maxWindowsNumber = value; }

    public ExternalWall()
    {
        this.edges = new List<Edge>();
    }



    public void Calculate(float windowWidth, float gap)
    {
        Calculate();

        // max number of windows
        maxWindowsNumber = (int)((length - gap) / (windowWidth + gap));
    }



    public GameObject SetupDoor(float doorWidth, float windowWidth, float gap, GameObject doorPrefab)
    { 
        this.gap = gap;
        hasDoor = true;

        Edge origin = edges[0];
        GameObject door = UnityEngine.Object.Instantiate(doorPrefab);
        door.transform.localScale = new Vector3(doorWidth, 0.2f, 1);
        door.transform.rotation = origin.transform.rotation;
        objects.Add(door);

        // recalculate max number of windows
        maxWindowsNumber = (int) ((length - door.transform.localScale.x - gap * 2) / (windowWidth + gap));

        return door;
    }

    public List<GameObject> SetupWindows(float windowWidth, GameObject windowPrefab)
    {
        // fill the wall with proper number of windows, but don't position them yet
        List<GameObject> windows = new List<GameObject>();

        for (int i = 0; i < windowsNumber; i++)
        {
            Edge origin = edges[0];
            GameObject window = UnityEngine.Object.Instantiate(windowPrefab);
            window.transform.localScale = new Vector3(windowWidth, 0.2f, 1);
            window.transform.rotation = origin.transform.rotation;

            objects.Add(window);
            windows.Add(window);
        }

        return windows;
    }
    public void SetupAll()
    {
        // position everything 
        // check how much space will be left for gaps
        float emptySpace = length;

        //Debug.Log(objects.Count);

        foreach(GameObject obj in objects)
        {
            emptySpace -= obj.transform.localScale.x;
        }

        // divide it between objects
        float correctGap = emptySpace / (objects.Count + 1);

        Vector3 pos = edges[0].Vertex1.transform.position;
        Vector3 dir = edges[0].Direction;

        // place everything
        foreach (GameObject obj in objects)
        {
            pos += dir * (correctGap + obj.transform.localScale.x / 2);
            obj.transform.transform.position = pos;
            pos += dir * (obj.transform.localScale.x / 2);
        }
    }

    // connect faces that share a window or door
    public void ConnectSharingCells()
    {
        foreach (GameObject obj in objects)
        {
            List<Face> faces = new List<Face>();
            List<Collider2D> colliders = new List<Collider2D>();

            Physics2D.OverlapCollider(obj.GetComponent<Collider2D>(), new ContactFilter2D().NoFilter(), colliders);

            // check which edges overlap with the window/door
            foreach(Collider2D collider in colliders)
            {
                if (collider.gameObject.tag == "Edge")
                {
                    Edge edge = collider.gameObject.GetComponent<Edge>();
                    if (edges.Contains(edge))
                    {
                        faces.Add(edge.faces[0]);
                    }
                }
            }

            foreach(Face face in faces)
            {
                // Debug

                foreach(Face other in faces)
                {
                    if (face == other) break;
                    if (obj.tag == "Window") face.ConnectToFace(other);
                    else face.ConnectToFace(other, true);
                }
            }
        }
    }

}


