using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Generator;
using UnityEngine.SocialPlatforms.Impl;

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
    private float length = 0;
    private Vector2 orientation;

    public Vector2 Orientation { get => orientation;}
    public float Length { get => length;}

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

    public void Calculate()
    {
        foreach(Edge edge in edges)
        {
            length += edge.GetLength();
        }

        // direction to outside
        orientation = (edges[0].transform.position - edges[0].faces[0].transform.position).normalized;
    }

    public int CompareTo(ExternalWall other)
    {
        if (other.length < length) return -1;
        else if (other.length > length) return 1;
        else return 0;
    }


}


