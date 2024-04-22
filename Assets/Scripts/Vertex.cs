using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Vertex : MonoBehaviour
{
    public List<Edge> edges = new List<Edge>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsConnectedTo(Vertex other)
    {
        foreach (Edge e in edges)
        {
            if (e.Vertex1 == other) return true;
            else if (e.Vertex2 == other) return true;
        }

        return false;
    }

    public Edge FindNextExternalEdge(Edge edge)
    {
        foreach(Edge e in edges)
        {
            if (e.IsExterior)
            {
                if(e != edge) return e;
            }
        }
        return null;
    }

    public List<Edge> GetNextInteriorEdges(Edge edge)
    {
        List<Edge> interiorEdges = new List<Edge>();

        foreach (Edge e in edges) { 
            if (e.IsInteriorWall && e != edge)
                interiorEdges.Add(e);
        }

        return interiorEdges;
    }

    public bool FindInteriorWallPath(Vertex end, List<Edge> passedWalls)
    {
        foreach (Edge e in edges)
        {
            if (!e.IsInteriorWall || passedWalls.Contains(e)) continue;
            else
            {
                passedWalls.Add(e);
                if (e.Vertex1 == end || e.Vertex2 == end) return true;
                else if (e.Vertex1.FindInteriorWallPath(end, passedWalls) ||
                    e.Vertex2.FindInteriorWallPath(end, passedWalls))
                {
                    e.Recolor(Color.red);
                    return true;
                }
            }
        }

        return false;
    }

    public void Recolor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }


}
