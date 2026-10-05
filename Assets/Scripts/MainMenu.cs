using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject[] allPanels; // MainPanel, PlayPanel, TrackPanel, OptionsPanel, CommandsPanel

    void Start()
    {
        ShowMain();
    }

    // Abre um painel e esconde todos os outros
    public void OpenPanel(GameObject panel)
    {
        foreach (GameObject p in allPanels)
            p.SetActive(p == panel);
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
        Debug.Log("Modo Carreira: em desenvolvimento");
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
        SceneManager.LoadScene("BraTest");
    }

    // Track 2
    public void OnTrack2Click()
    {
        SceneManager.LoadScene("ItaTest");
    }

    // Track 3
    public void OnTrack3Click()
    {
        SceneManager.LoadScene("MonzaTest");
    }

    // Para as outras pistas:
    // o nome da cena pode ser definido diretamente
    // no On Click() do botão.
    public void LoadTrack(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}