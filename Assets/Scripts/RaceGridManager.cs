using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceGridManager : MonoBehaviour
{
    [Header("Posições do Grid")]
    [SerializeField]
    private Transform[] gridPositions =
        new Transform[8];

    [Header("Carros")]
    [SerializeField]
    private GameObject[] cars =
        new GameObject[8];

    [Header("Carreira")]
    [Tooltip("Usa a classificação do campeonato para montar o grid das corridas seguintes.")]
    [SerializeField] private bool useCareerGrid = true;

    [Header("Orientação dos carros")]
    [Tooltip("Gira cada carro para o sentido da pista no ponto do grid (usa os waypoints). " +
             "Evita carros 'tortos' quando as posições do grid não estão giradas.")]
    [SerializeField] private bool alignCarsToTrack = true;

    [Tooltip("Objeto pai dos waypoints (ex: 'AI Waypoints'). Se vazio, procura um objeto com esse nome.")]
    [SerializeField] private Transform waypointsRoot;

    // cache dos waypoints (usado para descobrir o sentido da pista)
    private Vector2[] wp;
    private bool warnedNoWaypoints = false;

    private IEnumerator Start()
    {
        // Espera um frame para os objetos da cena terminarem
        // de inicializar.
        yield return null;

        SetupGrid();
    }

    // =========================================================
    // CONFIGURAÇÃO DO GRID
    // =========================================================

    private void SetupGrid()
    {
        if (gridPositions == null ||
            gridPositions.Length != 8)
        {
            Debug.LogError(
                "[RaceGridManager] O grid precisa ter exatamente 8 posições."
            );

            return;
        }

        if (cars == null ||
            cars.Length != 8)
        {
            Debug.LogError(
                "[RaceGridManager] O grid precisa ter exatamente 8 carros."
            );

            return;
        }

        CareerManager career =
            CareerManager.Instance;

        // -----------------------------------------------------
        // SEM CARREIRA
        // -----------------------------------------------------

        if (!useCareerGrid ||
            career == null ||
            string.IsNullOrWhiteSpace(
                career.PlayerName))
        {
            SetupNormalGrid();
            return;
        }

        // -----------------------------------------------------
        // PRIMEIRA CORRIDA
        // -----------------------------------------------------

        if (career.CurrentRace == 0)
        {
            Debug.Log(
                "[RaceGridManager] 🏁 PRIMEIRA CORRIDA - GRID NORMAL."
            );

            SetupNormalGrid();
            return;
        }

        // -----------------------------------------------------
        // CORRIDAS 2+
        // -----------------------------------------------------

        SetupInvertedChampionshipGrid();
    }

    // =========================================================
    // GRID NORMAL
    // =========================================================

    private void SetupNormalGrid()
    {
        for (int i = 0; i < 8; i++)
        {
            if (cars[i] == null ||
                gridPositions[i] == null)
            {
                Debug.LogWarning(
                    "[RaceGridManager] Carro ou posição faltando no índice " +
                    i
                );

                continue;
            }

            PlaceCar(
                cars[i],
                gridPositions[i]
            );
        }

        Debug.Log(
            "🏁 Grid normal configurado com 8 carros!"
        );
    }

    // =========================================================
    // GRID INVERTIDO PELO CAMPEONATO
    // =========================================================

    private void SetupInvertedChampionshipGrid()
    {
        CareerManager career =
            CareerManager.Instance;

        // -----------------------------------------------------
        // FORÇA A CONFIGURAÇÃO DOS PILOTOS
        // -----------------------------------------------------

        CareerRaceController careerController =
            FindFirstObjectByType<CareerRaceController>();

        if (careerController != null)
        {
            careerController.ConfigureCareerDrivers();

            Debug.Log(
                "[RaceGridManager] ✅ Pilotos da carreira " +
                "foram configurados antes do grid."
            );
        }
        else
        {
            Debug.LogError(
                "[RaceGridManager] CareerRaceController não encontrado na cena."
            );

            return;
        }

        // -----------------------------------------------------
        // CLASSIFICAÇÃO DO CAMPEONATO
        // -----------------------------------------------------

        List<CareerManager.DriverData> standings =
            career.GetChampionshipStandings();

        if (standings == null ||
            standings.Count != 8)
        {
            Debug.LogError(
                "[RaceGridManager] Classificação do campeonato inválida. " +
                "Esperado: 8 pilotos."
            );

            return;
        }

        Debug.Log("====================================");
        Debug.Log(
            "🏆 CLASSIFICAÇÃO DO CAMPEONATO ANTES DA CORRIDA " +
            (career.CurrentRace + 1)
        );

        for (int i = 0; i < standings.Count; i++)
        {
            CareerManager.DriverData driver =
                standings[i];

            Debug.Log(
                (i + 1) +
                "º - " +
                driver.name +
                " | " +
                driver.points +
                " pts"
            );
        }

        Debug.Log("====================================");

        // -----------------------------------------------------
        // INVERTE A CLASSIFICAÇÃO
        // -----------------------------------------------------

        standings.Reverse();

        // -----------------------------------------------------
        // MAPA PILOTO → CARRO
        // -----------------------------------------------------

        Dictionary<string, GameObject> carsByDriver =
            BuildCarDriverMap();

        // Diagnóstico: aponta exatamente qual piloto ficou sem carro
        foreach (CareerManager.DriverData d in standings)
        {
            if (!carsByDriver.ContainsKey(d.name))
            {
                Debug.LogError(
                    "[RaceGridManager] Sem carro para o piloto: " +
                    d.name
                );
            }
        }

        if (carsByDriver.Count != 8)
        {
            Debug.LogError(
                "[RaceGridManager] Não foi possível mapear os 8 pilotos " +
                "para os 8 carros. Encontrados: " +
                carsByDriver.Count
            );

            return;
        }

        // -----------------------------------------------------
        // POSICIONA
        // -----------------------------------------------------

        Debug.Log("====================================");
        Debug.Log(
            "🏁 GRID INVERTIDO PELO CAMPEONATO"
        );
        Debug.Log("====================================");

        for (int i = 0; i < standings.Count; i++)
        {
            CareerManager.DriverData driver =
                standings[i];

            if (!carsByDriver.TryGetValue(
                    driver.name,
                    out GameObject car))
            {
                Debug.LogError(
                    "[RaceGridManager] Não encontrei o carro do piloto: " +
                    driver.name
                );

                continue;
            }

            if (gridPositions[i] == null)
            {
                Debug.LogWarning(
                    "[RaceGridManager] GridPosition_" +
                    (i + 1).ToString("00") +
                    " está vazio."
                );

                continue;
            }

            PlaceCar(
                car,
                gridPositions[i]
            );

            Debug.Log(
                "GRID " +
                (i + 1) +
                " → " +
                driver.name +
                " | " +
                driver.points +
                " pts"
            );
        }

        Debug.Log("====================================");
        Debug.Log(
            "🏁 GRID INVERTIDO CONFIGURADO COM 8 CARROS!"
        );
        Debug.Log("====================================");
    }

    // =========================================================
    // POSICIONA UM CARRO NO GRID
    // =========================================================

    private void PlaceCar(
        GameObject car,
        Transform slot)
    {
        Vector2 position = slot.position;

        // Por padrão, usa a rotação da posição do grid
        Quaternion rotation = slot.rotation;

        // Se possível, gira o carro para o sentido da pista
        if (alignCarsToTrack &&
            TryGetTrackDirection(position, out Vector2 dir))
        {
            // A frente do carro é -transform.up
            float z =
                Mathf.Atan2(dir.x, -dir.y) *
                Mathf.Rad2Deg;

            rotation =
                Quaternion.Euler(0f, 0f, z);
        }

        car.transform.position = slot.position;
        car.transform.rotation = rotation;

        // Mantém o Rigidbody2D sincronizado e sem velocidade herdada
        Rigidbody2D rb =
            car.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.position = position;
            rb.rotation = rotation.eulerAngles.z;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // =========================================================
    // SENTIDO DA PISTA NUM PONTO (pelos waypoints)
    // =========================================================

    private void CacheWaypoints()
    {
        if (wp != null)
            return;

        if (waypointsRoot == null)
        {
            GameObject found =
                GameObject.Find("AI Waypoints");

            if (found != null)
                waypointsRoot = found.transform;
        }

        if (waypointsRoot == null ||
            waypointsRoot.childCount < 2)
        {
            return;
        }

        wp = new Vector2[waypointsRoot.childCount];

        for (int i = 0; i < wp.Length; i++)
            wp[i] = waypointsRoot.GetChild(i).position;
    }

    private bool TryGetTrackDirection(
        Vector2 pos,
        out Vector2 dir)
    {
        dir = Vector2.up;

        CacheWaypoints();

        if (wp == null)
        {
            if (!warnedNoWaypoints)
            {
                warnedNoWaypoints = true;

                Debug.LogWarning(
                    "[RaceGridManager] Waypoints não encontrados. " +
                    "Os carros usarão a rotação das posições do grid. " +
                    "Defina 'Waypoints Root' (ex: AI Waypoints)."
                );
            }

            return false;
        }

        int n = wp.Length;

        float best = float.MaxValue;
        Vector2 bestDir = Vector2.zero;

        for (int i = 0; i < n; i++)
        {
            Vector2 a = wp[i];
            Vector2 b = wp[(i + 1) % n];

            Vector2 ab = b - a;
            float sqr = ab.sqrMagnitude;

            float t = sqr > 0.0001f
                ? Mathf.Clamp01(Vector2.Dot(pos - a, ab) / sqr)
                : 0f;

            float d = Vector2.Distance(pos, a + ab * t);

            if (d < best)
            {
                best = d;
                bestDir = ab;
            }
        }

        if (bestDir.sqrMagnitude < 0.0001f)
            return false;

        dir = bestDir.normalized;

        return true;
    }

    // =========================================================
    // PILOTO → CARRO
    // =========================================================

    private Dictionary<string, GameObject> BuildCarDriverMap()
    {
        Dictionary<string, GameObject> map =
            new Dictionary<string, GameObject>();

        CareerManager career =
            CareerManager.Instance;

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------
        //
        // Procura o PlayerCarController ATIVO. Um componente
        // sobrando e desativado em outro carro não pode ser
        // confundido com o jogador.
        //

        PlayerCarController player = null;

        foreach (PlayerCarController candidate in
                 FindObjectsByType<PlayerCarController>(
                     FindObjectsSortMode.None))
        {
            if (candidate != null &&
                candidate.isActiveAndEnabled)
            {
                player = candidate;
                break;
            }
        }

        if (player != null)
        {
            AddDriverToMap(
                map,
                career.PlayerName,
                player.gameObject
            );
        }
        else
        {
            Debug.LogError(
                "[RaceGridManager] Nenhum PlayerCarController ATIVO encontrado."
            );
        }

        // -----------------------------------------------------
        // BOTS
        // -----------------------------------------------------

        AICarController[] bots =
            FindObjectsByType<AICarController>(
                FindObjectsSortMode.None
            );

        foreach (AICarController bot in bots)
        {
            if (bot == null ||
                !bot.isActiveAndEnabled)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    bot.CareerDriverName))
            {
                Debug.LogError(
                    "[RaceGridManager] O bot " +
                    GetPath(bot.transform) +
                    " continua sem Career Driver Name " +
                    "mesmo após a configuração."
                );

                continue;
            }

            AddDriverToMap(
                map,
                bot.CareerDriverName,
                bot.gameObject
            );
        }

        return map;
    }

    // =========================================================
    // ADICIONA PILOTO
    // =========================================================

    private void AddDriverToMap(
        Dictionary<string, GameObject> map,
        string driverName,
        GameObject car)
    {
        if (string.IsNullOrWhiteSpace(driverName) ||
            car == null)
        {
            return;
        }

        if (map.ContainsKey(driverName))
        {
            Debug.LogError(
                "[RaceGridManager] PILOTO DUPLICADO: " +
                driverName +
                " | Carro: " +
                car.name
            );

            return;
        }

        map.Add(
            driverName,
            car
        );

        Debug.Log(
            "[RaceGridManager] MAPEADO: " +
            driverName +
            " → " +
            GetPath(car.transform)
        );
    }

    // =========================================================
    // PATH DO OBJETO
    // =========================================================

    private string GetPath(Transform t)
    {
        string path = t.name;

        while (t.parent != null)
        {
            t = t.parent;
            path =
                t.name +
                "/" +
                path;
        }

        return path;
    }
}