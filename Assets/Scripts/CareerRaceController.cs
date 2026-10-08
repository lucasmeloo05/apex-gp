using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public class CareerRaceController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private LapController lapController;
    [SerializeField] private PlayerCarController player;

    [Tooltip("Mantido só para consulta. Com 'Auto Find Bots' ligado, a lista é montada da cena e este array é ignorado.")]
    [SerializeField] private AICarController[] bots;

    [Tooltip("Recomendado: monta a lista de bots direto da cena, sem repetições.")]
    [SerializeField] private bool autoFindBots = true;

    [Header("Configuração")]
    [SerializeField] private int expectedCars = 8;

    private bool resultRegistered = false;
    private bool resultProcessingFinished = false;
    private bool botsRefreshed = false;
    private bool warnedUnfinished = false;

    // Lista real usada na corrida
    private readonly List<AICarController> activeBots =
        new List<AICarController>();

    private class Entry
    {
        public GameObject go;
        public string driverName;
        public int position;
        public bool isPlayer;
    }

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        FindReferences();

        // IMPORTANTE:
        // Configura os nomes dos pilotos antes dos Starts dos outros
        // sistemas da cena, incluindo o RaceGridManager.
        ConfigureCareerDrivers();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        RefreshBots();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (resultRegistered || resultProcessingFinished)
            return;

        if (player == null)
        {
            FindReferences();
        }

        if (CareerManager.Instance == null)
            return;

        if (lapController == null)
            return;

        if (!lapController.IsRaceFinished())
            return;

        // Atualiza a lista uma única vez quando a corrida termina
        if (!botsRefreshed)
        {
            botsRefreshed = true;
            RefreshBots();
        }

        if (!AllCarsFinished())
        {
            if (!warnedUnfinished)
            {
                warnedUnfinished = true;

                Debug.LogWarning(
                    "[CareerRaceController] A corrida foi encerrada, " +
                    "mas nem todos os carros terminaram " +
                    "(provavelmente tempo máximo esgotado). " +
                    "O resultado NÃO será registrado."
                );
            }

            return;
        }

        resultProcessingFinished = true;

        RegisterResults();
    }

    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    private void FindReferences()
    {
        if (lapController == null)
        {
            lapController =
                FindFirstObjectByType<LapController>(
                    FindObjectsInactive.Include
                );
        }

        // ---------------------------------------------------------
        // PLAYER
        // ---------------------------------------------------------
        //
        // Procura inclusive objetos/componentes inativos.
        // O Player pode estar temporariamente desabilitado,
        // mas ainda precisamos da referência ao GameObject
        // para consultar o resultado da corrida.
        //

        if (player == null)
        {
            PlayerCarController[] candidates =
                FindObjectsByType<PlayerCarController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (PlayerCarController candidate in candidates)
            {
                if (candidate == null)
                    continue;

                // Se houver mais de um, prioriza o objeto
                // chamado exatamente PlayerCar.
                if (candidate.gameObject.name == "PlayerCar")
                {
                    player = candidate;
                    break;
                }

                // Caso contrário, guarda o primeiro encontrado.
                if (player == null)
                    player = candidate;
            }
        }

        Debug.Log(
            "[CareerRaceController] FindReferences | " +
            "Player=" +
            (player != null) +
            " | LapController=" +
            (lapController != null)
        );

        if (player == null)
        {
            Debug.LogError(
                "[CareerRaceController] Nenhum " +
                "PlayerCarController encontrado na cena."
            );
        }
    }

    // =========================================================
    // BOT UTILIZÁVEL
    // =========================================================

    // Antes do OnEnable dos bots, isActiveAndEnabled ainda é false.
    // enabled + activeInHierarchy já valem desde o Awake.
    private bool IsBotUsable(AICarController bot)
    {
        return bot != null &&
               bot.enabled &&
               bot.gameObject.activeInHierarchy;
    }

    // =========================================================
    // CONFIGURAÇÃO DOS PILOTOS DA CARREIRA
    // =========================================================

    public void ConfigureCareerDrivers()
    {
        if (CareerManager.Instance == null ||
    !CareerManager.Instance.IsCareerActive)
        {
            Debug.Log(
                "[CareerRaceController] Modo normal detectado. " +
                "Configuração de carreira ignorada."
            );

            return;
        }

        CareerManager career =
            CareerManager.Instance;

        if (career == null)
        {
            Debug.Log(
                "[CareerRaceController] CareerManager ainda não " +
                "está disponível. Configuração de pilotos ignorada."
            );

            return;
        }

        // -----------------------------------------------------
        // ENCONTRA OS BOTS POR PREFIXO
        // -----------------------------------------------------

        List<AICarController> drowBots =
            FindBotsByPrefix("DrowBot");

        List<AICarController> xeuBots =
            FindBotsByPrefix("XeuBot");

        List<AICarController> chibaBots =
            FindBotsByPrefix("ChibaBot");

        AICarController apexBot =
            FindBotByExactName("ApexBot");

        // -----------------------------------------------------
        // LOG DE DIAGNÓSTICO
        // -----------------------------------------------------

        Debug.Log("====================================");
        Debug.Log("🏎️ CONFIGURANDO PILOTOS DA CARREIRA");
        Debug.Log(
            "Jogador: " +
            career.PlayerName
        );
        Debug.Log(
            "Equipe: " +
            career.GetTeamName(career.PlayerTeam)
        );
        Debug.Log("------------------------------------");

        Debug.Log(
            "Drow bots encontrados: " +
            drowBots.Count
        );

        Debug.Log(
            "Xeu bots encontrados: " +
            xeuBots.Count
        );

        Debug.Log(
            "Chiba bots encontrados: " +
            chibaBots.Count
        );

        Debug.Log(
            "ApexBot encontrado: " +
            (apexBot != null)
        );

        Debug.Log("====================================");

        // -----------------------------------------------------
        // TODOS OS CENÁRIOS TÊM A VAGA IMPERIO NORMAL
        // -----------------------------------------------------

        if (apexBot != null)
        {
            apexBot.SetCareerDriverName(
                "Victor Bianchi"
            );
        }

        // -----------------------------------------------------
        // DROW
        // -----------------------------------------------------

        if (drowBots.Count >= 2)
        {
            if (career.PlayerTeam == CareerManager.Team.DrowGP)
            {
                // Jogador substituiu Enzo.
                // O primeiro bot Drow fica com Adrian.
                drowBots[0].SetCareerDriverName(
                    "Adrian Keller"
                );

                // O segundo slot é usado pela segunda vaga
                // da Imperio.
                drowBots[1].SetCareerDriverName(
                    "Rafael Costa"
                );
            }
            else
            {
                drowBots[0].SetCareerDriverName(
                    "Enzo Moretti"
                );

                drowBots[1].SetCareerDriverName(
                    "Adrian Keller"
                );
            }
        }
        else
        {
            Debug.LogError(
                "[CareerRaceController] Era esperado encontrar " +
                "2 bots Drow."
            );
        }

        // -----------------------------------------------------
        // XEU
        // -----------------------------------------------------

        if (xeuBots.Count >= 2)
        {
            if (career.PlayerTeam ==
                CareerManager.Team.XeuMotorsport)
            {
                // Jogador substituiu Lucas.
                // Matteo continua.
                xeuBots[0].SetCareerDriverName(
                    "Matteo Rossi"
                );

                // Segundo slot vira Rafael.
                xeuBots[1].SetCareerDriverName(
                    "Rafael Costa"
                );
            }
            else
            {
                xeuBots[0].SetCareerDriverName(
                    "Lucas Ferreira"
                );

                xeuBots[1].SetCareerDriverName(
                    "Matteo Rossi"
                );
            }
        }
        else
        {
            Debug.LogError(
                "[CareerRaceController] Era esperado encontrar " +
                "2 bots Xeu."
            );
        }

        // -----------------------------------------------------
        // CHIBA
        // -----------------------------------------------------

        if (chibaBots.Count >= 2)
        {
            if (career.PlayerTeam ==
                CareerManager.Team.ChibaRacing)
            {
                // Jogador substituiu Kenji.
                // Daniel continua.
                chibaBots[0].SetCareerDriverName(
                    "Daniel Novak"
                );

                // Segundo slot vira Rafael.
                chibaBots[1].SetCareerDriverName(
                    "Rafael Costa"
                );
            }
            else
            {
                chibaBots[0].SetCareerDriverName(
                    "Kenji Takahashi"
                );

                chibaBots[1].SetCareerDriverName(
                    "Daniel Novak"
                );
            }
        }
        else
        {
            Debug.LogError(
                "[CareerRaceController] Era esperado encontrar " +
                "2 bots Chiba."
            );
        }

        // -----------------------------------------------------
        // CASO IMPERIO
        // -----------------------------------------------------
        //
        // Se o jogador escolheu Imperio:
        //
        // Player      → jogador
        // ApexBot     → Victor Bianchi
        //
        // As outras equipes continuam normais.
        //

        if (career.PlayerTeam ==
            CareerManager.Team.ScuderiaImperio)
        {
            Debug.Log(
                "[CareerRaceController] Player pertence à " +
                "Scuderia Imperio. ApexBot = Victor Bianchi."
            );
        }

        // -----------------------------------------------------
        // MOSTRA O RESULTADO FINAL DA CONFIGURAÇÃO
        // -----------------------------------------------------

        Debug.Log("====================================");
        Debug.Log("✅ PILOTOS DA CARREIRA CONFIGURADOS");
        Debug.Log("====================================");

        LogBotDriver(apexBot);

        LogBotList(drowBots);
        LogBotList(xeuBots);
        LogBotList(chibaBots);

        Debug.Log("====================================");
    }

    // =========================================================
    // ENCONTRA BOTS POR PREFIXO
    // =========================================================

    private List<AICarController> FindBotsByPrefix(
        string prefix
    )
    {
        List<AICarController> found =
            new List<AICarController>();

        AICarController[] allBots =
            FindObjectsByType<AICarController>(
                FindObjectsSortMode.None
            );

        foreach (AICarController bot in allBots)
        {
            if (!IsBotUsable(bot))
                continue;

            if (bot.name.StartsWith(prefix))
            {
                found.Add(bot);
            }
        }

        // Ordem determinística:
        // DrowBot
        // DrowBot (1)
        // DrowBot (2)
        // etc.
        found.Sort(
            (a, b) =>
                string.Compare(
                    a.name,
                    b.name,
                    System.StringComparison.Ordinal
                )
        );

        return found;
    }

    // =========================================================
    // ENCONTRA UM BOT PELO NOME EXATO
    // =========================================================

    private AICarController FindBotByExactName(
        string botName
    )
    {
        AICarController[] allBots =
            FindObjectsByType<AICarController>(
                FindObjectsSortMode.None
            );

        foreach (AICarController bot in allBots)
        {
            if (!IsBotUsable(bot))
                continue;

            if (bot.name == botName)
                return bot;
        }

        return null;
    }

    // =========================================================
    // LOG
    // =========================================================

    private void LogBotDriver(
        AICarController bot
    )
    {
        if (bot == null)
            return;

        Debug.Log(
            bot.name +
            " → " +
            bot.CareerDriverName
        );
    }

    private void LogBotList(
        List<AICarController> botsList
    )
    {
        foreach (AICarController bot in botsList)
        {
            LogBotDriver(bot);
        }
    }

    // =========================================================
    // REFRESH BOTS
    // =========================================================

    private void RefreshBots()
    {
        activeBots.Clear();

        AICarController[] source;

        if (autoFindBots ||
            bots == null ||
            bots.Length == 0)
        {
            source =
                FindObjectsByType<AICarController>(
                    FindObjectsSortMode.None
                );
        }
        else
        {
            source = bots;
        }

        HashSet<GameObject> seen =
            new HashSet<GameObject>();

        foreach (AICarController bot in source)
        {
            if (bot == null ||
                !bot.isActiveAndEnabled)
            {
                continue;
            }

            // Nunca conta o player como bot
            if (player != null &&
                bot.gameObject == player.gameObject)
            {
                continue;
            }

            if (!seen.Add(bot.gameObject))
            {
                continue;
            }

            activeBots.Add(bot);
        }

        activeBots.Sort(
            (a, b) =>
                string.Compare(
                    a.name,
                    b.name,
                    System.StringComparison.Ordinal
                )
        );

        Debug.Log(
            "[CareerRaceController] Referências: " +
            "Player=" +
            (player != null) +
            " | Bots=" +
            activeBots.Count +
            " | LapController=" +
            (
                lapController != null
                    ? lapController.GetInstanceID()
                    : 0
            )
        );
    }

    // =========================================================
    // VERIFICAÇÃO
    // =========================================================

    private bool AllCarsFinished()
    {
        if (player == null)
            return false;

        if (!lapController.HasFinished(
                player.gameObject))
        {
            return false;
        }

        if (activeBots.Count !=
            expectedCars - 1)
        {
            Debug.LogError(
                "[CareerRaceController] Quantidade de bots " +
                "inesperada. Esperado: " +
                (expectedCars - 1) +
                " | Encontrado: " +
                activeBots.Count
            );

            return false;
        }

        foreach (AICarController bot in activeBots)
        {
            if (!lapController.HasFinished(
                    bot.gameObject))
            {
                return false;
            }
        }

        return true;
    }

    // =========================================================
    // RESULTADO
    // =========================================================

    private void RegisterResults()
    {
        CareerManager career =
            CareerManager.Instance;

        if (career == null)
        {
            Debug.LogError(
                "[CareerRaceController] CareerManager " +
                "não encontrado."
            );

            return;
        }

        List<Entry> entries =
            new List<Entry>();

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        int playerPosition =
            lapController.GetFinishPosition(
                player.gameObject
            );

        if (playerPosition <= 0)
        {
            Debug.LogError(
                "[CareerRaceController] Player terminou, " +
                "mas não possui posição válida."
            );

            return;
        }

        entries.Add(
            new Entry
            {
                go = player.gameObject,
                driverName = career.PlayerName,
                position = playerPosition,
                isPlayer = true
            }
        );

        // -----------------------------------------------------
        // BOTS
        // -----------------------------------------------------

        foreach (AICarController bot in activeBots)
        {
            if (string.IsNullOrWhiteSpace(
                    bot.CareerDriverName))
            {
                Debug.LogError(
                    "[CareerRaceController] O bot " +
                    GetPath(bot.transform) +
                    " não possui Career Driver Name."
                );

                return;
            }

            int position =
                lapController.GetFinishPosition(
                    bot.gameObject
                );

            if (position <= 0)
            {
                Debug.LogError(
                    "[CareerRaceController] O bot " +
                    GetPath(bot.transform) +
                    " (" +
                    bot.CareerDriverName +
                    ") não possui posição válida."
                );

                return;
            }

            if (career.GetDriver(
                    bot.CareerDriverName) == null)
            {
                Debug.LogError(
                    "[CareerRaceController] O piloto '" +
                    bot.CareerDriverName +
                    "' (" +
                    GetPath(bot.transform) +
                    ") não existe na carreira."
                );

                return;
            }

            entries.Add(
                new Entry
                {
                    go = bot.gameObject,
                    driverName =
                        bot.CareerDriverName,
                    position = position,
                    isPlayer = false
                }
            );
        }

        // -----------------------------------------------------
        // RESULTADOS BRUTOS
        // -----------------------------------------------------

        Debug.Log("====================================");
        Debug.Log("🔎 RESULTADOS BRUTOS");

        foreach (Entry e in entries)
        {
            Debug.Log(
                "PILOTO: " +
                e.driverName +
                " | POSIÇÃO: " +
                e.position +
                " | OBJETO: " +
                GetPath(e.go.transform) +
                " (ID " +
                e.go.GetInstanceID() +
                ")"
            );
        }

        Debug.Log("====================================");

        // -----------------------------------------------------
        // VALIDA
        // -----------------------------------------------------

        if (!ValidateEntries(entries))
        {
            Debug.LogError(
                "[CareerRaceController] Resultado não foi " +
                "registrado porque a classificação é inválida."
            );

            return;
        }

        // -----------------------------------------------------
        // ORDENA
        // -----------------------------------------------------

        entries.Sort(
            (a, b) =>
                a.position.CompareTo(b.position)
        );

        Debug.Log("====================================");
        Debug.Log("🏁 CLASSIFICAÇÃO FINAL");

        foreach (Entry e in entries)
        {
            Debug.Log(
                e.position +
                "º - " +
                e.driverName
            );
        }

        Debug.Log("====================================");

        // -----------------------------------------------------
        // CRIA RESULTADOS DO CAREER MANAGER
        // -----------------------------------------------------

        List<CareerManager.RaceResult> results =
            new List<CareerManager.RaceResult>();

        foreach (Entry e in entries)
        {
            results.Add(
                new CareerManager.RaceResult(
                    e.driverName,
                    e.position,
                    career.GetPointsForPosition(
                        e.position
                    )
                )
            );
        }

        // -----------------------------------------------------
        // REGISTRA
        // -----------------------------------------------------

        Debug.Log("====================================");
        Debug.Log(
            "🏆 REGISTRANDO RESULTADO DA CARREIRA"
        );

        Debug.Log(
            "🏁 " +
            career.GetCurrentRaceName()
        );

        Debug.Log("====================================");

        foreach (
            CareerManager.RaceResult result
            in results
        )
        {
            Debug.Log(
                result.position +
                "º - " +
                result.driverName +
                " | +" +
                result.points +
                " pontos"
            );
        }

        career.RegisterRaceResults(
            results
        );

        resultRegistered = true;

        Debug.Log("====================================");
        Debug.Log(
            "✅ RESULTADO REGISTRADO NO CAMPEONATO!"
        );

        Debug.Log(
            "Corrida atual agora: " +
            career.CurrentRace
        );

        Debug.Log("====================================");
    }

    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    private bool ValidateEntries(
        List<Entry> entries
    )
    {
        bool ok = true;

        if (entries.Count != expectedCars)
        {
            Debug.LogError(
                "[CareerRaceController] Número incorreto de " +
                "pilotos: " +
                entries.Count +
                ". Esperado: " +
                expectedCars
            );

            ok = false;
        }

        // -----------------------------------------------------
        // NOMES REPETIDOS
        // -----------------------------------------------------

        Dictionary<string, List<Entry>> byName =
            new Dictionary<string, List<Entry>>();

        foreach (Entry e in entries)
        {
            if (!byName.ContainsKey(
                    e.driverName))
            {
                byName[e.driverName] =
                    new List<Entry>();
            }

            byName[e.driverName].Add(e);
        }

        foreach (
            KeyValuePair<string, List<Entry>> pair
            in byName
        )
        {
            if (pair.Value.Count > 1)
            {
                Debug.LogError(
                    "[CareerRaceController] NOME REPETIDO '" +
                    pair.Key +
                    "' em " +
                    pair.Value.Count +
                    " carros: " +
                    Describe(pair.Value)
                );

                ok = false;
            }
        }

        // -----------------------------------------------------
        // POSIÇÕES
        // -----------------------------------------------------

        Dictionary<int, List<Entry>> byPosition =
            new Dictionary<int, List<Entry>>();

        foreach (Entry e in entries)
        {
            if (e.position < 1 ||
                e.position > expectedCars)
            {
                Debug.LogError(
                    "[CareerRaceController] Posição fora " +
                    "do intervalo: " +
                    e.position +
                    " | Piloto: " +
                    e.driverName
                );

                ok = false;

                continue;
            }

            if (!byPosition.ContainsKey(
                    e.position))
            {
                byPosition[e.position] =
                    new List<Entry>();
            }

            byPosition[e.position].Add(e);
        }

        foreach (
            KeyValuePair<int, List<Entry>> pair
            in byPosition
        )
        {
            if (pair.Value.Count > 1)
            {
                Debug.LogError(
                    "[CareerRaceController] POSIÇÃO DUPLICADA " +
                    pair.Key +
                    ": " +
                    Describe(pair.Value)
                );

                ok = false;
            }
        }

        for (
            int position = 1;
            position <= expectedCars;
            position++
        )
        {
            if (!byPosition.ContainsKey(
                    position))
            {
                Debug.LogError(
                    "[CareerRaceController] POSIÇÃO AUSENTE: " +
                    position
                );

                ok = false;
            }
        }

        return ok;
    }

    // =========================================================
    // UTILITÁRIOS
    // =========================================================

    private string Describe(
        List<Entry> list
    )
    {
        List<string> parts =
            new List<string>();

        foreach (Entry e in list)
        {
            parts.Add(
                e.driverName +
                " [" +
                GetPath(e.go.transform) +
                ", ID " +
                e.go.GetInstanceID() +
                "]"
            );
        }

        return string.Join(
            "  |  ",
            parts
        );
    }

    private string GetPath(
        Transform t
    )
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