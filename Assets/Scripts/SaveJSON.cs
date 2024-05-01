using System.Collections.Generic;
using UnityEngine;

public class SaveJSON : MonoBehaviour
{
    public void SaveIntoJson(List<Wall> walls, List<Room> rooms)
    {
        int vertexIndex = 0;
        int edgeIndex = 0;
        int faceIndex = 0;

        List<VertexJS> vertices = new List<VertexJS>();
        List<EdgeJS> edges = new List<EdgeJS>();
        List<FaceJS> faces = new List<FaceJS>();

        foreach(Wall wall in walls)
        {
            List<Vertex> ends = wall.FindEnds();

            if (ends.Count == 2)
            {
                wall.Recolor(Color.black);
                EdgeJS edge = new EdgeJS();

                edge.id = "e" + edgeIndex++;
                edge.w = wall;
                

                // save vertices
                foreach (Vertex end in ends)
                {
                    bool onList = false;

                    foreach (VertexJS vert in vertices)
                    {
                        if (vert.v == end)
                        {
                            // vertex already on list
                            edge.vertex_ids.Add(vert.id);
                            onList = true;
                            break;
                        }
                    }

                    if (onList) continue;

                    // vertex not yet on list
                    VertexJS vertex = new VertexJS();
                    vertex.id = "v" + vertexIndex++;
                    vertex.v = end;
                    vertex.x = end.transform.position.x;
                    vertex.y = end.transform.position.y;

                    vertices.Add(vertex);
                    edge.vertex_ids.Add(vertex.id);
                }

                edges.Add(edge);
            } else
            {
                Debug.Log("not found ends");
            }
        }

        foreach(Room room in rooms)
        {
            List<EdgeJS> roomEdges = new List<EdgeJS>();

            FaceJS face = new FaceJS();
            face.r = room;
            face.id = "f" + faceIndex++;

            foreach(EdgeJS edge in edges)
            {
                if (room.roomWalls.Contains(edge.w))
                {
                    face.edge_ids.Add(edge.id);
                    roomEdges.Add(edge);
                }
            }

            Debug.Log(room.roomWalls.Count + " / " + edges.Count);

            face.FindOrder(roomEdges);
            faces.Add(face);
        }

        GeometryJS geometry = new GeometryJS();
        geometry.vertices = vertices;
        geometry.edges = edges;
        geometry.faces = faces;
        string json = JsonUtility.ToJson(geometry);

        System.IO.File.WriteAllText(Application.persistentDataPath + "/next_geometry.json", json);

    }
}

[System.Serializable]
public class GeometryJS
{
    public List<VertexJS> vertices = new List<VertexJS>();
    public List<EdgeJS> edges = new List<EdgeJS>();
    public List<FaceJS> faces = new List<FaceJS>();
}

[System.Serializable]
public class SpaceJS
{
    public string id;
    public string name;
    public string face_id;
}

[System.Serializable]
public class VertexJS
{
    public string id;
    public float x, y;
    [System.NonSerialized] public Vertex v;
}

[System.Serializable]
public class EdgeJS
{
    public string id;
    public List<string> vertex_ids = new List<string>();
    [System.NonSerialized] public Wall w;
    [System.NonSerialized] public bool used = false;
}

[System.Serializable]
public class FaceJS
{
    public string id;
    public List<string> edge_ids = new List<string>();
    public List<int> edge_order = new List<int>();
    [System.NonSerialized] public Room r;

    public void FindOrder(List<EdgeJS> edges)
    {
        List<string> edgeIds = new List<string>();

        // pick any edge to start
        EdgeJS currentEdge = edges[0];
        bool found = false;
        string current, last;

        // start with edge already passed, if there are any
        foreach (EdgeJS edge in edges)
        {
            if (edge.used) { currentEdge = edge; found = true; break; }
        }

        if (found)
        {
            current = currentEdge.vertex_ids[0];
            last = currentEdge.vertex_ids[1];

            edge_order.Add(0);
            edgeIds.Add(currentEdge.id);
        }
        else
        {
            current = currentEdge.vertex_ids[1];
            last = currentEdge.vertex_ids[0];

            edge_order.Add(1);
            edgeIds.Add(currentEdge.id);
        }

        

        int counter = 0;

        while(current != last)
        {
            counter++;

            if (counter > 200)
            {
                Debug.Log("cant find order"); break;
            }

            foreach(EdgeJS edge in edges)
            {
                if (edge != currentEdge && edge.vertex_ids.Contains(current))
                {
                    currentEdge = edge;
                    int index = edge.vertex_ids.IndexOf(current);
                    if (index == 0) edge_order.Add(1);
                    else edge_order.Add(0);
                    edgeIds.Add(currentEdge.id);
                    if (index == 0) current = edge.vertex_ids[1];
                    else current = edge.vertex_ids[0];
                    break;
                }
            }
        }

        edge_ids = edgeIds;
    }
}