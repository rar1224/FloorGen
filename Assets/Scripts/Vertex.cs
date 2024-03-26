using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
