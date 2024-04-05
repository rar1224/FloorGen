using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Room
{
    public float rectanglePreference;
    public float exteriorPreference;
    public Vector2 preferredDirection;
    public List<Face> faces;
    public Color color;

    public Room(float rectanglePreference, float exteriorPreference, Vector2 preferredDirection, Face face, Color color)
    {
        this.rectanglePreference = rectanglePreference;
        this.exteriorPreference = exteriorPreference;
        this.preferredDirection = preferredDirection;
        faces = new List<Face> { face };
        face.room = this;
        
        this.color = color;
        face.GetComponent<Renderer>().material.color = color;
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

}


