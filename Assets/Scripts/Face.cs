using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : MonoBehaviour { 
    public List<Edge> edges = new List<Edge>();
    public List<Vertex> vertices = new List<Vertex>();

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
        this.GetComponent<Renderer>().material.color = Random.ColorHSV();

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
}
