using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    public static LoadingController Instance { get; private set; }

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private float loadingTime = 5f;

    private bool isLoading;

    private void Awake()
    {
        Instance = this;
    }

    public void LoadGame(string sceneName)
    {
        if (isLoading) return;   // evita clique duplo
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;
        loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (timer < loadingTime)
        {
            timer += Time.unscaledDeltaTime;
            if (progressBar != null) progressBar.value = timer / loadingTime;
            yield return null;
        }

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }
}