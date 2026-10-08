using UnityEngine;
using UnityEngine.SceneManagement;

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

    // Track 1
    public void OnTrack1Clicked()
    {
        LoadingController.Instance.LoadGame("BraTest");
    }

    // Track 2
    public void OnTrack2Click()
    {
        LoadingController.Instance.LoadGame("ItaTest");
    }

    // Track 3
    public void OnTrack3Click()
    {
        LoadingController.Instance.LoadGame("MonzaTest");
    }

    // Track 4
    public void OnTrack4Click()
    {
        LoadingController.Instance.LoadGame("AdTest");
    }

    // Para as outras pistas:
    // o nome da cena pode ser definido diretamente
    // no On Click() do botão.
    public void LoadTrack(string sceneName)
    {
        LoadingController.Instance.LoadGame("AdTest");
    }
}