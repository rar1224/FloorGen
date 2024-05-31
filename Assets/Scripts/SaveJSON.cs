using System.Collections.Generic;
using UnityEngine;

public class SaveJSON : MonoBehaviour
{
    public void SaveIntoJson(List<Wall> walls, List<Room> rooms,
        float windowWidth, float frontDoorWidth)
    {
        int vertexIndex = 0;
        int edgeIndex = 0;
        int faceIndex = 0;
        int objIndex = 0;

        List<VertexJS> vertices = new List<VertexJS>();
        List<EdgeJS> edges = new List<EdgeJS>();
        List<FaceJS> faces = new List<FaceJS>();
        List<SpaceJS> spaces = new List<SpaceJS>();

        
        List<DoorJS> doors = new List<DoorJS>();
        List<WindowJS> windows = new List<WindowJS>();

        foreach(Wall wall in walls)
        {
            wall.Calculate();
            List<Vertex> ends = wall.FindEnds();

            if (ends.Count != 2)
            {
                wall.FindEnds();
            }

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

                // add doors and windows

                if (wall.objects.Count > 0)
                {
                    foreach(GameObject obj in wall.objects)
                    {
                        if (obj.tag == "FrontDoor")
                        {
                            DoorJS door = new DoorJS();
                            door.id = "o" + objIndex;
                            door.door_definition_id = "Door_0";
                            door.edge_id = edge.id;
                            door.name = "Door " + objIndex;
                            door.alpha = (obj.transform.position - ends[0].transform.position).magnitude / wall.Length;
                            doors.Add(door);
                            objIndex++;

                        }  else
                        {
                            WindowJS window = new WindowJS();
                            window.id = "o" + objIndex;
                            window.window_definition_id = "Window_0";
                            window.edge_id = edge.id;
                            window.name = "Window " + objIndex;
                            window.alpha = (obj.transform.position - ends[0].transform.position).magnitude / wall.Length;
                            windows.Add(window);
                            objIndex++;
                        }
                    }
                }
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
            face.id = "f" + faceIndex;

            SpaceJS space = new SpaceJS();
            space.id = "s" + faceIndex;
            space.name = "Space " + faceIndex;
            space.color = "#" + ColorUtility.ToHtmlStringRGB(room.color);
            space.face_id = face.id;

            switch(room.name)
            {
                case "living_room":
                    space.space_type_id = "Living_Room";
                    space.thermal_zone_id = "Living_Zone";
                    break;
                case "bathroom":
                    space.space_type_id = "Bathroom";
                    space.thermal_zone_id = "Bathroom_Zone";
                    break;
                case "bedroom":
                    space.space_type_id = "Bedroom";
                    space.thermal_zone_id = "Bedroom_Zone";
                    break;
                case "kitchen":
                    space.space_type_id = "Kitchen";
                    space.thermal_zone_id = "Kitchen_Zone";
                    break;
                default:
                    space.space_type_id = "Corridors";
                    space.thermal_zone_id = "Corridor_Zone";
                    break;
            }

            spaces.Add(space);

            faceIndex++;

            foreach (EdgeJS edge in edges)
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

        // windows and doors
        GeometryJS geometry = new GeometryJS();
        geometry.vertices = vertices;
        geometry.edges = edges;
        geometry.faces = faces;

        List<StoryJS> stories = new List<StoryJS>();
        StoryJS story = new StoryJS();
        story.geometry = geometry;
        story.spaces = spaces;
        story.doors = doors;
        story.windows = windows;
        stories.Add(story);

        List<WindowDefinitionJS> window_definitions = new List<WindowDefinitionJS>();
        WindowDefinitionJS windowDef = new WindowDefinitionJS();
        windowDef.id = "Window_0";
        windowDef.name = "Window 0";
        windowDef.width = windowWidth;
        window_definitions.Add(windowDef);

        List<DoorDefinitionJS> door_definitions = new List<DoorDefinitionJS>();
        DoorDefinitionJS frontDoor = new DoorDefinitionJS();
        frontDoor.id = "Door_0";
        frontDoor.name = "Door 0";
        frontDoor.width = frontDoorWidth;
        door_definitions.Add(frontDoor);

        FullJS full = new FullJS();
        full.stories = stories;
        full.door_definitions = door_definitions;
        full.window_definitions = window_definitions;

        string json = JsonUtility.ToJson(full);

        System.IO.File.WriteAllText(Application.persistentDataPath + "/next_geometry.json", json);
    }
}

[System.Serializable]
public class FullJS
{
    public List<StoryJS> stories = new List<StoryJS>();
    public List<WindowDefinitionJS> window_definitions = new List<WindowDefinitionJS>();
    public List<DoorDefinitionJS> door_definitions = new List<DoorDefinitionJS>();
}

[System.Serializable]
public class StoryJS
{
    public string id = "1";
    public string name = "Story 1";
    public string color = "#88ccee";
    public int floor_to_ceiling_height = 3;
    public int multiplier = 1;
    public GeometryJS geometry;
    public List<SpaceJS> spaces = new List<SpaceJS>();
    public List<DoorJS> doors = new List<DoorJS>();
    public List<WindowJS> windows = new List<WindowJS>();
}

[System.Serializable]
public class GeometryJS
{
    public string id = "g0";
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
    public string color;
    public string space_type_id;
    public string construction_set_id = "Construction_Set_0";
    public string thermal_zone_id;
    public string type = "space";
}

[System.Serializable]
public class WindowJS
{
    public string window_definition_id;
    public string edge_id;
    public float alpha;
    public string id;
    public string name;
}

[System.Serializable]
public class DoorJS
{
    public string door_definition_id;
    public string edge_id;
    public float alpha;
    public string id;
    public string name;
}

[System.Serializable]
public class WindowDefinitionJS
{
    public string id;
    public string name;
    public float sill_height = 1;
    public float height = 1;
    public float width;
}

[System.Serializable]
public class DoorDefinitionJS
{
    public string id;
    public string name;
    public float height = 1;
    public float width;
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