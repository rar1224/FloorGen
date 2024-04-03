using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
