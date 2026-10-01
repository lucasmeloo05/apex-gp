using System.Collections;
using UnityEngine;

public class RaceStartManager : MonoBehaviour
{
    [Header("Corrida")]
    [SerializeField] private LapController lapController;

    [Header("Tempo das Luzes")]
    [SerializeField] private float timeBetweenLights = 1f;
    [SerializeField] private float timeBeforeStart = 1f;

    [Header("Carros IA")]
    [SerializeField] private AICarController[] aiCars;

    [Header("Jogador")]
    [SerializeField] private PlayerCarController playerCar;

    private bool raceStarted = false;

    private void Start()
    {
        StartCoroutine(StartSequence());
    }

    private IEnumerator StartSequence()
    {
        Debug.Log("================================");
        Debug.Log("🏁 PREPARE-SE!");
        Debug.Log("================================");

        yield return new WaitForSeconds(timeBeforeStart);

        for (int i = 1; i <= 5; i++)
        {
            Debug.Log("🔴 LUZ " + i);
            yield return new WaitForSeconds(timeBetweenLights);
        }

        Debug.Log("================================");
        Debug.Log("🟢 GO!");
        Debug.Log("================================");

        raceStarted = true;

        foreach (AICarController ai in aiCars)
        {
            if (ai != null)
            {
                ai.SetRaceStarted(true);
            }
        }

        if (playerCar != null)
        {
            playerCar.SetRaceStarted(true);
        }
        else
        {
            Debug.LogError(
                "RaceStartManager: PlayerCar não foi configurado!"
            );
        }

        if (lapController != null)
        {
            lapController.StartRace();
        }
        else
        {
            Debug.LogError(
                "RaceStartManager: LapController não foi configurado!"
            );
        }
    }

    public bool IsRaceStarted()
    {
        return raceStarted;
    }
}