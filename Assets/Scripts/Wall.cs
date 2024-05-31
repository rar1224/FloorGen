using System;
using System.Collections.Generic;
using UnityEngine;

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
            foreach (Edge edge1 in edges)
            {
                if (edge1.FindCommonVertex(edge) != null)
                {
                    edges.Add(edge);
                    return true;
                }
            }
            return false;

        }
        else
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
        foreach (Edge edge in edges)
        {
            foreach (Edge edge1 in edge.Vertex1.edges)
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

        if (ends.Count == 3)
        {
            ends.RemoveAt(1);
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
        door.transform.position = v.transform.position + dir * (length / 2);

        objects.Add(door.gameObject);

        return door;
    }



    public void Recolor(Color color)
    {
        foreach (Edge edge in edges)
        {
            edge.Recolor(color);
        }
    }
}
