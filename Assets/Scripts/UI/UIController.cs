using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject grid;
    public GridVertex gridVertexPrefab;
    public Vertex vertexPrefab;
    public Generator generator;

    public TMP_InputField maxCellHeight;
    public TMP_InputField maxCellWidth;
    public TMP_InputField minCellSize;
    public TMP_InputField frontDoorWidth;
    public TMP_InputField windowWidth;
    public TMP_InputField windowNumber;
    public TMP_InputField minGapBetween;
    public Scrollbar optimizationBar;
    public Toggle optimizedExpansion;

    public Button startButton;
    public Button resetButton;

    private bool resetGenerator = false;

    void Start()
    {
        MakeGrid();
        SetDefaults();

        startButton.interactable = false;
        resetButton.interactable = true;
    }

    public void SetDefaults()
    {
        maxCellHeight.SetTextWithoutNotify("2");
        maxCellWidth.SetTextWithoutNotify("2.2");
        minCellSize.SetTextWithoutNotify("1");
        frontDoorWidth.SetTextWithoutNotify("1.2");
        windowWidth.SetTextWithoutNotify("0.5");
        windowNumber.SetTextWithoutNotify("7");
        minGapBetween.SetTextWithoutNotify("0.5");
        optimizationBar.value = 0.5f;
        optimizedExpansion.SetIsOnWithoutNotify(true);
    }

    public void MakeGrid()
    {
        Camera cam = Camera.main;
        float aspect = (float)Screen.width / Screen.height;
        int worldHeight = (int)(cam.orthographicSize);
        int worldWidth = (int)(worldHeight * aspect);

        for (int i = -worldWidth - 1; i < worldWidth + 1; i++)
        {
            for (int j = -worldHeight - 1; j < worldHeight + 1; j++)
            {
                GridVertex v = Instantiate(gridVertexPrefab, grid.transform);
                v.transform.position = new Vector3(i, j, 0);
                v.controller = this;
            }
        }
    }

    public void TurnToVertex(GridVertex v)
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Vertex v1 = Instantiate(vertexPrefab, generator.envelope.transform);
            v1.transform.position = v.transform.position;
            Destroy(v);

            if (generator.envelope.transform.childCount == 4)
            {
                startButton.interactable = true;
            }
        }
    }

    public void StartGenerating()
    {
        foreach (Transform t in grid.transform)
        {
            Destroy(t.gameObject);
        }

        bool optimize = optimizationBar.value > 0;
        bool superOptimize = optimizationBar.value == 1;

        generator.SetParameters(float.Parse(maxCellHeight.text, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(maxCellWidth.text, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(minCellSize.text, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(frontDoorWidth.text, System.Globalization.CultureInfo.InvariantCulture),
            float.Parse(windowWidth.text, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(windowNumber.text),
            float.Parse(minGapBetween.text, System.Globalization.CultureInfo.InvariantCulture), optimize, superOptimize, optimizedExpansion.isOn);
        generator.StartGenerating();

        startButton.interactable = false;
        resetButton.interactable = false;
        resetGenerator = true;
    }

    public void Reset()
    {
        SetDefaults();
        ResetGenerator();
    }

    public void Cancel()
    {
        generator.Cancel();
        ResetGenerator();
    }

    public void ResetGenerator()
    {
        foreach(Transform t in grid.transform)
        {
            Destroy(t.gameObject);
        }

        foreach(Transform t in generator.envelope.transform)
        {
            Destroy(t.gameObject);
        }

        if (resetGenerator)
        {
            generator.ResetAll();
            resetGenerator = false;
        }

        MakeGrid();
        startButton.interactable = false;
    }

    public void SetReadyToRestart()
    {
        resetButton.interactable = true;
    }


}


