using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Main Menu")]
    [SerializeField] private CanvasGroup mainMenuGroup;
    [SerializeField] private Button firstSelectedButton;

    [Header("Panels")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Button creditsBackButton;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip startClip;

    [Header("Fade")]
    [SerializeField] private float fadeInTime = 1.25f;
    [SerializeField] private float startGameDelay = 0.35f;

    private bool loadingGame;

    private void Awake()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (mainMenuGroup != null)
        {
            mainMenuGroup.alpha = 0f;
            mainMenuGroup.interactable = false;
            mainMenuGroup.blocksRaycasts = false;
        }
    }

    private IEnumerator Start()
    {
        if (mainMenuGroup != null)
        {
            yield return FadeCanvas(mainMenuGroup, 0f, 1f, fadeInTime);

            mainMenuGroup.interactable = true;
            mainMenuGroup.blocksRaycasts = true;
        }

        SelectButton(firstSelectedButton);
    }

    public void StartGame()
    {
        if (loadingGame)
            return;

        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        loadingGame = true;

        PlaySound(startClip);

        if (mainMenuGroup != null)
            mainMenuGroup.interactable = false;

        yield return new WaitForSecondsRealtime(startGameDelay);

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        PlaySound(clickClip);

        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        SelectButton(optionsBackButton);
    }

    public void CloseOptions()
    {
        PlaySound(clickClip);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        SelectButton(firstSelectedButton);
    }

    public void OpenCredits()
    {
        PlaySound(clickClip);

        if (creditsPanel != null)
            creditsPanel.SetActive(true);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        SelectButton(creditsBackButton);
    }

    public void CloseCredits()
    {
        PlaySound(clickClip);

        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        SelectButton(firstSelectedButton);
    }

    public void QuitGame()
    {
        PlaySound(clickClip);

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void PlayHoverSound()
    {
        PlaySound(hoverClip);
    }

    public void PlayClickSound()
    {
        PlaySound(clickClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null)
            return;

        uiAudioSource.PlayOneShot(clip);
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float time)
    {
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / time;

            group.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        group.alpha = to;
    }
}