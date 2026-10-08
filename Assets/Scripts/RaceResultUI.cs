using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaceResultUI : MonoBehaviour
{
    [Header("Painel")]
    [SerializeField] private GameObject raceResultPanel;

    [Header("Textos")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text resultsText;

    [Header("Classificação Final - SOMENTE NA ÚLTIMA CORRIDA")]
    [Tooltip("Opcional. Preencha apenas no AdTest.")]
    [SerializeField] private TMP_Text championshipTitle;

    [Tooltip("Opcional. Preencha apenas no AdTest.")]
    [SerializeField] private TMP_Text championshipText;

    [Header("Botão")]
    [SerializeField] private Button nextRaceButton;

    [Header("Garantias de clique")]
    [Tooltip("Força o Canvas do resultado a ficar em Screen Space - Overlay, acima de tudo.")]
    [SerializeField] private bool forceCanvasOnTop = true;

    [SerializeField] private int resultCanvasSortOrder = 100;

    [Tooltip("Desliga o Raycast Target de tudo que está no HUD.")]
    [SerializeField] private bool disableHudRaycasts = true;

    [Header("Atalho de teclado")]
    [Tooltip("Enter na tela de resultado executa a ação do botão.")]
    [SerializeField] private bool allowEnterShortcut = true;

    private CareerManager career;
    private LapController lapController;

    // Guarda qual corrida estava acontecendo quando a cena começou.
    private int raceIndexAtStart;
    private string raceNameAtStart;

    private bool resultShown = false;
    private bool loadingNextScene = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        career = CareerManager.Instance;

        lapController =
            FindFirstObjectByType<LapController>();

        if (career == null)
        {
            Debug.LogWarning(
                "[RaceResultUI] CareerManager não encontrado."
            );

            return;
        }

        // Guarda os dados da corrida ANTES do CareerManager
        // avançar para a próxima.
        raceIndexAtStart =
            career.CurrentRace;

        raceNameAtStart =
            career.GetCurrentRaceName();

        // -----------------------------------------------------
        // PAINEL
        // -----------------------------------------------------

        if (raceResultPanel != null)
            raceResultPanel.SetActive(false);

        // -----------------------------------------------------
        // CLASSIFICAÇÃO FINAL
        // -----------------------------------------------------
        //
        // Esses objetos só existem no AdTest.
        // Portanto, nas outras pistas podem ser null.
        //

        if (championshipTitle != null)
            championshipTitle.gameObject.SetActive(false);

        if (championshipText != null)
            championshipText.gameObject.SetActive(false);

        // -----------------------------------------------------
        // BOTÃO
        // -----------------------------------------------------

        if (nextRaceButton != null)
        {
            nextRaceButton.onClick.RemoveAllListeners();

            nextRaceButton.onClick.AddListener(
                OnNextRaceClicked
            );

            if (nextRaceButton.onClick.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning(
                    "[RaceResultUI] O botão também possui evento " +
                    "configurado no Inspector."
                );
            }
        }

        Debug.Log(
            "[RaceResultUI] Preparado para: " +
            raceNameAtStart
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (resultShown)
        {
            HandleKeyboardShortcut();
            return;
        }

        // -----------------------------------------------------
        // GARANTE CAREER MANAGER
        // -----------------------------------------------------

        if (career == null)
        {
            career =
                CareerManager.Instance;

            if (career == null)
                return;
        }

        // -----------------------------------------------------
        // GARANTE LAP CONTROLLER
        // -----------------------------------------------------

        if (lapController == null)
        {
            lapController =
                FindFirstObjectByType<LapController>();

            if (lapController == null)
                return;
        }

        // -----------------------------------------------------
        // CORRIDA AINDA NÃO TERMINOU
        // -----------------------------------------------------

        if (!lapController.IsRaceFinished())
            return;

        // -----------------------------------------------------
        // CAREER RACE CONTROLLER AINDA NÃO REGISTROU
        // -----------------------------------------------------

        if (career.CurrentRace <= raceIndexAtStart)
            return;

        if (career.LastRaceResults == null ||
            career.LastRaceResults.Count == 0)
        {
            return;
        }

        ShowResult();
    }

    // =========================================================
    // MOSTRA RESULTADO
    // =========================================================

    private void ShowResult()
    {
        if (resultShown)
            return;

        resultShown = true;

        // -----------------------------------------------------
        // PAINEL
        // -----------------------------------------------------

        if (raceResultPanel != null)
            raceResultPanel.SetActive(true);

        PrepareUIForClicks();

        // -----------------------------------------------------
        // TÍTULO
        // -----------------------------------------------------

        if (title != null)
            title.text = raceNameAtStart;

        // -----------------------------------------------------
        // RESULTADO DA CORRIDA
        // -----------------------------------------------------

        if (resultsText != null)
        {
            List<CareerManager.RaceResult> results =
                new List<CareerManager.RaceResult>(
                    career.LastRaceResults
                );

            results.Sort(
                (a, b) =>
                    a.position.CompareTo(b.position)
            );

            StringBuilder text =
                new StringBuilder();

            foreach (
                CareerManager.RaceResult result
                in results
            )
            {
                text.AppendLine(
                    result.position +
                    "º  " +
                    result.driverName +
                    "    +" +
                    result.points +
                    " PTS"
                );
            }

            resultsText.text =
                text.ToString();
        }

        // -----------------------------------------------------
        // ÚLTIMA CORRIDA?
        // -----------------------------------------------------

        bool isFinalRace =
            career.IsChampionshipFinished();

        ConfigureFinalChampionshipUI(
            isFinalRace
        );

        // -----------------------------------------------------
        // BOTÃO
        // -----------------------------------------------------

        UpdateNextButton(
            isFinalRace
        );

        Debug.Log(
            "[RaceResultUI] Resultado exibido: " +
            raceNameAtStart
        );

        if (isFinalRace)
        {
            Debug.Log(
                "[RaceResultUI] 🏆 ESTA FOI A ÚLTIMA CORRIDA."
            );
        }
    }

    // =========================================================
    // CLASSIFICAÇÃO FINAL
    // =========================================================

    private void ConfigureFinalChampionshipUI(
        bool isFinalRace
    )
    {
        // -----------------------------------------------------
        // TÍTULO FINAL
        // -----------------------------------------------------

        if (championshipTitle != null)
        {
            championshipTitle.gameObject.SetActive(
                isFinalRace
            );

            if (isFinalRace)
            {
                championshipTitle.text =
                    "CLASSIFICAÇÃO FINAL";
            }
        }

        // -----------------------------------------------------
        // TEXTO FINAL
        // -----------------------------------------------------

        if (championshipText != null)
        {
            championshipText.gameObject.SetActive(
                isFinalRace
            );

            if (!isFinalRace)
                return;

            List<CareerManager.DriverData> standings =
                career.GetChampionshipStandings();

            StringBuilder text =
                new StringBuilder();

            foreach (
                CareerManager.DriverData driver
                in standings
            )
            {
                text.AppendLine(
                    GetOrdinal(driverPosition: GetDriverPosition(
                        standings,
                        driver
                    )) +
                    "  " +
                    driver.name +
                    "    " +
                    driver.points +
                    " PTS"
                );
            }

            CareerManager.DriverData champion =
                career.GetChampion();

            if (champion != null)
            {
                text.AppendLine();
                text.AppendLine(
                    "🏆 CAMPEÃO: " +
                    champion.name
                );
            }

            championshipText.text =
                text.ToString();
        }
    }

    // =========================================================
    // POSIÇÃO DO PILOTO
    // =========================================================

    private int GetDriverPosition(
        List<CareerManager.DriverData> standings,
        CareerManager.DriverData driver
    )
    {
        for (int i = 0;
             i < standings.Count;
             i++)
        {
            if (standings[i] == driver)
                return i + 1;
        }

        return 0;
    }

    // =========================================================
    // ORDINAL
    // =========================================================

    private string GetOrdinal(
        int driverPosition
    )
    {
        return driverPosition + "º";
    }

    // =========================================================
    // GARANTE CLIQUES
    // =========================================================

    private void PrepareUIForClicks()
    {
        // -----------------------------------------------------
        // BOTÃO
        // -----------------------------------------------------

        if (nextRaceButton != null)
        {
            nextRaceButton.gameObject.SetActive(true);
            nextRaceButton.interactable = true;
        }

        // -----------------------------------------------------
        // CANVAS
        // -----------------------------------------------------

        if (forceCanvasOnTop &&
            raceResultPanel != null)
        {
            Canvas canvas =
                raceResultPanel.GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                Canvas root =
                    canvas.rootCanvas;

                if (root.renderMode !=
                    RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogWarning(
                        "[RaceResultUI] O Canvas '" +
                        root.name +
                        "' estava em " +
                        root.renderMode +
                        ". Mudando para " +
                        "Screen Space - Overlay."
                    );

                    root.renderMode =
                        RenderMode.ScreenSpaceOverlay;
                }

                root.sortingOrder =
                    resultCanvasSortOrder;

                if (root.GetComponent<
                        GraphicRaycaster>() == null)
                {
                    Debug.LogWarning(
                        "[RaceResultUI] O Canvas '" +
                        root.name +
                        "' não tinha GraphicRaycaster. " +
                        "Adicionando."
                    );

                    root.gameObject.AddComponent<
                        GraphicRaycaster>();
                }
            }
        }

        // -----------------------------------------------------
        // HUD
        // -----------------------------------------------------

        if (disableHudRaycasts)
        {
            RaceHUDController hud =
                FindFirstObjectByType<RaceHUDController>();

            if (hud != null)
            {
                foreach (
                    Graphic g
                    in hud.GetComponentsInChildren<
                        Graphic>(true)
                )
                {
                    g.raycastTarget =
                        false;
                }
            }
        }
    }

    // =========================================================
    // BOTÃO
    // =========================================================

    private void UpdateNextButton(
        bool isFinalRace
    )
    {
        if (nextRaceButton == null)
            return;

        TMP_Text buttonText =
            nextRaceButton.GetComponentInChildren<
                TMP_Text
            >();

        if (buttonText == null)
            return;

        if (isFinalRace)
        {
            buttonText.text =
                "VOLTAR AO MENU";
        }
        else
        {
            buttonText.text =
                "PRÓXIMA CORRIDA";
        }
    }

    // =========================================================
    // ATALHO ENTER
    // =========================================================

    private void HandleKeyboardShortcut()
    {
        if (!allowEnterShortcut ||
            loadingNextScene)
        {
            return;
        }

        Keyboard kb =
            Keyboard.current;

        if (kb == null)
            return;

        if (kb.enterKey.wasPressedThisFrame ||
            kb.numpadEnterKey.wasPressedThisFrame)
        {
            Debug.Log(
                "[RaceResultUI] Enter pressionado."
            );

            OnNextRaceClicked();
        }
    }

    // =========================================================
    // CLIQUE / AÇÃO DO BOTÃO
    // =========================================================

    public void OnNextRaceClicked()
    {
        if (loadingNextScene)
            return;

        Debug.Log(
            "[RaceResultUI] Botão acionado."
        );

        if (career == null)
        {
            Debug.LogError(
                "[RaceResultUI] CareerManager não encontrado."
            );

            return;
        }

        // =====================================================
        // CAMPEONATO TERMINOU
        // =====================================================

        if (career.IsChampionshipFinished())
        {
            string menuScene =
                "MenuApex";

            if (!Application.CanStreamedLevelBeLoaded(
                    menuScene))
            {
                Debug.LogError(
                    "[RaceResultUI] A cena '" +
                    menuScene +
                    "' não está no Build Profile."
                );

                return;
            }

            loadingNextScene = true;

            Debug.Log(
                "[RaceResultUI] 🏆 Campeonato encerrado. " +
                "Voltando ao menu."
            );

            Time.timeScale = 1f;

            SceneManager.LoadScene(
                menuScene
            );

            return;
        }

        // =====================================================
        // PRÓXIMA CORRIDA
        // =====================================================

        string nextScene =
            career.GetCurrentRaceScene();

        if (string.IsNullOrWhiteSpace(
                nextScene))
        {
            Debug.LogError(
                "[RaceResultUI] Próxima cena não definida."
            );

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                nextScene))
        {
            Debug.LogError(
                "[RaceResultUI] A cena '" +
                nextScene +
                "' não está no Build Profile."
            );

            return;
        }

        loadingNextScene = true;

        Debug.Log(
            "[RaceResultUI] Indo para próxima corrida: " +
            nextScene
        );

        Time.timeScale = 1f;

        SceneManager.LoadScene(
            nextScene
        );
    }
}