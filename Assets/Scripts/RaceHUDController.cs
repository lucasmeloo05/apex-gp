using UnityEngine;
using TMPro;

public class RaceHUDController : MonoBehaviour
{
    [Header("Textos da HUD")]
    [SerializeField] private TMP_Text lapText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text lapTimeText;

    [Header("Referências")]
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private LapController lapController;
    [SerializeField] private RacePositionManager racePositionManager;

    [Header("Configuração")]
    [SerializeField] private int totalCars = 8;

    private void Update()
    {
        UpdateSpeed();
        UpdateLap();
        UpdatePosition();
        UpdateLapTime();
    }

    // =========================================================
    // VOLTA
    // =========================================================

    private void UpdateLap()
    {
        if (lapText == null)
            return;

        int currentLap = 0;
        int configuredLaps = 0;

        if (lapController != null)
        {
            currentLap =
                lapController.GetCurrentLap(
                    playerRb.gameObject
                );

            configuredLaps =
                lapController.GetTotalLaps();
        }

        lapText.text =
            "LAP " +
            currentLap +
            "/" +
            configuredLaps;
    }

    // =========================================================
    // POSIÇÃO
    // =========================================================

    private void UpdatePosition()
    {
        if (positionText == null)
            return;

        int position = 1;

        if (racePositionManager != null)
        {
            position =
                racePositionManager.GetPlayerPosition();
        }

        positionText.text =
            "POSITION\n" +
            position +
            "/" +
            totalCars;
    }

    // =========================================================
    // VELOCIDADE
    // =========================================================

    private void UpdateSpeed()
    {
        if (speedText == null || playerRb == null)
            return;

        float speed =
            playerRb.linearVelocity.magnitude;

        float speedKmH =
            speed * 3.6f;

        speedText.text =
            Mathf.RoundToInt(speedKmH) +
            " km/h";
    }

    // =========================================================
    // TEMPO
    // =========================================================

    private void UpdateLapTime()
    {
        if (lapTimeText == null)
            return;

        float time = 0f;

        if (lapController != null)
        {
            time =
                lapController.GetRaceTime();
        }

        int minutes =
            Mathf.FloorToInt(time / 60f);

        int seconds =
            Mathf.FloorToInt(time % 60f);

        int milliseconds =
            Mathf.FloorToInt(
                (time * 1000f) % 1000f
            );

        lapTimeText.text =
            string.Format(
                "{0:00}:{1:00}.{2:000}",
                minutes,
                seconds,
                milliseconds
            );
    }
}