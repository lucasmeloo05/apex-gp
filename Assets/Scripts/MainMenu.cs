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

    // Do seu colega, mantido como estava
    public void OnTrack1Clicked()
    {
        SceneManager.LoadScene("BraTest");
    }

    // Para as outras pistas: o nome da cena é digitado no On Click do botão
    public void LoadTrack(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}