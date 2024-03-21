using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face 
{
    public List<Edge> edges = new List<Edge>();

    public Face(List<Edge> edges)
    {
        this.edges = edges;
        foreach (Edge e in edges)
        {
            e.faces.Add(this);
        }
    }
}
