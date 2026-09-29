using UnityEngine;

public class PlayerWaypointProgress : MonoBehaviour
{
    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;

    [Header("Configuração")]
    [SerializeField] private float waypointReachDistance = 3f;

    [SerializeField] private int currentWaypoint = 0;

    public int CurrentWaypoint => currentWaypoint;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        UpdateWaypoint();
    }

    private void UpdateWaypoint()
    {
        Transform target = waypoints[currentWaypoint];

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance <= waypointReachDistance)
        {
            currentWaypoint++;

            if (currentWaypoint >= waypoints.Length)
            {
                currentWaypoint = 0;
            }
        }
    }
}