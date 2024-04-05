using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : MonoBehaviour { 
    public List<Edge> edges = new List<Edge>();
    public List<Vertex> vertices = new List<Vertex>();
    public Room room;

    public List<Face> connectedFaces = new List<Face>();
    private bool hasWindows = false;
    private bool hasFrontDoor = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void MakeVisible()
    {
        this.GetComponent<Renderer>().enabled = true;
        Vector3 position = Vector3.zero;
        float scaleX = 1, scaleY = 1;

        foreach (Edge edge in edges)
        {
            position += edge.transform.position;
            if (edge.Direction == Vector2.left || edge.Direction == Vector2.right) scaleX = edge.GetLength();
            else if (edge.Direction == Vector2.up || edge.Direction == Vector2.down) scaleY = edge.GetLength();
        }

        transform.position = position / 4;
        transform.localScale = new Vector3 (scaleX, scaleY, 0);
        this.GetComponent<Renderer>().material.color = Color.black;

    }

    public void SetEdges(List<Edge> edges)
    {
        this.edges = edges;

        foreach(Edge edge in edges)
        {
            edge.faces.Add(this);
        }
    }

    public float GetArea()
    {
        return transform.localScale.x * transform.localScale.y;
    }

    public Edge GetEdgeInDirection(Vector3 direction)
    {
        foreach(Edge edge in edges)
        {
            if ((edge.transform.position - transform.position).normalized == direction) return edge;
        }
        return null;
    }

    // Debug
    public void Recolor()
    {
        GetComponent<Renderer>().material.color = room.color;
    }

    public void Recolor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }

    public bool IsExteriorAdjacent()
    {
        foreach (Edge edge in edges)
        {
            if (edge.IsExterior == true) return true;
        }
        return false;
    }

    public void ConnectToFace(Face face, bool forFrontDoor = false)
    {
        connectedFaces.Add(face);
        face.connectedFaces.Add(this);
        if (forFrontDoor) { hasFrontDoor = true; face.hasFrontDoor = true; }
        else { hasWindows = true; face.hasWindows = true; }

        
    }

    public bool IsConnectedTo(Face face)
    {
        return connectedFaces.Contains(face);
    }

    public void SetRoom(Room room)
    {
        room.faces.Add(this);
        this.room = room;
        Recolor();

        foreach (Face face in connectedFaces)
        {
            room.faces.Add(face);
            face.room = room;
            face.Recolor();
        }
    }

}
