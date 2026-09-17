using UnityEngine;

public class LapController : MonoBehaviour
{
    [Header("Corrida")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private float maxRaceTime = 90f;

    private int currentLap = 0;
    private bool canCompleteLap = false;

    private float raceTimer = 0f;
    private bool raceStarted = false;
    private bool raceFinished = false;

    private void Update()
    {
        // Só conta o tempo depois que a corrida começar
        if (!raceStarted || raceFinished)
            return;

        raceTimer += Time.deltaTime;

        // Tempo acabou
        if (raceTimer >= maxRaceTime)
        {
            Defeat();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Primeira passagem: inicia a corrida
        if (!canCompleteLap)
        {
            canCompleteLap = true;
            raceStarted = true;
            raceTimer = 0f;

            Debug.Log("Volta iniciada!");
            Debug.Log("Tempo máximo: " + maxRaceTime + " segundos");

            return;
        }

        // Se a corrida já terminou, ignora
        if (raceFinished)
            return;

        currentLap++;

        Debug.Log(
            "Volta completada: " +
            currentLap +
            "/" +
            totalLaps +
            " | Tempo: " +
            raceTimer.ToString("F2") +
            "s"
        );

        // Vitória
        if (currentLap >= totalLaps)
        {
            Victory();
        }
    }

    private void Victory()
    {
        raceFinished = true;

        Debug.Log("================================");
        Debug.Log("VITÓRIA!");
        Debug.Log(
            "Tempo final: " +
            raceTimer.ToString("F2") +
            " segundos"
        );
        Debug.Log("================================");
    }

    private void Defeat()
    {
        raceFinished = true;

        Debug.Log("================================");
        Debug.Log("DERROTA!");
        Debug.Log("Tempo limite de 90 segundos atingido.");
        Debug.Log("================================");
    }
}