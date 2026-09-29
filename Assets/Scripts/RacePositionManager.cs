using UnityEngine;

public class RacePositionManager : MonoBehaviour
{
    [Header("Corrida")]
    [SerializeField] private LapController lapController;

    [Header("Player")]
    [SerializeField] private PlayerCarController player;

    [Header("Bots")]
    [SerializeField] private AICarController[] bots;

    [Header("Waypoints")]
    [SerializeField] private int totalWaypoints = 32;

    private void Update()
    {
        if (lapController == null || player == null)
            return;

        CalculatePlayerPosition();
    }

    private void CalculatePlayerPosition()
    {
        GameObject playerObject =
            player.gameObject;

        int playerProgress =
            GetTotalProgress(playerObject);

        int position = 1;

        // =====================================================
        // COMPARA COM CADA BOT
        // =====================================================

        foreach (AICarController bot in bots)
        {
            if (bot == null)
                continue;

            int botProgress =
                GetTotalProgress(bot.gameObject);

            if (botProgress > playerProgress)
            {
                position++;
            }
        }

        Debug.Log(
            "PLAYER: P" +
            position +
            " | Progresso: " +
            playerProgress
        );
    }

    // =========================================================
    // PROGRESSO TOTAL DA CORRIDA
    // =========================================================

    private int GetTotalProgress(GameObject car)
    {
        int lap =
            lapController.GetCurrentLap(car);

        int waypoint = 0;

        // =====================================================
        // PLAYER
        // =====================================================

        PlayerWaypointProgress playerProgress =
            car.GetComponent<PlayerWaypointProgress>();

        if (playerProgress != null)
        {
            waypoint =
                playerProgress.CurrentWaypoint;
        }

        // =====================================================
        // IA
        // =====================================================

        AICarController ai =
            car.GetComponent<AICarController>();

        if (ai != null)
        {
            waypoint =
                ai.CurrentWaypoint;
        }

        // =====================================================
        // PROGRESSO TOTAL
        // =====================================================

        return
            (lap * totalWaypoints) +
            waypoint;
    }
}