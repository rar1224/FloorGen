using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
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

    public float GetCellRatio()
    {
        IsRectangular();

        if (currentCols > currentRows) return (float)currentRows / (float)currentCols;
        else return (float)currentCols / (float)currentRows;
    }

    public float GetRatio()
    {
        float vertLength = 0;
        float horLength = 0;

        foreach(Wall wall in roomWalls)
        {
            if (wall.Orientation == Vector2.down || wall.Orientation == Vector2.up) vertLength += wall.Length;
            else horLength += wall.Length;
        }

        if (vertLength > horLength) return horLength / vertLength;
        else return vertLength / horLength;
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
            foreach (Edge edge in shareOrigin[1].edges)
            {
                edge.wall = shareOrigin[0];
                if (!shareOrigin[0].edges.Contains(edge))
                {
                    shareOrigin[0].edges.Add(edge);
                }
            }
                
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

    public List<Wall> ShortestWallPath(Room room, int maximum)
    {
        List<Wall> shortestWallPath = null;

        foreach(Wall wall in roomWalls)
        {
            List<Wall> path = new List<Wall>();

            List<Wall> shortest = wall.WallDistance(room, path, maximum);

            if (shortestWallPath == null) shortestWallPath = shortest;
            else
            {
                if (shortest.Count < shortestWallPath.Count) shortestWallPath = shortest;
            }
        }

        return shortestWallPath;
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






