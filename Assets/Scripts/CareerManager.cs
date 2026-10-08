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
    [Tooltip("0 = primeira corrida, 1 = segunda, 2 = terceira.")]
    [SerializeField] private int currentRace = 0;

    [SerializeField]
    private List<DriverData> drivers = new List<DriverData>();

    [SerializeField]
    private List<RaceResult> lastRaceResults = new List<RaceResult>();


    // =========================================================
    // PISTAS DO CAMPEONATO
    // =========================================================

    private readonly string[] raceScenes =
    {
        "BraTest",
        "ItaTest",
        "MonzaTest",
        "AdTest"
    };

    private readonly string[] raceNames =
    {
        "GP Brasil",
        "GP Itália",
        "GP Monza",
        "Gp Abu Dhabi"
    };


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

    // 0 = primeira corrida
    // 1 = segunda corrida
    // 2 = terceira corrida
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

        // Começa na primeira corrida
        currentRace = 0;

        // Limpa dados antigos
        drivers.Clear();
        lastRaceResults.Clear();

        // Cria os 8 pilotos
        CreateDrivers();

        Debug.Log("====================================");
        Debug.Log("🏆 NOVA CARREIRA");
        Debug.Log("====================================");
        Debug.Log("Piloto: " + playerName);
        Debug.Log("Equipe: " + GetTeamName(playerTeam));
        Debug.Log("Voltas por corrida: " + lapsPerRace);
        Debug.Log("------------------------------------");

        Debug.Log(
            "Próxima corrida: " +
            GetCurrentRaceName() +
            " | Cena: " +
            GetCurrentRaceScene()
        );

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
        if (results == null || results.Count == 0)
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

        Debug.Log("====================================");
        Debug.Log(
            "🏁 RESULTADO - " +
            GetCurrentRaceName()
        );
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

        // A corrida terminou.
        // Avançamos para a próxima corrida.
        currentRace++;

        Debug.Log("====================================");

        if (IsChampionshipFinished())
        {
            Debug.Log("🏆 CAMPEONATO ENCERRADO!");
            Debug.Log("====================================");

            DriverData champion = GetChampion();

            if (champion != null)
            {
                Debug.Log(
                    "🏆 CAMPEÃO: " +
                    champion.name +
                    " | " +
                    champion.points +
                    " pontos"
                );
            }
        }
        else
        {
            Debug.Log(
                "Próxima corrida: " +
                GetCurrentRaceName() +
                " | Cena: " +
                GetCurrentRaceScene()
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
    // PEGAR DADOS DO JOGADOR
    // =========================================================

    public DriverData GetPlayerDriver()
    {
        return GetDriver(playerName);
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
    // INFORMAÇÕES DAS CORRIDAS
    // =========================================================

    public int GetRaceCount()
    {
        return raceScenes.Length;
    }


    public int GetCurrentRaceNumber()
    {
        return currentRace + 1;
    }


    public string GetCurrentRaceScene()
    {
        if (IsChampionshipFinished())
            return "";

        return raceScenes[currentRace];
    }


    public string GetCurrentRaceName()
    {
        if (IsChampionshipFinished())
            return "Campeonato encerrado";

        return raceNames[currentRace];
    }


    public bool IsChampionshipFinished()
    {
        return currentRace >= raceScenes.Length;
    }


    // =========================================================
    // NOVA CORRIDA
    // =========================================================

    public void StartNextRace()
    {
        if (IsChampionshipFinished())
        {
            Debug.Log("🏆 O campeonato já terminou.");
            return;
        }

        Debug.Log(
            "🏁 Preparando corrida " +
            GetCurrentRaceNumber() +
            "/" +
            GetRaceCount() +
            ": " +
            GetCurrentRaceName()
        );

        Debug.Log(
            "Cena: " +
            GetCurrentRaceScene()
        );
    }


    // =========================================================
    // GRID INVERTIDO
    // =========================================================

    public List<DriverData> GetGridOrder(bool inverted)
    {
        List<DriverData> grid = new List<DriverData>(drivers);

        if (inverted)
            grid.Reverse();

        return grid;
    }


    // =========================================================
    // CLASSIFICAÇÃO DO CAMPEONATO
    // =========================================================

    public List<DriverData> GetChampionshipStandings()
    {
        List<DriverData> standings =
            new List<DriverData>(drivers);

        standings.Sort(
            (a, b) => b.points.CompareTo(a.points)
        );

        return standings;
    }


    // =========================================================
    // CAMPEÃO
    // =========================================================

    public DriverData GetChampion()
    {
        if (drivers == null || drivers.Count == 0)
            return null;

        List<DriverData> standings =
            GetChampionshipStandings();

        return standings[0];
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