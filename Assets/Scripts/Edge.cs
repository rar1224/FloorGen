using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge : MonoBehaviour
{
    public Vertex Vertex1 { get; set; }
    public Vertex Vertex2 { get; set; }

    public List<Face> faces = new List<Face>();

    public Vector2 Direction { get; set; }

    public bool IsExterior = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePosition()
    {
        transform.position = (Vertex1.transform.position + Vertex2.transform.position) / 2;
        Vector3 directionVector = Vertex2.transform.position - Vertex1.transform.position;
        transform.localScale = new Vector3(directionVector.magnitude, 0.1f, 1);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.right, directionVector);
        transform.rotation = rotation;
        Direction = directionVector.normalized;
    }

    public Vertex FindCommonVertex(Edge other)
    {
        if (Vertex1 == other.Vertex1 || Vertex1 == other.Vertex2) return Vertex1;
        else if (Vertex2 == other.Vertex1 || Vertex2 == other.Vertex2) { return Vertex2; }
        else return null;
    }

    public Edge DivideEdge(Vertex divider)
    {
        Edge other = Instantiate(this);
        other.Vertex1 = divider;
        other.Vertex2 = Vertex2;
        other.UpdatePosition();

        Vertex2.edges.Remove(this);
        Vertex2.edges.Add(other);

        this.Vertex2 = divider;
        this.UpdatePosition();

        divider.edges.Add(this);
        divider.edges.Add(other);

        if (IsExterior) other.IsExterior = true;

        return other;
    }

    public Edge DivideEdgeReverse(Vertex divider)
    {
        Edge other = Instantiate(this);
        other.Vertex2 = divider;
        other.Vertex1 = Vertex1;
        other.UpdatePosition();

        Vertex1.edges.Remove(this);
        Vertex1.edges.Add(other);

        this.Vertex1 = divider;
        this.UpdatePosition();

        divider.edges.Add(this);
        divider.edges.Add(other);

        if (IsExterior) other.IsExterior = true;

        return other;
    }

    public float GetLength()
    {
        return transform.localScale.x;
    }

    public Vertex GetOtherVertex(Vertex vertex)
    {
        if (Vertex1 == vertex) return Vertex2;
        else if (Vertex2 == vertex) return Vertex1;
        else return null;
    }

    public Face GetOtherFace(Face face)
    {
        if (faces.Count < 2) return null;
        if (faces[0] == face) return faces[1];
        else if (faces[1] == face) return faces[0];
        else return null;
    }

    public Vector2 GetDirectionFrom(Vertex origin)
    {
        if (origin == Vertex1) return Direction;
        else return -Direction;
    }
}
