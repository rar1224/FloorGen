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
    public bool preferRectangular;
    public Color color;

    public bool passedMinArea = false;
    public bool isRectangular = false;
    public bool finished = false;

    public List<Face> faces;
    private float currentArea = 0;

    public Room(float exteriorPreference, Vector2 preferredDirection, float minArea, bool preferRectangular, Face face, Color color)
    {
        this.exteriorPreference = exteriorPreference;
        this.preferredDirection = preferredDirection;
        this.minArea = minArea;
        this.preferRectangular = preferRectangular;

        faces = new List<Face>();
        face.SetRoom(this);
        
        this.color = color;
        face.GetComponent<Renderer>().material.color = color;
    }

    public void AddFace(Face face)
    {
        faces.Add(face);
        face.room = this;
        face.Recolor();
        currentArea += face.GetArea();

        if (currentArea >= minArea) passedMinArea = true;
        if (preferRectangular) isRectangular = IsRectangular();
        if (isRectangular && passedMinArea) finished = true;
    }

    public bool IsRectangular()
    {
        // check rows
        Dictionary<float, int> rows = new Dictionary<float, int>();
        Dictionary<float, int> columns = new Dictionary<float, int>();

        foreach (Face face in faces)
        {
            if (rows.ContainsKey(face.transform.position.y)) rows[face.transform.position.y]++;
            else rows.Add(face.transform.position.y, 1);

            if (columns.ContainsKey(face.transform.position.x)) columns[face.transform.position.x]++;
            else columns.Add(face.transform.position.x, 1);
        }

        for(int i = 0; i < rows.Count - 1; i++) { if (rows.ElementAt(i).Value != rows.ElementAt(i + 1).Value) return false; }
        for (int i = 0; i < columns.Count - 1; i++) { if (columns.ElementAt(i).Value != columns.ElementAt(i + 1).Value) return false; }

        return true;
    }
}

public class ExternalWall : IComparable<ExternalWall>
{
    public List<Edge> edges;
    public List<GameObject> objects = new List<GameObject>();

    private float length = 0;
    private Vector2 orientation;

    private int maxWindowsNumber = 0;
    public int windowsNumber = 0;
    private bool hasDoor = false;
    private float gap = 0;

    public Vector2 Orientation { get => orientation;}
    public float Length { get => length;}
    public int MaxWindowsNumber { get => maxWindowsNumber; set => maxWindowsNumber = value; }

    public ExternalWall()
    {
        this.edges = new List<Edge>();
    }

    public bool IsAligned(Edge edge)
    {
        if (edges.Count == 0) return true;
        else if (edges[0].Direction == edge.Direction || edges[0].Direction == -edge.Direction) return true;
        else return false;
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

    public int CompareTo(ExternalWall other)
    {
        if (other.length < length) return -1;
        else if (other.length > length) return 1;
        else return 0;
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


