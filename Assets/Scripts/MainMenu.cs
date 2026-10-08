using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject careerPanel;
    public GameObject[] allPanels;
    // MainPanel, PlayPanel, TrackPanel, OptionsPanel, CommandsPanel, CareerPanel

    void Start()
    {
        ShowMain();
    }

    // Abre um painel e esconde todos os outros
    public void OpenPanel(GameObject panel)
    {
        foreach (GameObject p in allPanels)
        {
            if (p != null)
                p.SetActive(p == panel);
        }
    }

    public void ShowMain()
    {
        OpenPanel(mainPanel);
    }

    public void OnBackClicked()
    {
        ShowMain();
    }

    public void OnCareerClicked()
    {
        OpenPanel(careerPanel);
    }

    public void OnExitClicked()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // =========================================================
    // MODO NORMAL
    // =========================================================

    // Track 1
    public void OnTrack1Clicked()
    {
        ResetCareerForNormalRace();
        LoadingController.Instance.LoadGame("BraTest");
    }

    // Track 2
    public void OnTrack2Click()
    {
        ResetCareerForNormalRace();
        LoadingController.Instance.LoadGame("ItaTest");
    }

    // Track 3
    public void OnTrack3Click()
    {
        ResetCareerForNormalRace();
        LoadingController.Instance.LoadGame("MonzaTest");
    }

    // Track 4
    public void OnTrack4Click()
    {
        ResetCareerForNormalRace();
        LoadingController.Instance.LoadGame("AdTest");
    }

    // =========================================================
    // RESET DA CARREIRA AO ENTRAR NO MODO NORMAL
    // =========================================================

    private void ResetCareerForNormalRace()
    {
        if (CareerManager.Instance != null)
        {
            CareerManager.Instance.ResetCareer();

            Debug.Log(
                "[MainMenu] Carreira resetada. " +
                "Iniciando corrida no modo normal."
            );
        }
    }

    // =========================================================
    // OUTRAS PISTAS
    // =========================================================

    public void LoadTrack(string sceneName)
    {
        ResetCareerForNormalRace();
        LoadingController.Instance.LoadGame(sceneName);
    }
}