using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Room
{
    public float exteriorPreference;
    public Vector2 preferredDirection;
    public float minArea;
    public Color color;

    public bool passedMinArea = false;
    public bool isRectangular = false;
    public bool finished = false;
    public Vector2 lastDirection = Vector2.zero;

    public List<Face> faces;
    private float currentArea = 0;
    public int currentRows = 0;
    public int currentCols = 0;

    public List<Wall> roomWalls;
    public List<Edge> roomEdges;

    public Room(float minArea, Face face, Color color)
    {
        this.minArea = minArea;

        this.color = color;
        face.GetComponent<Renderer>().material.color = color;

        faces = new List<Face>();
        face.SetRoom(this, Vector2.zero);

        this.roomEdges = new List<Edge>();
    }

    public void AddFace(Face face, Vector2 dir)
    {
        lastDirection = dir;
        if (faces.Contains(face)) return;

        faces.Add(face);
        face.room = this;
        face.Recolor(color);
        currentArea += face.GetArea();

        // Debug
        //if (faces.Count > 100) finished = true;

        if (currentArea >= minArea) passedMinArea = true;
        isRectangular = IsRectangular();
        if (isRectangular && passedMinArea) finished = true;
        //if (!isRectangular) finished = true;
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

        for(int i = 0; i < rows.Count - 1; i++) { if (rows.ElementAt(i).Value != rows.ElementAt(i + 1).Value) return false; }
        for (int i = 0; i < columns.Count - 1; i++) { if (columns.ElementAt(i).Value != columns.ElementAt(i + 1).Value) return false; }

        currentRows = rows.Count;
        currentCols = columns.Count;

        return true;
    }

    public int GetElementsNumber(Vector2 direction)
    {
        if (direction == Vector2.left || direction == Vector2.right) return currentRows;
        else return currentCols;
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
        roomWalls = new List<Wall>();
        roomEdges = new List<Edge>();

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

        Wall wall = new Wall();
        Wall currentWall = wall;

        currentWall.edges.Add(currentEdge);

        
        while (next != origin)
        {
            foreach (Edge edge in next.edges)
            {
                if (edge != currentEdge && roomEdges.Contains(edge))
                {
                    currentEdge = edge;
                    next = currentEdge.GetOtherVertex(next);

                    Room otherRoom = currentEdge.GetOtherRoom(this);
                    if (passedRooms.Contains(otherRoom)) break;

                    // still the same wall
                    if (currentWall.IsAligned(edge)
                        && otherRoom == currentOtherRoom)
                    {
                        currentWall.edges.Add(currentEdge);

                    } else
                    {
                        currentOtherRoom = otherRoom;
                        currentEdge.Recolor(Color.red);

                        Wall newWall = new Wall();
                        newWall.edges.Add(currentEdge);

                        roomWalls.Add(currentWall);
                        currentWall = newWall;
                    }
                    break;
                }
            }
        }
        

        //currentWall.edges.Add(currentEdge);
        roomWalls.Add(currentWall);
        Debug.Log(roomWalls.Count);

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

    public float GetArea()
    {
        return currentArea;
    }

}

public class Wall : IComparable<Wall>
{
    public List<Edge> edges;
    protected float length = 0;
    protected Vector2 orientation;

    private List<Room> rooms;

    public Vector2 Orientation { get => orientation; }
    public float Length { get => length; }

    public Wall()
    {
        this.edges = new List<Edge>();
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

    public bool IsAligned(Edge edge)
    {
        if (edges.Count == 0) return true;
        else if (edges[0].Direction == edge.Direction || edges[0].Direction == -edge.Direction) return true;
        else return false;
    }

}

public class ExternalWall : Wall
{ 
    public List<GameObject> objects = new List<GameObject>();

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
        // length of wall
        foreach(Edge edge in edges)
        {
            length += edge.GetLength();
        }

        // direction to outside
        orientation = (edges[0].transform.position - edges[0].faces[0].transform.position).normalized;

        // max number of windows
        maxWindowsNumber = (int)((length - gap) / (windowWidth + gap));
    }



    public void SetupDoor(float doorWidth, float windowWidth, float gap, GameObject doorPrefab)
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
    }

    public void SetupWindows(float windowWidth, GameObject windowPrefab)
    {
        // fill the wall with proper number of windows, but don't position them yet

        for (int i = 0; i < windowsNumber; i++)
        {
            Edge origin = edges[0];
            GameObject window = UnityEngine.Object.Instantiate(windowPrefab);
            window.transform.localScale = new Vector3(windowWidth, 0.2f, 1);
            window.transform.rotation = origin.transform.rotation;
            objects.Add(window);
        }
    }
    public void SetupAll()
    {
        // position everything 
        // check how much space will be left for gaps
        float emptySpace = length;

        Debug.Log(objects.Count);

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


