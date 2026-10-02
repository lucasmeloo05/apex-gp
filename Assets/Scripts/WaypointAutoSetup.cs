using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class WaypointAutoSetup
{
    [MenuItem("Apex GP/Preencher Waypoints Automaticamente")]
    public static void FillWaypoints()
    {
        // Procura o objeto principal dos waypoints
        GameObject waypointRoot = GameObject.Find("AI Waypoints");

        if (waypointRoot == null)
        {
            Debug.LogError(
                "[WaypointAutoSetup] Não encontrei um objeto chamado 'AI Waypoints' na cena."
            );

            return;
        }

        // Pega todos os filhos diretos
        List<Transform> waypoints = new List<Transform>();

        for (int i = 0; i < waypointRoot.transform.childCount; i++)
        {
            Transform child = waypointRoot.transform.GetChild(i);

            if (child.name.StartsWith("WP"))
            {
                waypoints.Add(child);
            }
        }

        if (waypoints.Count == 0)
        {
            Debug.LogError(
                "[WaypointAutoSetup] Nenhum waypoint encontrado dentro de 'AI Waypoints'."
            );

            return;
        }

        // Ordena pelo número:
        // WP00
        // WP01
        // WP02
        // ...
        // WP39
        waypoints = waypoints
            .OrderBy(GetWaypointNumber)
            .ToList();

        Debug.Log(
            $"[WaypointAutoSetup] Encontrados {waypoints.Count} waypoints."
        );

        // --------------------------------------------------
        // AI CARS
        // --------------------------------------------------

        AICarController[] aiCars =
            UnityEngine.Object.FindObjectsByType<AICarController>(
                FindObjectsSortMode.None
            );

        int aiCount = 0;

        foreach (AICarController ai in aiCars)
        {
            if (ai == null)
                continue;

            SerializedObject serializedAI = new SerializedObject(ai);

            SerializedProperty waypointProperty =
                serializedAI.FindProperty("waypoints");

            if (waypointProperty == null)
            {
                Debug.LogWarning(
                    $"[WaypointAutoSetup] O AICarController de '{ai.gameObject.name}' não possui um campo 'waypoints'."
                );

                continue;
            }

            waypointProperty.arraySize = waypoints.Count;

            for (int i = 0; i < waypoints.Count; i++)
            {
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue =
                    waypoints[i];
            }

            serializedAI.ApplyModifiedProperties();

            EditorUtility.SetDirty(ai);

            aiCount++;
        }

        // --------------------------------------------------
        // PLAYER
        // --------------------------------------------------

        PlayerWaypointProgress[] players =
            UnityEngine.Object.FindObjectsByType<PlayerWaypointProgress>(
                FindObjectsSortMode.None
            );

        int playerCount = 0;

        foreach (PlayerWaypointProgress player in players)
        {
            if (player == null)
                continue;

            SerializedObject serializedPlayer =
                new SerializedObject(player);

            SerializedProperty waypointProperty =
                serializedPlayer.FindProperty("waypoints");

            if (waypointProperty == null)
            {
                Debug.LogWarning(
                    $"[WaypointAutoSetup] O PlayerWaypointProgress de '{player.gameObject.name}' não possui um campo 'waypoints'."
                );

                continue;
            }

            waypointProperty.arraySize = waypoints.Count;

            for (int i = 0; i < waypoints.Count; i++)
            {
                waypointProperty.GetArrayElementAtIndex(i).objectReferenceValue =
                    waypoints[i];
            }

            serializedPlayer.ApplyModifiedProperties();

            EditorUtility.SetDirty(player);

            playerCount++;
        }

        // Salva as alterações
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[WaypointAutoSetup] CONCLUÍDO! " +
            $"Waypoints: {waypoints.Count} | " +
            $"AIs configurados: {aiCount} | " +
            $"Players configurados: {playerCount}"
        );

        EditorUtility.DisplayDialog(
            "Apex GP",
            $"Waypoints preenchidos com sucesso!\n\n" +
            $"Waypoints: {waypoints.Count}\n" +
            $"Carros AI: {aiCount}\n" +
            $"Players: {playerCount}",
            "OK"
        );
    }

    private static int GetWaypointNumber(Transform waypoint)
    {
        string number = new string(
            waypoint.name
                .Where(char.IsDigit)
                .ToArray()
        );

        if (int.TryParse(number, out int result))
            return result;

        return int.MaxValue;
    }
}