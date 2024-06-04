using System.Collections.Generic;
using UnityEngine;

public class ExternalWall : Wall
{
    public int maxWindowsNumber = 0;
    public int windowsNumber = 0;
    private float gap = 0;

    public ExternalWall()
    {
        this.edges = new List<Edge>();
    }



    public void Calculate(float windowWidth, float gap)
    {
        Calculate();

        // max number of windows
        maxWindowsNumber = (int)((length - gap) / (windowWidth + gap));
    }



    public GameObject SetupDoor(float doorWidth, float windowWidth, float gap, GameObject doorPrefab)
    {
        this.gap = gap;

        Edge origin = edges[0];
        GameObject door = UnityEngine.Object.Instantiate(doorPrefab);
        door.transform.localScale = new Vector3(doorWidth, 0.2f, 1);
        door.transform.rotation = origin.transform.rotation;
        objects.Add(door);

        // recalculate max number of windows
        maxWindowsNumber = (int)((length - door.transform.localScale.x - gap * 2) / (windowWidth + gap));

        return door;
    }

    public List<GameObject> SetupWindows(float windowWidth, GameObject windowPrefab)
    {
        // fill the wall with proper number of windows, but don't position them yet
        List<GameObject> windows = new List<GameObject>();

        for (int i = 0; i < windowsNumber; i++)
        {
            Edge origin = edges[0];
            GameObject window = UnityEngine.Object.Instantiate(windowPrefab);
            window.transform.localScale = new Vector3(windowWidth, 0.2f, 1);
            window.transform.rotation = origin.transform.rotation;

            objects.Add(window);
            windows.Add(window);
        }

        return windows;
    }
    public void SetupAll()
    {
        // position everything 
        // check how much space will be left for gaps
        float emptySpace = length;

        //Debug.Log(objects.Count);

        foreach (GameObject obj in objects)
        {
            emptySpace -= obj.transform.localScale.x;
        }

        // divide it between objects
        float correctGap = emptySpace / (objects.Count + 1);

        Vector3 pos = edges[0].Vertex1.transform.position;
        Vector3 dir = edges[0].Direction;

        // place everything
        foreach (GameObject obj in objects)
        {
            pos += dir * (correctGap + obj.transform.localScale.x / 2);
            obj.transform.transform.position = pos;
            pos += dir * (obj.transform.localScale.x / 2);
        }
    }

    // connect faces that share a window or door
    public void ConnectSharingCells()
    {
        foreach (GameObject obj in objects)
        {
            List<Face> faces = new List<Face>();
            List<Collider2D> colliders = new List<Collider2D>();

            Physics2D.OverlapCollider(obj.GetComponent<Collider2D>(), new ContactFilter2D().NoFilter(), colliders);

            // check which edges overlap with the window/door
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject.tag == "Edge")
                {
                    Edge edge = collider.gameObject.GetComponent<Edge>();
                    if (edges.Contains(edge))
                    {
                        faces.Add(edge.faces[0]);
                    }
                }
            }

            foreach (Face face in faces)
            {
                // Debug

                foreach (Face other in faces)
                {
                    if (face == other) break;
                    if (obj.tag == "Window") face.ConnectToFace(other);
                    else face.ConnectToFace(other, true);
                }
            }
        }
    }

    public void RemoveExteriorObjects()
    {
        objects.Clear();

        foreach (Edge edge in edges)
        {
            edge.faces[0].connectedFaces.Clear();
            edge.faces[0].hasFrontDoor = false;
            edge.faces[0].hasWindows = false;
        }
        windowsNumber = 0;
        door = null;
    }

}
