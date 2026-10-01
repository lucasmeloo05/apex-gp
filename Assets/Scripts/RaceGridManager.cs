using UnityEngine;

public class RaceGridManager : MonoBehaviour
{
    [Header("Posições do Grid")]
    [SerializeField] private Transform[] gridPositions = new Transform[8];

    [Header("Carros")]
    [SerializeField] private GameObject[] cars = new GameObject[8];

    private void Start()
    {
        SetupGrid();
    }

    private void SetupGrid()
    {
        if (gridPositions.Length != 8)
        {
            Debug.LogError("O grid precisa ter exatamente 8 posições.");
            return;
        }

        if (cars.Length != 8)
        {
            Debug.LogError("O grid precisa ter exatamente 8 carros.");
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            if (cars[i] == null || gridPositions[i] == null)
            {
                Debug.LogWarning(
                    "Carro ou posição faltando no índice " + i
                );

                continue;
            }

            cars[i].transform.position = gridPositions[i].position;
        }

        Debug.Log("🏁 Grid configurado com 8 carros!");
    }
}