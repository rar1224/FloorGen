using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public float rectanglePreference;
    public float exteriorPreference;
    public Vector2 preferredDirection;
    public List<Face> faces;

    public Room(float rectanglePreference, float exteriorPreference, Vector2 preferredDirection, Face face)
    {
        this.rectanglePreference = rectanglePreference;
        this.exteriorPreference = exteriorPreference;
        this.preferredDirection = preferredDirection;
        faces = new List<Face>{face};
        face.GetComponent<Renderer>().material.color = Color.white;
    }
}
