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

    private float raceTimer = 0f;

    private bool raceStarted = false;
    private bool raceFinished = false;

    private void Update()
    {
        // Só conta o tempo depois que a corrida começou
        if (!raceStarted || raceFinished)
            return;

        raceTimer += Time.deltaTime;

        // =========================
        // TEMPO LIMITE
        // =========================

        if (raceTimer >= maxRaceTime)
        {
            DefeatByTime();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // =====================================================
        // IDENTIFICA O CARRO
        // =====================================================

        PlayerCarController player =
            other.GetComponentInParent<PlayerCarController>();

        AICarController ai =
            other.GetComponentInParent<AICarController>();

        // Não é Player nem IA
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

        // =====================================================
        // CRIA REGISTRO DO CARRO
        // =====================================================

        if (!cars.ContainsKey(car))
        {
            cars.Add(
                car,
                new CarRaceData()
            );
        }

        CarRaceData data = cars[car];

        // =====================================================
        // EVITA CONTAGEM DUPLA
        // =====================================================

        if (Time.time - data.lastCrossingTime < 1f)
            return;

        data.lastCrossingTime = Time.time;

        // =====================================================
        // PRIMEIRA PASSAGEM
        // =====================================================

        if (!raceStarted)
        {
            raceStarted = true;
            raceTimer = 0f;

            data.hasStarted = true;

            Debug.Log("================================");
            Debug.Log("🏁 CORRIDA INICIADA!");
            Debug.Log("================================");

            return;
        }

        // =====================================================
        // CARRO AINDA NÃO ESTAVA NA CORRIDA
        // =====================================================

        if (!data.hasStarted)
        {
            data.hasStarted = true;
            return;
        }

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
    // FINALIZA CARRO
    // =========================================================

    private void FinishCar(GameObject car)
    {
        CarRaceData data = cars[car];

        data.hasFinished = true;

        Debug.Log(
            GetCarName(car) +
            " terminou a corrida!"
        );

        Debug.Log(
            "Tempo: " +
            raceTimer.ToString("F2") +
            " segundos"
        );

        // =====================================================
        // PLAYER TERMINOU
        // =====================================================

        if (car.GetComponent<PlayerCarController>() != null)
        {
            Victory();
            return;
        }

        // =====================================================
        // IA TERMINOU
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
        Debug.Log("🏁 VOCÊ TERMINOU A CORRIDA EM PRIMEIRO!");
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
        Debug.Log("🏎️ A IA TERMINOU A CORRIDA PRIMEIRO!");
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