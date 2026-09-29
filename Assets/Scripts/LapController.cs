using System.Collections.Generic;
using UnityEngine;

public class LapController : MonoBehaviour
{
    [Header("Corrida")]
    [SerializeField] private int totalLaps = 3;
    [SerializeField] private float maxRaceTime = 90f;

    [Header("Final da Corrida")]
    [SerializeField] private float finishDelay = 3f;

    private class CarRaceData
    {
        public int currentLap = 0;
        public bool hasStarted = false;
        public bool hasFinished = false;
        public float lastCrossingTime = -999f;
    }

    private Dictionary<GameObject, CarRaceData> cars =
        new Dictionary<GameObject, CarRaceData>();

    public int GetCurrentLap(GameObject car)
    {
        if (!cars.ContainsKey(car))
            return 0;

        return cars[car].currentLap;
    }

    public bool HasFinished(GameObject car)
    {
        if (!cars.ContainsKey(car))
            return false;

        return cars[car].hasFinished;
    }

    private float raceTimer = 0f;

    private bool raceStarted = false;
    private bool raceFinished = false;

    private void Update()
    {
        if (!raceStarted || raceFinished)
            return;

        raceTimer += Time.deltaTime;

        if (raceTimer >= maxRaceTime)
        {
            DefeatByTime();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCarController player =
            other.GetComponentInParent<PlayerCarController>();

        AICarController ai =
            other.GetComponentInParent<AICarController>();

        if (player == null && ai == null)
            return;

        GameObject car;

        if (player != null)
        {
            car = player.gameObject;
        }
        else
        {
            car = ai.gameObject;
        }

        if (!cars.ContainsKey(car))
        {
            cars.Add(
                car,
                new CarRaceData()
            );
        }

        CarRaceData data = cars[car];

        // Evita o mesmo carro contar a linha
        // várias vezes seguidas.
        if (Time.time - data.lastCrossingTime < 1f)
            return;

        data.lastCrossingTime = Time.time;

        // =====================================================
        // PRIMEIRA PASSAGEM PELA LINHA
        // =====================================================

        if (!raceStarted)
        {
            raceStarted = true;
            raceTimer = 0f;

            data.hasStarted = true;

            Debug.Log("================================");
            Debug.Log("🏁 CORRIDA INICIADA!");
            Debug.Log("🏁 VOLTAS: " + totalLaps);
            Debug.Log("================================");

            return;
        }

        // =====================================================
        // CARRO AINDA NÃO ENTROU NA CORRIDA
        // =====================================================

        if (!data.hasStarted)
        {
            data.hasStarted = true;

            return;
        }

        // =====================================================
        // CORRIDA JÁ TERMINOU
        // =====================================================

        if (raceFinished)
            return;

        // =====================================================
        // CARRO JÁ TERMINOU
        // =====================================================

        if (data.hasFinished)
            return;

        // =====================================================
        // COMPLETOU UMA VOLTA
        // =====================================================

        data.currentLap++;

        Debug.Log(
            GetCarName(car) +
            " completou a volta " +
            data.currentLap +
            "/" +
            totalLaps +
            " | Tempo: " +
            raceTimer.ToString("F2") +
            "s"
        );

        // =====================================================
        // TERMINOU A CORRIDA
        // =====================================================

        if (data.currentLap >= totalLaps)
        {
            FinishCar(car);
        }
    }

    // =========================================================
    // CARRO TERMINOU
    // =========================================================

    private void FinishCar(GameObject car)
    {
        if (raceFinished)
            return;

        CarRaceData data = cars[car];

        data.hasFinished = true;

        Debug.Log("================================");
        Debug.Log(
            GetCarName(car) +
            " terminou a corrida!"
        );

        Debug.Log(
            "Tempo: " +
            raceTimer.ToString("F2") +
            " segundos"
        );
        Debug.Log("================================");

        // =====================================================
        // PLAYER VENCEU
        // =====================================================

        if (car.GetComponent<PlayerCarController>() != null)
        {
            Victory();
            return;
        }

        // =====================================================
        // IA VENCEU
        // =====================================================

        if (car.GetComponent<AICarController>() != null)
        {
            DefeatByAI();
        }
    }

    // =========================================================
    // VITÓRIA
    // =========================================================

    private void Victory()
    {
        if (raceFinished)
            return;

        raceFinished = true;

        Debug.Log("================================");
        Debug.Log("🏆 VITÓRIA!");
        Debug.Log("🏁 VOCÊ TERMINOU A CORRIDA EM 1º!");
        Debug.Log(
            "Tempo final: " +
            raceTimer.ToString("F2") +
            " segundos"
        );
        Debug.Log("================================");

        Invoke(
            nameof(StopRace),
            finishDelay
        );
    }

    // =========================================================
    // DERROTA - IA
    // =========================================================

    private void DefeatByAI()
    {
        if (raceFinished)
            return;

        raceFinished = true;

        Debug.Log("================================");
        Debug.Log("❌ DERROTA!");
        Debug.Log("🏎️ A IA TERMINOU A CORRIDA EM 1º!");
        Debug.Log(
            "Tempo: " +
            raceTimer.ToString("F2") +
            " segundos"
        );
        Debug.Log("================================");

        Invoke(
            nameof(StopRace),
            finishDelay
        );
    }

    // =========================================================
    // DERROTA - TEMPO
    // =========================================================

    private void DefeatByTime()
    {
        if (raceFinished)
            return;

        raceFinished = true;

        Debug.Log("================================");
        Debug.Log("⏱️ TEMPO ESGOTADO!");
        Debug.Log("❌ DERROTA!");
        Debug.Log(
            "Tempo máximo de " +
            maxRaceTime +
            " segundos atingido."
        );
        Debug.Log("================================");

        Invoke(
            nameof(StopRace),
            finishDelay
        );
    }

    // =========================================================
    // PARA A CORRIDA
    // =========================================================

    private void StopRace()
    {
        Time.timeScale = 0f;

        Debug.Log("================================");
        Debug.Log("🏁 CORRIDA ENCERRADA.");
        Debug.Log("================================");
    }

    // =========================================================
    // NOME DO CARRO
    // =========================================================

    private string GetCarName(GameObject car)
    {
        if (car.GetComponent<PlayerCarController>() != null)
        {
            return "PLAYER";
        }

        if (car.GetComponent<AICarController>() != null)
        {
            return "IA";
        }

        return car.name;
    }
}