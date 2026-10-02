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

        public int finishPosition = 0;
    }

    private Dictionary<GameObject, CarRaceData> cars =
        new Dictionary<GameObject, CarRaceData>();

    // =========================================================
    // ESTADO DA CORRIDA
    // =========================================================

    private float raceTimer = 0f;

    private bool raceStarted = false;
    private bool raceFinished = false;

    private int nextFinishPosition = 1;

    // =========================================================
    // ACESSO EXTERNO
    // =========================================================

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

    public int GetFinishPosition(GameObject car)
    {
        if (!cars.ContainsKey(car))
            return 0;

        return cars[car].finishPosition;
    }

    public bool IsRaceStarted()
    {
        return raceStarted;
    }

    public bool IsRaceFinished()
    {
        return raceFinished;
    }

    public float GetRaceTime()
    {
        return raceTimer;
    }

    public int GetTotalLaps()
    {
        return totalLaps;
    }

    // =========================================================
    // INÍCIO DA CORRIDA
    // =========================================================

    public void StartRace()
    {
        if (raceStarted || raceFinished)
            return;

        raceStarted = true;
        raceTimer = 0f;

        Debug.Log("================================");
        Debug.Log("🏁 CORRIDA INICIADA!");
        Debug.Log("🏁 VOLTAS: " + totalLaps);
        Debug.Log("================================");
    }

    // =========================================================
    // UPDATE
    // =========================================================

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

    // =========================================================
    // LINHA DE CHEGADA
    // =========================================================

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

        // =====================================================
        // CORRIDA AINDA NÃO COMEÇOU
        // =====================================================

        if (!raceStarted)
            return;

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
        // EVITA DUPLO CONTATO
        // =====================================================

        if (Time.time - data.lastCrossingTime < 1f)
            return;

        data.lastCrossingTime = Time.time;

        // =====================================================
        // PRIMEIRA PASSAGEM PELA LINHA
        // =====================================================
        //
        // O carro começa ANTES da linha.
        //
        // Portanto:
        //
        // largada = LAP 0
        //
        // primeira passagem pela linha = LAP 1
        //
        // =====================================================

        if (!data.hasStarted)
        {
            data.hasStarted = true;
            data.currentLap = 1;

            Debug.Log(
                GetCarName(car) +
                " iniciou a corrida oficialmente! " +
                "LAP 1/" +
                totalLaps
            );

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
        // COMPLETOU MAIS UMA VOLTA
        // =====================================================

        // Se já estava na última volta, este cruzamento é a chegada
        if (data.currentLap >= totalLaps)
        {
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

            FinishCar(car);
            return;
        }

        data.currentLap++;

        Debug.Log(
            GetCarName(car) +
            " iniciou a volta " +
            data.currentLap +
            "/" +
            totalLaps +
            " | Tempo: " +
            raceTimer.ToString("F2") +
            "s"
        );
    }

    // =========================================================
    // CARRO TERMINOU
    // =========================================================

    private void FinishCar(GameObject car)
    {
        if (!cars.ContainsKey(car))
            return;

        CarRaceData data = cars[car];

        if (data.hasFinished)
            return;

        data.hasFinished = true;

        data.finishPosition = nextFinishPosition;

        nextFinishPosition++;

        Debug.Log("================================");

        Debug.Log(
            "🏁 " +
            GetCarName(car) +
            " terminou!"
        );

        Debug.Log(
            "🏆 POSIÇÃO: " +
            data.finishPosition +
            "º"
        );

        Debug.Log(
            "⏱️ Tempo: " +
            raceTimer.ToString("F2") +
            " segundos"
        );

        Debug.Log("================================");

        // =====================================================
        // IA
        // =====================================================

        if (car.GetComponent<AICarController>() != null)
        {
            Debug.Log(
                "🤖 IA terminou em " +
                data.finishPosition +
                "º lugar."
            );

            return;
        }

        // =====================================================
        // PLAYER
        // =====================================================

        if (car.GetComponent<PlayerCarController>() != null)
        {
            FinishPlayer(car);
        }
    }

    // =========================================================
    // FINAL DO PLAYER
    // =========================================================

    private void FinishPlayer(GameObject player)
    {
        if (raceFinished)
            return;

        raceFinished = true;

        int position = cars[player].finishPosition;

        Debug.Log("================================");

        if (position == 1)
        {
            Debug.Log("🏆 VITÓRIA!");
        }
        else
        {
            Debug.Log("🏁 CORRIDA TERMINADA!");
        }

        Debug.Log(
            "🏎️ VOCÊ TERMINOU EM " +
            position +
            "º LUGAR!"
        );

        Debug.Log(
            "⏱️ Tempo final: " +
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
    // DERROTA POR TEMPO
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