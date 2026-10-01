using UnityEngine;
using UnityEngine.UI;

public class MenuAudioManager : MonoBehaviour
{
    [Header("Áudio do Menu")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource introSource;

    [Header("Volume")]
    [SerializeField] private Slider volumeSlider;

    [Header("Controle da Música")]
    [SerializeField] private Toggle musicToggle;

    private void Start()
    {
        // Música do menu
        musicSource.loop = true;
        musicSource.Play();

        // Som dos carros
        introSource.loop = false;
        introSource.Play();

        // Volume inicial
        volumeSlider.value = musicSource.volume;

        // Estado inicial da música
        musicToggle.isOn = true;

        // Eventos
        volumeSlider.onValueChanged.AddListener(ChangeVolume);
        musicToggle.onValueChanged.AddListener(ToggleMusic);
    }

    private void ChangeVolume(float volume)
    {
        musicSource.volume = volume;
    }

    private void ToggleMusic(bool isOn)
    {
        if (isOn)
        {
            musicSource.UnPause();
        }
        else
        {
            musicSource.Pause();
        }
    }
}