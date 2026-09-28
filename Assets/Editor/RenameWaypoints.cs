using UnityEngine;
using UnityEditor;

public class RenameWaypoints
{
    [MenuItem("Tools/Renomear Waypoints")]
    public static void Rename()
    {
        GameObject parent = GameObject.Find("AI Waypoints");

        if (parent == null)
        {
            Debug.LogError("Não encontrei o objeto 'AI Waypoints'!");
            return;
        }

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform waypoint = parent.transform.GetChild(i);

            waypoint.name = $"WP{i:00}";
        }

        Debug.Log($"Foram renomeados {parent.transform.childCount} waypoints!");
    }
}