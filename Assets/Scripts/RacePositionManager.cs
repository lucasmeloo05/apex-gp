using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calcula a posição do jogador pela DISTÂNCIA percorrida na pista
/// (projeção do carro sobre a linha dos waypoints), e não só pelo número do waypoint.
/// </summary>
public class RacePositionManager : MonoBehaviour
{
    [Header("Corrida")]
    [SerializeField] private LapController lapController;

    [Header("Player")]
    [Tooltip("Se vazio, encontra sozinho.")]
    [SerializeField] private PlayerCarController player;

    [Header("Bots")]
    [Tooltip("Se vazio, encontra todos os AICarController da cena.")]
    [SerializeField] private AICarController[] bots;

    [Header("Pista")]
    [Tooltip("Objeto pai dos waypoints (ex: 'AI Waypoints'). Os filhos, em ordem, formam a pista. Se vazio, procura um objeto chamado 'AI Waypoints'.")]
    [SerializeField] private Transform waypointsRoot;
    [Tooltip("Quantos segmentos à frente do último conhecido o carro procura (evita confundir trechos da pista que passam perto).")]
    [SerializeField] private int searchWindow = 8;

    private class Tracker
    {
        public GameObject go;
        public int lastSeg = -1;
        public float lastRel;
        public int lap;
        public bool initialized;
        public float progress;
    }

    // pista
    private Vector2[] wp;
    private float[] cum;
    private float trackLength;
    private float anchorS;
    private bool ready;

    // carros
    private Tracker playerTracker;
    private readonly List<Tracker> botTrackers = new List<Tracker>();

    private int playerPosition = 1;
    private int totalCars = 1;

    // =========================================================
    // ACESSO EXTERNO
    // =========================================================

    public int GetPlayerPosition()
    {
        return playerPosition;
    }

    public int GetTotalCars()
    {
        return totalCars;
    }

    // =========================================================
    // INICIALIZAÇÃO
    // =========================================================

    private void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerCarController>();

        if (bots == null || bots.Length == 0)
            bots = FindObjectsByType<AICarController>(FindObjectsSortMode.None);

        if (waypointsRoot == null)
        {
            GameObject found = GameObject.Find("AI Waypoints");
            if (found != null)
                waypointsRoot = found.transform;
        }

        if (player == null)
        {
            Debug.LogWarning("[RacePositionManager] Player não encontrado.");
            return;
        }

        if (waypointsRoot == null || waypointsRoot.childCount < 3)
        {
            Debug.LogWarning("[RacePositionManager] Waypoints Root não definido (ou com menos de 3 filhos).");
            return;
        }

        if (lapController == null)
            Debug.LogWarning("[RacePositionManager] Lap Controller vazio: a linha de chegada não será usada como referência.");

        BuildTrack();

        playerTracker = new Tracker { go = player.gameObject };

        botTrackers.Clear();
        foreach (AICarController bot in bots)
        {
            if (bot != null)
                botTrackers.Add(new Tracker { go = bot.gameObject });
        }

        totalCars = 1 + botTrackers.Count;
        ready = true;
    }

    private void BuildTrack()
    {
        int n = waypointsRoot.childCount;

        wp = new Vector2[n];
        cum = new float[n];

        for (int i = 0; i < n; i++)
            wp[i] = waypointsRoot.GetChild(i).position;

        for (int i = 1; i < n; i++)
            cum[i] = cum[i - 1] + Vector2.Distance(wp[i - 1], wp[i]);

        trackLength = cum[n - 1] + Vector2.Distance(wp[n - 1], wp[0]);

        // A linha de chegada é a origem das voltas
        anchorS = 0f;

        if (lapController != null)
        {
            int seg = -1;
            anchorS = Project(lapController.transform.position, ref seg);
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!ready)
            return;

        UpdateTracker(playerTracker);

        for (int i = 0; i < botTrackers.Count; i++)
        {
            if (botTrackers[i].go != null)
                UpdateTracker(botTrackers[i]);
        }

        float myProgress = ProgressOf(playerTracker);
        int position = 1;

        for (int i = 0; i < botTrackers.Count; i++)
        {
            Tracker bot = botTrackers[i];

            if (bot.go == null)
                continue;

            if (ProgressOf(bot) > myProgress)
                position++;
        }

        playerPosition = position;
    }

    // Carros que já terminaram ficam travados na posição final, acima de todos que ainda correm
    private float ProgressOf(Tracker t)
    {
        if (lapController != null && lapController.HasFinished(t.go))
            return 1e9f - lapController.GetFinishPosition(t.go);

        return t.progress;
    }

    // =========================================================
    // PROGRESSO NA PISTA
    // =========================================================

    private void UpdateTracker(Tracker t)
    {
        // Antes da largada, continua reiniciando (o grid pode ser posicionado depois do Start)
        if (lapController != null && !lapController.IsRaceStarted())
            t.initialized = false;

        float s = Project(t.go.transform.position, ref t.lastSeg);

        // distância percorrida a partir da linha de chegada
        float rel = Mathf.Repeat(s - anchorS, trackLength);

        if (!t.initialized)
        {
            t.initialized = true;

            // Está ATRÁS da linha (grid): volta -1, então o progresso começa levemente negativo
            t.lap = rel > trackLength * 0.5f ? -1 : 0;
        }
        else
        {
            float delta = rel - t.lastRel;

            if (delta < -trackLength * 0.5f)
                t.lap++;      // cruzou a linha para frente
            else if (delta > trackLength * 0.5f)
                t.lap--;      // cruzou a linha para trás
        }

        t.lastRel = rel;
        t.progress = t.lap * trackLength + rel;
    }

    // Retorna a distância (0..trackLength) do ponto mais próximo sobre a linha dos waypoints
    private float Project(Vector2 pos, ref int lastSeg)
    {
        int n = wp.Length;

        float bestDist = float.MaxValue;
        float bestS = 0f;
        int bestSeg = 0;

        if (lastSeg >= 0)
        {
            SearchSegments(pos, lastSeg - 3, searchWindow + 3, ref bestDist, ref bestS, ref bestSeg);

            // Perdeu a pista (teleporte, saída do grid...)? Busca completa.
            if (bestDist > 30f)
            {
                bestDist = float.MaxValue;
                SearchSegments(pos, 0, n, ref bestDist, ref bestS, ref bestSeg);
            }
        }
        else
        {
            SearchSegments(pos, 0, n, ref bestDist, ref bestS, ref bestSeg);
        }

        lastSeg = bestSeg;
        return bestS;
    }

    private void SearchSegments(Vector2 pos, int start, int count, ref float bestDist, ref float bestS, ref int bestSeg)
    {
        int n = wp.Length;

        for (int k = 0; k < count; k++)
        {
            int i = ((start + k) % n + n) % n;
            int next = (i + 1) % n;

            Vector2 a = wp[i];
            Vector2 ab = wp[next] - a;
            float sqr = ab.sqrMagnitude;

            float t = sqr > 0.0001f
                ? Mathf.Clamp01(Vector2.Dot(pos - a, ab) / sqr)
                : 0f;

            float d = Vector2.Distance(pos, a + ab * t);

            if (d < bestDist)
            {
                bestDist = d;
                bestSeg = i;
                bestS = cum[i] + t * Mathf.Sqrt(sqr);
            }
        }
    }
}