using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CareerSetupUI : MonoBehaviour
{
    [Header("Campos da carreira")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Dropdown teamDropdown;
    [SerializeField] private Slider lapsSlider;
    [SerializeField] private TMP_Text lapsText;

    private void Start()
    {
        if (lapsSlider != null)
        {
            lapsSlider.minValue = 3;
            lapsSlider.maxValue = 10;
            lapsSlider.wholeNumbers = true;
            lapsSlider.value = 5;

            lapsSlider.onValueChanged.AddListener(OnLapsChanged);
        }

        UpdateLapsText();
    }

    private void OnLapsChanged(float value)
    {
        UpdateLapsText();
    }

    private void UpdateLapsText()
    {
        if (lapsText == null || lapsSlider == null)
            return;

        lapsText.text = "LAPS: " + Mathf.RoundToInt(lapsSlider.value);
    }

    public void StartCareer()
    {
        if (CareerManager.Instance == null)
        {
            Debug.LogError("CareerSetupUI: CareerManager não encontrado!");
            return;
        }

        string playerName = nameInput != null
            ? nameInput.text.Trim()
            : "";

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Digite um nome para o piloto.");
            return;
        }

        int teamIndex = teamDropdown != null
            ? teamDropdown.value
            : 0;

        int laps = lapsSlider != null
            ? Mathf.RoundToInt(lapsSlider.value)
            : 5;

        CareerManager.Team selectedTeam =
            (CareerManager.Team)teamIndex;

        CareerManager.Instance.StartCareer(
            playerName,
            selectedTeam,
            laps
        );

        Debug.Log(
            "🏆 CARREIRA CRIADA\n" +
            "Piloto: " + playerName + "\n" +
            "Equipe: " + CareerManager.Instance.GetTeamName(selectedTeam) + "\n" +
            "Voltas: " + laps
        );

        SceneManager.LoadScene("BraTest");
    }
}