using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RaceLightsController : MonoBehaviour
{
    [Header("Luzes da largada")]
    [SerializeField] private Image[] lights;

    [Header("Cores")]
    [SerializeField] private Color offColor = new Color(0.15f, 0f, 0f);
    [SerializeField] private Color redColor = Color.red;

    [Header("Tempo da sequência")]
    [SerializeField] private float timeBetweenLights = 1f;
    [SerializeField] private float timeBeforeStart = 0.5f;

    private void Start()
    {
        TurnAllOff();

        StartCoroutine(LightSequence());
    }

    private IEnumerator LightSequence()
    {
        // Pequena espera antes da primeira luz
        yield return new WaitForSeconds(timeBeforeStart);

        // Acende as 5 luzes, uma por uma
        for (int i = 0; i < lights.Length; i++)
        {
            TurnOnLight(i);

            yield return new WaitForSeconds(timeBetweenLights);
        }

        // Todas as luzes ficam acesas por um pequeno instante
        yield return new WaitForSeconds(0.5f);

        // Apaga todas
        TurnAllOff();

        Debug.Log("🏁 LARGADA!");

        // Esconde completamente as luzes
        gameObject.SetActive(false);
    }

    public void TurnAllOff()
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].color = offColor;
        }
    }

    public void TurnOnLight(int index)
    {
        if (index < 0 || index >= lights.Length)
            return;

        if (lights[index] != null)
            lights[index].color = redColor;
    }
}