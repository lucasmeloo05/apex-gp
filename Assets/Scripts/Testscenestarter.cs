using UnityEngine;

/// <summary>
/// Coloque em um GameObject vazio de uma CENA DE TESTE (não na prefab).
/// Entrega os waypoints da cena para as IAs e inicia a corrida de todos os carros.
/// Na cena principal, continue usando o RaceStartManager.
/// </summary>
public class TestSceneStarter : MonoBehaviour
{
    [Tooltip("Pai dos waypoints desta cena (os filhos, em ordem, formam a pista). Opcional: sem isso só o jogador anda.")]
    [SerializeField] private Transform waypointsRoot;

    [Tooltip("Tempo de espera antes de liberar os carros.")]
    [SerializeField] private float startDelay = 1f;

    private void Start()
    {
        Invoke(nameof(StartAll), startDelay);
    }

    private void StartAll()
    {
        Transform[] waypoints = null;

        if (waypointsRoot != null && waypointsRoot.childCount >= 2)
        {
            waypoints = new Transform[waypointsRoot.childCount];

            for (int i = 0; i < waypoints.Length; i++)
                waypoints[i] = waypointsRoot.GetChild(i);
        }
        else
        {
            Debug.LogWarning("[TestSceneStarter] Sem Waypoints Root: as IAs não terão pista para seguir.");
        }

        foreach (AICarController ai in FindObjectsByType<AICarController>(FindObjectsSortMode.None))
        {
            if (waypoints != null)
                ai.SetWaypoints(waypoints);

            ai.SetRaceStarted(true);
        }

        foreach (PlayerCarController player in FindObjectsByType<PlayerCarController>(FindObjectsSortMode.None))
            player.SetRaceStarted(true);
    }
}