using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RacePauseController : MonoBehaviour
{
    [Header("Painel de pausa")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Cena do menu")]
    [Tooltip("Cena carregada ao sair da corrida. Precisa estar em File > Build Profiles.")]
    [SerializeField] private string menuSceneName = "MenuApex";

    [Header("Garantias de clique")]
    [Tooltip("Força o Canvas da pausa a ficar em Screen Space - Overlay, acima de tudo.")]
    [SerializeField] private bool forceCanvasOnTop = true;
    [SerializeField] private int pauseCanvasSortOrder = 100;
    [Tooltip("Desliga o Raycast Target de tudo que está no HUD (ele nunca precisa de cliques).")]
    [SerializeField] private bool disableHudRaycasts = true;

    [Header("Visual dos botões")]
    [Tooltip("Aplica cores de hover/clique visíveis nos botões de pausa.")]
    [SerializeField] private bool styleButtons = true;
    [SerializeField] private Color highlightedColor = new Color(0.78f, 0.89f, 1f, 1f);
    [SerializeField] private Color pressedColor = new Color(0.55f, 0.72f, 0.95f, 1f);

    private bool isPaused;
    private bool isLeavingRace;
    private float previousTimeScale = 1f;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeRace);
            resumeButton.onClick.AddListener(ResumeRace);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitRace);
            quitButton.onClick.AddListener(QuitRace);
        }

        StyleButton(resumeButton);
        StyleButton(quitButton);

        PrepareUIForClicks();

        if (resumeButton == null || quitButton == null || pausePanel == null)
        {
            Debug.LogError(
                "[RacePauseController] Referências faltando: confira Pause Panel, " +
                "Resume Button e Quit Button no Inspector desta cena."
            );
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            Debug.LogError(
                "[RacePauseController] Não há EventSystem na cena. " +
                "Crie em GameObject > UI > Event System."
            );
        }
    }

    private void Update()
    {
        if (isLeavingRace)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        if (isPaused)
            ResumeRace();
        else
            PauseRace();
    }

    // =========================================================
    // GARANTE QUE NADA BLOQUEIE O CLIQUE
    // =========================================================

    private void PrepareUIForClicks()
    {
        // 1) Canvas da pausa acima de tudo, em Overlay, com GraphicRaycaster
        if (forceCanvasOnTop && pausePanel != null)
        {
            Canvas canvas = pausePanel.GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                Canvas root = canvas.rootCanvas;

                if (root.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogWarning(
                        "[RacePauseController] O Canvas '" + root.name + "' estava em " +
                        root.renderMode + ". Mudando para Screen Space - Overlay."
                    );

                    root.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                root.sortingOrder = Mathf.Max(root.sortingOrder, pauseCanvasSortOrder);

                if (root.GetComponent<GraphicRaycaster>() == null)
                {
                    Debug.LogWarning(
                        "[RacePauseController] O Canvas '" + root.name +
                        "' não tinha GraphicRaycaster. Adicionando."
                    );

                    root.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
        }

        // 2) HUD nunca precisa receber cliques
        if (disableHudRaycasts)
        {
            RaceHUDController hud = FindFirstObjectByType<RaceHUDController>();

            if (hud != null)
            {
                foreach (Graphic g in hud.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;
            }
        }
    }

    // =========================================================
    // VISUAL DO BOTÃO (hover / clique visíveis)
    // =========================================================

    private void StyleButton(Button button)
    {
        if (!styleButtons || button == null)
            return;

        button.interactable = true;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;

        colors.normalColor = Color.white;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        // depois do clique o botão fica "selecionado": não deixa a cor presa
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;

        button.colors = colors;
    }

    // =========================================================
    // PAUSA
    // =========================================================

    public void PauseRace()
    {
        if (isPaused || isLeavingRace)
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        PrepareUIForClicks();

        if (pausePanel != null)
            pausePanel.SetActive(true);

        // começa sem nenhum botão "selecionado"
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        Debug.Log("[RacePauseController] Corrida pausada.");
    }

    public void ResumeRace()
    {
        Debug.Log("[RacePauseController] Botão CONTINUAR acionado.");

        if (!isPaused || isLeavingRace)
            return;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = previousTimeScale;
        isPaused = false;

        Debug.Log("[RacePauseController] Corrida retomada.");
    }

    public void QuitRace()
    {
        Debug.Log("[RacePauseController] Botão SAIR acionado.");

        if (isLeavingRace)
            return;

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            Debug.LogError("[RacePauseController] 'Menu Scene Name' está vazio.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError(
                "[RacePauseController] A cena '" + menuSceneName +
                "' não está no Build Profile (File > Build Profiles). " +
                "Adicione-a à Scene List."
            );

            return;
        }

        isLeavingRace = true;
        Time.timeScale = 1f;

        // Não registra resultados nem avança a etapa.
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnDestroy()
    {
        if (isPaused)
            Time.timeScale = 1f;
    }
}