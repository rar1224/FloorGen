using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridVertex : MonoBehaviour
{
    public UIController controller;
    public void OnMouseDown()
    {
        controller.TurnToVertex(this);
    }
}
