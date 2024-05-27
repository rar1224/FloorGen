using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomType
{
    List<Vector2> orientations = new List<Vector2>();
    int sizeIndex;
    int wallLengthIndex;
    int minWindows;

    public RoomType(List<Vector2> orientations, int sizeIndex, int wallLengthIndex, int minWindows)
    {
        this.orientations = orientations;
        this.sizeIndex = sizeIndex;
        this.wallLengthIndex = wallLengthIndex;
        this.minWindows = minWindows;
    }

    public int GetScore(Room room, List<Room> roomsToDefine)
    {
        int score = 0;
        roomsToDefine.Sort();

        foreach(Vector2 orientation in orientations) if (room.outOrientations.ContainsKey(orientation)) { score++; }

        if (roomsToDefine.IndexOf(room) > sizeIndex) { score++; }


    }
}
