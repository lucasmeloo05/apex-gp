using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingDots : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string baseText = "Loading";
    [SerializeField] private int maxDots = 3;
    [SerializeField] private float interval = 0.4f;

    private Coroutine routine;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        routine = StartCoroutine(Animate());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }

    private IEnumerator Animate()
    {
        int dots = 0;
        while (true)
        {
            label.text = baseText + new string('.', dots);
            dots = (dots + 1) % (maxDots + 1);
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}