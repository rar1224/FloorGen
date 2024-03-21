using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge : MonoBehaviour
{
    public Vertex Vertex1 { get; set; }
    public Vertex Vertex2 { get; set; }

    public List<Face> faces = new List<Face>();

    public Vector3 Direction { get; set; }

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
        Vector3 directionVector = Vertex1.transform.position - Vertex2.transform.position;
        transform.localScale = new Vector3(directionVector.magnitude, 0.1f, 1);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.right, directionVector);
        transform.rotation = rotation;
        Direction = directionVector;
    }

    public Vertex FindCommonVertex(Edge other)
    {
        if (Vertex1 == other.Vertex1 || Vertex1 == other.Vertex2) return Vertex1;
        else if (Vertex2 == other.Vertex1 || Vertex2 == other.Vertex2) { return Vertex2; }
        else return null;
    }

    public void DivideEdge(Vertex divider)
    {
        Edge other = Instantiate(this);
        other.Vertex1 = divider;
        other.Vertex2 = Vertex2;
        other.UpdatePosition();

        this.Vertex2 = divider;
        Vertex2.edges.Remove(this);
        Vertex2.edges.Add(other);
        this.UpdatePosition();

        divider.edges.Add(this);
        divider.edges.Add(other);

        foreach(Face face in faces)
        {
            face.edges.Add(other);
        }
    }
}
