using UnityEngine;

public class CarTeamVisual : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private bool isPlayerCar = false;

    [Header("Sprite do carro")]
    [SerializeField] private SpriteRenderer carSpriteRenderer;

    [Header("Pinturas das equipes")]
    [SerializeField] private Sprite apexSprite;
    [SerializeField] private Sprite drowSprite;
    [SerializeField] private Sprite xeuSprite;
    [SerializeField] private Sprite chibaSprite;

    [Header("Piloto")]
    [Tooltip("Preenchido automaticamente durante a carreira.")]
    [SerializeField] private string driverName;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        ApplyVisual();
    }

    // =========================================================
    // PILOTO
    // =========================================================

    public void SetDriverName(string name)
    {
        driverName = name;

        ApplyVisual();
    }

    // =========================================================
    // APLICA VISUAL
    // =========================================================

    private void ApplyVisual()
    {
        if (carSpriteRenderer == null)
        {
            Debug.LogError(
                "[CarTeamVisual] SpriteRenderer não configurado em " +
                gameObject.name
            );

            return;
        }

        // =====================================================
        // CARREIRA
        // =====================================================

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            if (CareerManager.Instance == null)
            {
                Debug.LogWarning(
                    "[CarTeamVisual] CareerManager não encontrado."
                );

                return;
            }

            CareerManager.DriverData driver =
                CareerManager.Instance.GetDriver(
                    driverName
                );

            if (driver == null)
            {
                Debug.LogError(
                    "[CarTeamVisual] Piloto não encontrado: " +
                    driverName
                );

                return;
            }

            ApplyTeamVisual(
                driver.team
            );

            Debug.Log(
                "[CarTeamVisual] " +
                driverName +
                " → " +
                CareerManager.Instance.GetTeamName(
                    driver.team
                )
            );

            return;
        }

        // =====================================================
        // PLAYER NO MODO NORMAL/CARREIRA
        // =====================================================

        if (isPlayerCar)
        {
            if (CareerManager.Instance == null)
            {
                Debug.LogWarning(
                    "[CarTeamVisual] CareerManager não encontrado " +
                    "para o PlayerCar."
                );

                return;
            }

            ApplyTeamVisual(
                CareerManager.Instance.PlayerTeam
            );

            Debug.Log(
                "[CarTeamVisual] Player → " +
                CareerManager.Instance.GetTeamName(
                    CareerManager.Instance.PlayerTeam
                )
            );

            return;
        }

        // =====================================================
        // IA NO MODO NORMAL
        // =====================================================
        //
        // Não fazemos absolutamente nada.
        //
        // A IA mantém a sprite original do prefab.
        //

        Debug.Log(
            "[CarTeamVisual] IA normal → mantendo sprite original: " +
            gameObject.name
        );
    }

    // =========================================================
    // PINTURA DA EQUIPE
    // =========================================================

    private void ApplyTeamVisual(
        CareerManager.Team team
    )
    {
        switch (team)
        {
            case CareerManager.Team.DrowGP:

                carSpriteRenderer.sprite =
                    drowSprite;

                break;

            case CareerManager.Team.XeuMotorsport:

                carSpriteRenderer.sprite =
                    xeuSprite;

                break;

            case CareerManager.Team.ScuderiaImperio:

                carSpriteRenderer.sprite =
                    apexSprite;

                break;

            case CareerManager.Team.ChibaRacing:

                carSpriteRenderer.sprite =
                    chibaSprite;

                break;
        }
    }
}