using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject mainPanel;
    public GameObject trackPanel;
    public GameObject optionsPanel;

    void Start()
    {
        ShowMain();
    }

    public void ShowMain()
    {
        mainPanel.SetActive(true);
        trackPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }

    public void OnPlayClicked()
    {
        mainPanel.SetActive(false);
        trackPanel.SetActive(true);
    }

    public void OnOptionsClicked()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        ShowMain();
    }

    public void OnExitClicked()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnTrack1Clicked()
    {
        SceneManager.LoadScene("BraTest");
    }
}