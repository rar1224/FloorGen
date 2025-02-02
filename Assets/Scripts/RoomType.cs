using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomType
{
    public string name;
    List<Vector2> orientations = new List<Vector2>();
    int minSizeIndex;
    int maxSizeIndex;

    int maxWallLengthIndex;
    bool needsWindows;

    private Color color;

    public RoomType(string name, List<Vector2> orientations, int minSizeIndex, int maxSizeIndex, int maxWallLengthIndex, bool needsWindows,
        Color color)
    {
        this.name = name;
        this.orientations = orientations;
        this.minSizeIndex = minSizeIndex;
        this.maxSizeIndex = maxSizeIndex;
        this.maxWallLengthIndex = maxWallLengthIndex;
        this.needsWindows = needsWindows;
        this.color = color;
    }

    public RoomType(string name, Color color)
    {
        this.name = name;
        this.color = color;
    }

    public int GetScore(Room room, List<Room> roomsToDefine)
    {
        int score = 0;
        roomsToDefine.Sort();

        foreach(Vector2 orientation in orientations) if (room.outOrientations.ContainsKey(orientation)) { score++; }

        if (roomsToDefine.IndexOf(room) >= minSizeIndex && roomsToDefine.IndexOf(room) <= maxSizeIndex) { score++; }

        roomsToDefine = roomsToDefine.OrderBy(room => room.wallLength).ToList();

        if (roomsToDefine.IndexOf(room) < maxWallLengthIndex) { score++;  }

        if (needsWindows && !room.HasWindows()) { score = score - 5; }

        return score;
    }

    public void AssignRoom(Room room)
    {
        room.name = name;
        room.color = color;
        room.Recolor(color);
    }
}
