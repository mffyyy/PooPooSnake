using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    [Header("Score UI")]
    public TMP_Text currentScoreText;
    public TMP_Text highestScoreText;
    public GameObject gameOverBar;
    public GameObject noteBar;
    private int score = 0;
    private int highestScore = 0;
    private const string HighestScoreKey = "HighestScore";
    private const string FirstNoteSeenKey = "FirstNoteSeen";

    [Header("Pause UI")]
    public Button pauseButton;
    public GameObject firstNote;
    public TMP_Text firstNoteText;
    public GameObject noteToilet;
    public GameObject note2;
    public float firstNoteShowDelay = 1f;
    public float firstNoteTypewriterDelay = 1f;
    public float firstNoteCharsPerSecond = 15f;
    public string noteToiletTriggerPhrase = "地方";

    private Coroutine firstNoteRoutine;
    private Coroutine firstFoodTutorialRoutine;
    public Sprite[] pauseIcons; // 0: Paused, 1: Play，2：restart

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip gameOverClip;
    public AudioClip addScoreClip;
    public AudioClip pooPooClip;
    public AudioClip eatingClip;
    [Range(0f, 1f)]
    public float soundEffectVolume = 0.4f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        DisableButtonNavigation();
        if (gameOverBar != null)  
            gameOverBar.SetActive(false);

        ResolveNoteBar();
        if (noteBar != null)
            noteBar.SetActive(false);

        ResolveFirstNoteObjects();
        HideFirstNoteObjects();

        ResolveScoreTexts();
        highestScore = PlayerPrefs.GetInt(HighestScoreKey, 0);
        UpdateScoreText();
        UpdateHighestScoreText();
        UpdatePauseIcon();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePause();
    }

    public float GetMoveTimer()
    {
        int speedLevel = Mathf.Max(0, score) / Config.scorePerSpeedLevel;
        float moveTimer = Config.moveTimer - speedLevel * Config.moveTimerDecreasePerLevel;
        return Mathf.Max(Config.minMoveTimer, moveTimer);
    }

#region Score Management
    public void AddScore(int value)
    {
        if (CurrentState != GameState.Playing)
            return;

        score += value;
        if (score > highestScore)
        {
            highestScore = score;
            PlayerPrefs.SetInt(HighestScoreKey, highestScore);
            PlayerPrefs.Save();
            UpdateHighestScoreText();
        }

        UpdateScoreText();
        // Debug.Log($"Score: {score}");
    }
    private void UpdateScoreText()
    {
        if (currentScoreText != null)
            currentScoreText.text = score.ToString();
    }
    private void UpdateHighestScoreText()
    {
        if (highestScoreText != null)
            highestScoreText.text = highestScore.ToString();
    }
    private void ResolveScoreTexts()
    {
        if (currentScoreText == null)
        {
            GameObject currentScoreObject = GameObject.Find("T_CurrentScore_number");
            if (currentScoreObject != null)
                currentScoreText = currentScoreObject.GetComponent<TMP_Text>();
        }

        if (highestScoreText == null)
        {
            GameObject highestScoreObject = GameObject.Find("T_HighestScore_number");
            if (highestScoreObject != null)
                highestScoreText = highestScoreObject.GetComponent<TMP_Text>();
        }
    }
#endregion

    public bool IsPlaying()
    {
        return CurrentState == GameState.Playing;
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver)
            return;

        PlayGameOverSound();
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        UpdatePauseIcon();
        if (gameOverBar != null)
            gameOverBar.SetActive(true);
            PlayUiAnimator(gameOverBar);

        ClearSelectedButton();
        // Debug.Log("Game Over!");
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ShowNote()
    {
        if (CurrentState == GameState.GameOver)
        {
            if (gameOverBar != null)
                gameOverBar.SetActive(false);

            ShowNoteBar();
            UpdatePauseIcon();
            ClearSelectedButton();
            return;
        }

        ShowNoteBar();
        SetPaused(true);
    }

    private void ShowNoteBar()
    {
        ResolveNoteBar();
        if (noteBar == null)
            return;

        noteBar.SetActive(true);
        PlayUiAnimator(noteBar);
    }

    private void ResolveNoteBar()
    {
        if (noteBar != null)
            return;

        noteBar = FindSceneObject("NoteBar ");
        if (noteBar == null)
            noteBar = FindSceneObject("NoteBar");
    }

#region Pause Management
    public void TogglePause()
    {
        if (CurrentState == GameState.GameOver)
            RestartGame();
        else if (CurrentState == GameState.Playing)
            SetPaused(true);
        else if (CurrentState == GameState.Paused)
            SetPaused(false);
    }
    private void SetPaused(bool paused)
    {
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = paused ? GameState.Paused : GameState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        if (!paused && noteBar != null)
        {
            noteBar.SetActive(false);
            HideFirstNoteObjects();
            StopFirstNoteTypewriter();
        }

        UpdatePauseIcon();
        ClearSelectedButton();
        // Debug.Log(paused ? "Paused" : "Resume");
    }
    private void DisableButtonNavigation()
    {
        if (pauseButton == null)
            return;

        Navigation navigation = pauseButton.navigation;
        navigation.mode = Navigation.Mode.None;
        pauseButton.navigation = navigation;
    }
    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
    private void UpdatePauseIcon()
    {
        if (pauseButton == null || pauseButton.image == null)
            return;

        Sprite icon = null;
        if (CurrentState == GameState.Paused && pauseIcons != null && pauseIcons.Length > 1)
            icon = pauseIcons[1];
        else if (CurrentState == GameState.GameOver && pauseIcons != null && pauseIcons.Length > 2)
            icon = pauseIcons[2];
        else if (pauseIcons != null && pauseIcons.Length > 0)
            icon = pauseIcons[0];

        if (icon != null)
            pauseButton.image.sprite = icon;
    }
#endregion   
    
#region First Note Management
    public void ShowFirstFoodTutorial()
    {
        if (CurrentState == GameState.GameOver)
            return;

        // if (PlayerPrefs.GetInt(FirstNoteSeenKey, 0) == 1)
        //     return;

        PlayerPrefs.SetInt(FirstNoteSeenKey, 1);
        PlayerPrefs.Save();

        if (firstFoodTutorialRoutine != null)
            StopCoroutine(firstFoodTutorialRoutine);

        firstFoodTutorialRoutine = StartCoroutine(ShowFirstFoodTutorialDelayed());
    }

    private IEnumerator ShowFirstFoodTutorialDelayed()
    {
        if (firstNoteShowDelay > 0f)
            yield return new WaitForSecondsRealtime(firstNoteShowDelay);

        firstFoodTutorialRoutine = null;

        if (CurrentState == GameState.GameOver)
            yield break;

        ResolveFirstNoteObjects();
        HideFirstNoteObjects();

        ShowFirstNoteStep(firstNote);

        StopFirstNoteTypewriter();
        if (firstNoteText != null)
            firstNoteRoutine = StartCoroutine(PlayFirstNoteTypewriter());

        SetPaused(true);
    }
    private void ResolveFirstNoteObjects()
    {
        if (firstNote == null)
            firstNote = FindSceneObject("FirstNote");

        if (firstNoteText == null)
        {
            GameObject firstNoteTextObject = FindSceneObject("FirstNoteText");
            if (firstNoteTextObject != null)
                firstNoteText = firstNoteTextObject.GetComponent<TMP_Text>();
        }

        if (noteToilet == null)
            noteToilet = FindSceneObject("NoteToilet");

        if (note2 == null)
            note2 = FindSceneObject("Note 2");
    }

    private void HideFirstNoteObjects()
    {
        if (firstNote != null)
            firstNote.SetActive(false);

        if (noteToilet != null)
            noteToilet.SetActive(false);

        if (note2 != null)
            note2.SetActive(false);
    }

    private IEnumerator PlayFirstNoteTypewriter()
    {
        string fullText = firstNoteText.text;
        firstNoteText.maxVisibleCharacters = 0;
        firstNoteText.ForceMeshUpdate();

        int totalCharacters = firstNoteText.textInfo.characterCount;
        int toiletTriggerCharacters = GetVisibleCharacterCountBeforeTrigger(fullText);
        bool toiletShown = false;
        float visibleCharacters = 0f;
        int lastVisibleCharacters = 0;

        if (firstNoteTypewriterDelay > 0f)
            yield return new WaitForSecondsRealtime(firstNoteTypewriterDelay);

        while (visibleCharacters < totalCharacters)
        {
            visibleCharacters += Mathf.Max(1f, firstNoteCharsPerSecond) * Time.unscaledDeltaTime;
            int currentVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visibleCharacters), 0, totalCharacters);
            firstNoteText.maxVisibleCharacters = currentVisibleCharacters;
            if (currentVisibleCharacters > lastVisibleCharacters)
            {
                lastVisibleCharacters = currentVisibleCharacters;
            }

            if (!toiletShown && toiletTriggerCharacters >= 0 && currentVisibleCharacters >= toiletTriggerCharacters)
            {
                toiletShown = true;
                ShowFirstNoteStep(noteToilet);
            }

            yield return null;
        }

        firstNoteText.maxVisibleCharacters = totalCharacters;

        if (!toiletShown)
            ShowFirstNoteStep(noteToilet);

        ShowFirstNoteStep(note2);
        firstNoteRoutine = null;
    }

    private int GetVisibleCharacterCountBeforeTrigger(string fullText)
    {
        if (string.IsNullOrEmpty(noteToiletTriggerPhrase))
            return -1;

        int phraseIndex = fullText.IndexOf(noteToiletTriggerPhrase);
        if (phraseIndex < 0)
            return -1;

        return phraseIndex + noteToiletTriggerPhrase.Length;
    }

    private void ShowFirstNoteStep(GameObject target)
    {
        if (target == null)
            return;

        target.SetActive(true);
        PlayUiAnimator(target);
    }

    private void StopFirstNoteTypewriter()
    {
        if (firstNoteRoutine != null)
        {
            StopCoroutine(firstNoteRoutine);
            firstNoteRoutine = null;
        }

        if (firstNoteText != null)
            firstNoteText.maxVisibleCharacters = int.MaxValue;
    }
#endregion
    private GameObject FindSceneObject(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name == objectName && objects[i].scene.IsValid())
                return objects[i];
        }

        return null;
    }

    private void PlayUiAnimator(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play(0, 0, 0f);
        }
    }
#region Sound Management
    public void PlayAddScoreSound()
    {
        PlaySound(addScoreClip);
    }

    public void PlayPooPooSound()
    {
        PlaySound(pooPooClip);
    }
     public void PlayEatingSound()
    {
        PlaySound(eatingClip);
    }

    private void PlayGameOverSound()
    {
        PlaySound(gameOverClip);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip, soundEffectVolume);
    }

#endregion
}
