using System;
using System.Collections.Generic;
using UnityEngine;

public class CareerManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static CareerManager Instance { get; private set; }

    // =========================================================
    // EQUIPES
    // =========================================================

    public enum Team
    {
        DrowGP,
        XeuMotorsport,
        ScuderiaImperio,
        ChibaRacing
    }

    // =========================================================
    // DADOS DO PILOTO
    // =========================================================

    [Serializable]
    public class DriverData
    {
        public string name;
        public Team team;
        public int points;

        public DriverData(string name, Team team)
        {
            this.name = name;
            this.team = team;
            this.points = 0;
        }
    }

    // =========================================================
    // DADOS DA CORRIDA
    // =========================================================

    [Serializable]
    public class RaceResult
    {
        public string driverName;
        public int position;
        public int points;

        public RaceResult(string driverName, int position, int points)
        {
            this.driverName = driverName;
            this.position = position;
            this.points = points;
        }
    }

    // =========================================================
    // DADOS DA CARREIRA
    // =========================================================

    [Header("Dados do jogador")]
    [SerializeField] private string playerName;
    [SerializeField] private Team playerTeam;
    [SerializeField] private int lapsPerRace = 5;

    [Header("Campeonato")]
    [SerializeField] private int currentRace = 0;

    [SerializeField]
    private List<DriverData> drivers = new List<DriverData>();

    [SerializeField]
    private List<RaceResult> lastRaceResults = new List<RaceResult>();

    // =========================================================
    // PONTUAÇÃO
    // =========================================================

    private readonly int[] pointsTable =
    {
        25, // 1º
        18, // 2º
        15, // 3º
        12, // 4º
        10, // 5º
        8,  // 6º
        6,  // 7º
        4   // 8º
    };

    // =========================================================
    // PROPRIEDADES PÚBLICAS
    // =========================================================

    public string PlayerName => playerName;
    public Team PlayerTeam => playerTeam;
    public int LapsPerRace => lapsPerRace;
    public int CurrentRace => currentRace;

    public List<DriverData> Drivers => drivers;

    public List<RaceResult> LastRaceResults => lastRaceResults;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // INICIAR CARREIRA
    // =========================================================

    public void StartCareer(string name, Team team, int laps)
    {
        // Nome
        playerName = name;

        // Equipe
        playerTeam = team;

        // Limita as voltas entre 3 e 10
        lapsPerRace = Mathf.Clamp(laps, 3, 10);

        // Primeira corrida
        currentRace = 0;

        // Limpa dados antigos
        drivers.Clear();
        lastRaceResults.Clear();

        // Cria os pilotos
        CreateDrivers();

        Debug.Log("====================================");
        Debug.Log("🏆 NOVA CARREIRA");
        Debug.Log("====================================");
        Debug.Log("Piloto: " + playerName);
        Debug.Log("Equipe: " + GetTeamName(playerTeam));
        Debug.Log("Voltas por corrida: " + lapsPerRace);
        Debug.Log("------------------------------------");

        foreach (DriverData driver in drivers)
        {
            Debug.Log(
                driver.name +
                " | " +
                GetTeamName(driver.team)
            );
        }

        Debug.Log("====================================");
    }

    // =========================================================
    // CRIAR PILOTOS
    // =========================================================

    private void CreateDrivers()
    {
        // -----------------------------------------------------
        // DROW GP
        // -----------------------------------------------------

        AddDriverOrPlayer(
            "Enzo Moretti",
            Team.DrowGP
        );

        AddDriver(
            "Adrian Keller",
            Team.DrowGP
        );

        // -----------------------------------------------------
        // XEU MOTORSPORT
        // -----------------------------------------------------

        AddDriverOrPlayer(
            "Lucas Ferreira",
            Team.XeuMotorsport
        );

        AddDriver(
            "Matteo Rossi",
            Team.XeuMotorsport
        );

        // -----------------------------------------------------
        // SCUDERIA IMPERIO
        // -----------------------------------------------------

        AddDriverOrPlayer(
            "Rafael Costa",
            Team.ScuderiaImperio
        );

        AddDriver(
            "Victor Bianchi",
            Team.ScuderiaImperio
        );

        // -----------------------------------------------------
        // CHIBA RACING
        // -----------------------------------------------------

        AddDriverOrPlayer(
            "Kenji Takahashi",
            Team.ChibaRacing
        );

        AddDriver(
            "Daniel Novak",
            Team.ChibaRacing
        );
    }

    // =========================================================
    // ADICIONAR PILOTO NORMAL
    // =========================================================

    private void AddDriver(string name, Team team)
    {
        drivers.Add(
            new DriverData(name, team)
        );
    }

    // =========================================================
    // ADICIONAR PILOTO OU JOGADOR
    // =========================================================

    private void AddDriverOrPlayer(string defaultName, Team team)
    {
        if (playerTeam == team)
        {
            drivers.Add(
                new DriverData(playerName, team)
            );
        }
        else
        {
            drivers.Add(
                new DriverData(defaultName, team)
            );
        }
    }

    // =========================================================
    // PONTOS DE UMA POSIÇÃO
    // =========================================================

    public int GetPointsForPosition(int position)
    {
        if (position < 1 || position > pointsTable.Length)
            return 0;

        return pointsTable[position - 1];
    }

    // =========================================================
    // REGISTRAR RESULTADO DA CORRIDA
    // =========================================================

    public void RegisterRaceResults(List<RaceResult> results)
    {
        if (results == null)
            return;

        lastRaceResults.Clear();

        foreach (RaceResult result in results)
        {
            lastRaceResults.Add(result);

            DriverData driver = GetDriver(result.driverName);

            if (driver != null)
            {
                driver.points += result.points;
            }
        }

        currentRace++;

        Debug.Log("====================================");
        Debug.Log("🏁 RESULTADO DA CORRIDA");
        Debug.Log("====================================");

        foreach (RaceResult result in results)
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

        Debug.Log("====================================");
    }

    // =========================================================
    // PROCURAR PILOTO
    // =========================================================

    public DriverData GetDriver(string name)
    {
        foreach (DriverData driver in drivers)
        {
            if (driver.name == name)
                return driver;
        }

        return null;
    }

    // =========================================================
    // PEGAR NOME DA EQUIPE
    // =========================================================

    public string GetTeamName(Team team)
    {
        switch (team)
        {
            case Team.DrowGP:
                return "Drow GP";

            case Team.XeuMotorsport:
                return "Xeu Motorsport";

            case Team.ScuderiaImperio:
                return "Scuderia Imperio";

            case Team.ChibaRacing:
                return "Chiba Racing";

            default:
                return "Unknown";
        }
    }

    // =========================================================
    // NOVA CORRIDA
    // =========================================================

    public void StartNextRace()
    {
        currentRace++;

        Debug.Log(
            "🏁 Iniciando corrida " +
            currentRace
        );
    }

    // =========================================================
    // RESETAR CARREIRA
    // =========================================================

    public void ResetCareer()
    {
        playerName = "";
        playerTeam = Team.DrowGP;
        lapsPerRace = 5;
        currentRace = 0;

        drivers.Clear();
        lastRaceResults.Clear();

        Debug.Log("🏁 Carreira resetada.");
    }
}