// Scripts/GameUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUI : MonoBehaviour
{
    // ===== Screens / Groups =====
    [Header("Screens")]
    [SerializeField] private GameObject titleGroup;
    [SerializeField] private GameObject chooseModeGroup;
    [SerializeField] private GameObject playingGroup;      // ���� HUD
    [SerializeField] private GameObject classicGroup;      // ClassicMode
    [SerializeField] private GameObject timerGroup;        // TimerMode
    [SerializeField] private GameObject pauseGroup;        // Pause-Playing/Panel
    [SerializeField] private GameObject settingTitleGroup; // Setting-Title/Panel
    [SerializeField] private GameObject gameOverGroup;     // GameOver/Panel

    // ===== Title =====
    [Header("Title")]
    [SerializeField] private Button startBTN;
    [SerializeField] private Button settingBTN;
    [SerializeField] private Button exitBTN;

    // ===== Choose Mode =====
    [Header("Choose Mode")]
    [SerializeField] private Button classicBTN;
    [SerializeField] private Button timeBTN;
    [SerializeField] private Button chooseBackBTN;

    // ===== In-Game (HUD) =====
    [Header("Playing HUD")]
    [SerializeField] private Button pauseBTN;
    [SerializeField] private TextMeshProUGUI classicHighText; // ClassicMode/High_Classic
    [SerializeField] private TextMeshProUGUI timerHighText;   // TimerMode/High_Timer

    // ===== Score UI (��庰 �и�) =====
    [Header("Score UI (Separated)")]
    [SerializeField] private TextMeshProUGUI classicScoreText; // ClassicMode/Score_Classic
    [SerializeField] private TextMeshProUGUI timerScoreText;   // TimerMode/Score_Timer

    // ===== Timer Mode =====
    [Header("Time Attack")]
    [SerializeField] private TextMeshProUGUI timeText;     // TimerMode/TimerUI
    [SerializeField] private float timeAttackSeconds = 60f;

    // ===== Pause Panel =====
    [Header("Pause UI")]
    [SerializeField] private Slider pauseBgmSlider;
    [SerializeField] private Slider pauseSfxSlider;
    [SerializeField] private Button pauseRestartBTN;
    [SerializeField] private Button pauseHomeBTN;
    [SerializeField] private Button pauseBackBTN;

    // ===== Game Over Panel =====
    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button goRestartBTN;
    [SerializeField] private Button goHomeBTN;
    [SerializeField] private Button goExitBTN;

    // ===== Title Setting Panel =====
    [Header("Title Setting")]
    [SerializeField] private Slider titleBgmSlider;
    [SerializeField] private Slider titleSfxSlider;
    [SerializeField] private Button settingBackBTN;

    // ===== State =====
    private enum Flow { Title, ChooseMode, PlayingClassic, PlayingTimer, Paused, GameOver }
    private Flow state = Flow.Title;

    private float timerRemain;
    private Spawner[] spawners;
    private bool currentIsTimer = false; // �̹� ���� ���

    // PlayerPrefs Ű
    const string HS_CLASSIC = "HighScore_Classic";
    const string HS_TIMER = "HighScore_Timer";

    void Awake()
    {
        spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        SetSpawnersEnabled(false);
        Time.timeScale = 1f;
    }

    void Start()
    {
        // �ʱ� ȭ��
        ShowTitle();

        // Title
        if (startBTN) startBTN.onClick.AddListener(ShowChooseMode);
        if (settingBTN) settingBTN.onClick.AddListener(OpenTitleSetting);
        if (exitBTN) exitBTN.onClick.AddListener(exitGame);

        // Choose Mode
        if (classicBTN) classicBTN.onClick.AddListener(() => BeginGameplay(false));
        if (timeBTN) timeBTN.onClick.AddListener(() => BeginGameplay(true));
        if (chooseBackBTN) chooseBackBTN.onClick.AddListener(ShowTitle);

        // Playing HUD
        if (pauseBTN) pauseBTN.onClick.AddListener(OpenPause);

        // Pause Panel
        if (pauseRestartBTN) pauseRestartBTN.onClick.AddListener(RestartScene);
        if (pauseHomeBTN) pauseHomeBTN.onClick.AddListener(GoHomeScene);
        if (pauseBackBTN) pauseBackBTN.onClick.AddListener(ClosePause);

        if (pauseBgmSlider)
        {
            pauseBgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));
            pauseBgmSlider.value = AudioManager.Instance ? AudioManager.Instance.GetBgmVolume() : 0.5f;
        }
        if (pauseSfxSlider)
        {
            pauseSfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
            pauseSfxSlider.value = AudioManager.Instance ? AudioManager.Instance.GetSfxVolume() : 0.5f;
        }

        // Game Over Panel
        if (goRestartBTN) goRestartBTN.onClick.AddListener(RestartScene);
        if (goHomeBTN) goHomeBTN.onClick.AddListener(GoHomeScene);
        if (goExitBTN) goExitBTN.onClick.AddListener(exitGame);

        // Title Setting Panel
        if (settingBackBTN) settingBackBTN.onClick.AddListener(CloseTitleSetting);
        if (titleBgmSlider)
        {
            titleBgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));
            titleBgmSlider.value = AudioManager.Instance ? AudioManager.Instance.GetBgmVolume() : 0.5f;
        }
        if (titleSfxSlider)
        {
            titleSfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
            titleSfxSlider.value = AudioManager.Instance ? AudioManager.Instance.GetSfxVolume() : 0.5f;
        }

        // ����ŸƮ�� ���� �� ��� ����
        if (RestartHelper.ModeIsTimer.HasValue)
        {
            BeginGameplay(RestartHelper.ModeIsTimer.Value);
            RestartHelper.ModeIsTimer = null;
        }
        else
        {
            ShowTitle();
        }

        // ���� �� �� �� ��ü HS �ؽ�Ʈ ����ȭ
        RefreshHighTexts();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // === �����: ���̽��ھ� �ʱ�ȭ ����Ű ===
        if (Input.GetKeyDown(KeyCode.F12))
            ResetHighScores();

        // ���� ���� (��庰)
        int score = GameManager.Instance.GetScore();
        if (state == Flow.PlayingClassic && classicScoreText) classicScoreText.text = score.ToString();
        if (state == Flow.PlayingTimer && timerScoreText) timerScoreText.text = score.ToString();

        // ��庰 HS �ؽ�Ʈ ��� ����(Ȱ���� �ʸ� ����)
        if (state == Flow.PlayingClassic || state == Flow.PlayingTimer)
            RefreshHighTexts();

        // Ÿ�Ӿ��� ī��Ʈ�ٿ�
        if (state == Flow.PlayingTimer && !GameManager.Instance.IsGameOver)
        {
            timerRemain -= Time.deltaTime;
            if (timerRemain < 0f) timerRemain = 0f;
            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();
            if (timerRemain <= 0f) GameManager.Instance.GameOver();
        }

        // ���ӿ��� �г� ���� + ���̽��ھ� ����
        if (GameManager.Instance.IsGameOver && state != Flow.GameOver)
            OpenGameOverUI(score);
    }

    // ======== Flow helpers ========
    void ShowTitle()
    {
        state = Flow.Title;
        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, pause: false, settingTitle: false, over: false);
        SetSpawnersEnabled(false);
        Time.timeScale = 1f;
        AudioManager.Instance?.EnsureBgmPlaying();
        RefreshHighTexts();
    }

    void ShowChooseMode()
    {
        state = Flow.ChooseMode;
        ToggleGroups(title: true, choose: true, playing: false, classic: false, timer: false, pause: false, settingTitle: false, over: false);
        SetSpawnersEnabled(false);
    }

    void BeginGameplay(bool timerMode)
    {
        currentIsTimer = timerMode;

        if (timerMode)
        {
            state = Flow.PlayingTimer;
            timerRemain = timeAttackSeconds;
            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();
        }
        else
        {
            state = Flow.PlayingClassic;
        }

        ToggleGroups(title: false, choose: false, playing: true,
                     classic: !timerMode, timer: timerMode, pause: false, settingTitle: false, over: false);

        SetSpawnersEnabled(true);
        AudioManager.Instance?.EnsureBgmPlaying();
        RefreshHighTexts();
    }

    // �� ��� �����ʿ� �Է� ���� ��ȣ ������
    void SuppressPointerOnAllSpawners()
    {
        if (spawners == null) return;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].SuppressNextPointerRelease();
    }

    void OpenPause()
    {
        if (state != Flow.PlayingClassic && state != Flow.PlayingTimer) return; // ���� ��� ����
        state = Flow.Paused;

        // �� �г� ���� ���� ���� ���� ��ȣ(���� ������ ��� ����)
        SuppressPointerOnAllSpawners();

        ToggleGroups(title: false, choose: false, playing: true, classic: !currentIsTimer, timer: currentIsTimer,
                     pause: true, settingTitle: false, over: false);

        SetSpawnersEnabled(false);
        Time.timeScale = 0f;

        // �г� ���� �� ���� �����̴� �ֽŰ� �ݿ�
        if (pauseBgmSlider) pauseBgmSlider.value = AudioManager.Instance ? AudioManager.Instance.GetBgmVolume() : pauseBgmSlider.value;
        if (pauseSfxSlider) pauseSfxSlider.value = AudioManager.Instance ? AudioManager.Instance.GetSfxVolume() : pauseSfxSlider.value;
    }

    void ClosePause()
    {
        bool wasTimer = currentIsTimer;
        state = wasTimer ? Flow.PlayingTimer : Flow.PlayingClassic;

        // �� �簳 ��ư Ŭ���� �ܿ� �Է� ����
        SuppressPointerOnAllSpawners();

        ToggleGroups(title: false, choose: false, playing: true,
                     classic: !wasTimer, timer: wasTimer, pause: false, settingTitle: false, over: false);

        SetSpawnersEnabled(true);
        Time.timeScale = 1f;
        RefreshHighTexts();
    }

    void OpenGameOverUI(int finalScore)
    {
        bool wasTimer = currentIsTimer;
        state = Flow.GameOver;

        // �� ���ӿ��� �г� ���� �ÿ��� �ܿ� �Է� ����
        SuppressPointerOnAllSpawners();

        // ȭ�� ��� (HUD�� ���ܵ� ���¿��� Over�� On)
        ToggleGroups(title: false, choose: false, playing: true, classic: !wasTimer, timer: wasTimer, pause: false, settingTitle: false, over: true);
        SetSpawnersEnabled(false);

        // ���̽��ھ� ���� + �ؽ�Ʈ ����
        SaveHighScore(finalScore, wasTimer);
        if (finalScoreText) finalScoreText.text = finalScore.ToString();
        RefreshHighTexts();
    }

    void RestartScene()
    {
        bool wasTimer = currentIsTimer;
        GameManager.Instance?.ResetTimescaleIfPaused();
        RestartHelper.ModeIsTimer = wasTimer;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoHomeScene()
    {
        GameManager.Instance?.ResetTimescaleIfPaused();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OpenTitleSetting()
    {
        // �� Ÿ��Ʋ ���� �гε� ���ڸ��� ����(Ȥ�� Ÿ��Ʋ���� ��� �Է� ���� ���� ���)
        SuppressPointerOnAllSpawners();

        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, pause: false, settingTitle: true, over: false);

        // �г� ���� �� �����̴� �ֽŰ� �ݿ�
        if (titleBgmSlider) titleBgmSlider.value = AudioManager.Instance ? AudioManager.Instance.GetBgmVolume() : titleBgmSlider.value;
        if (titleSfxSlider) titleSfxSlider.value = AudioManager.Instance ? AudioManager.Instance.GetSfxVolume() : titleSfxSlider.value;
    }

    void CloseTitleSetting()
    {
        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, pause: false, settingTitle: false, over: false);
    }

    // ======== Utilities ========
    void ToggleGroups(bool title, bool choose, bool playing, bool classic, bool timer, bool pause, bool settingTitle, bool over)
    {
        if (titleGroup) titleGroup.SetActive(title);
        if (chooseModeGroup) chooseModeGroup.SetActive(choose);
        if (playingGroup) playingGroup.SetActive(playing);
        if (classicGroup) classicGroup.SetActive(classic);
        if (timerGroup) timerGroup.SetActive(timer);
        if (pauseGroup) pauseGroup.SetActive(pause);
        if (settingTitleGroup) settingTitleGroup.SetActive(settingTitle);
        if (gameOverGroup) gameOverGroup.SetActive(over);
    }

    void SetSpawnersEnabled(bool on)
    {
        if (spawners == null) return;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].enabled = on;
    }

    // ======== High Score ========
    int GetHighScore(bool isTimer)
    {
        string key = isTimer ? HS_TIMER : HS_CLASSIC;
        return PlayerPrefs.GetInt(key, 0);
    }

    void SaveHighScore(int newScore, bool isTimer)
    {
        string key = isTimer ? HS_TIMER : HS_CLASSIC;
        int prev = PlayerPrefs.GetInt(key, 0);
        if (newScore > prev)
        {
            PlayerPrefs.SetInt(key, newScore);
            PlayerPrefs.Save();
        }
    }

    void RefreshHighTexts()
    {
        // ���� ����� �ְ� ����� �� UI�� �ݿ�
        if (classicHighText)
            classicHighText.text = GetHighScore(false).ToString();
        if (timerHighText)
            timerHighText.text = GetHighScore(true).ToString();
    }

    // ======== Debug: High Score Reset ========
    void ResetHighScores()
    {
        PlayerPrefs.DeleteKey(HS_CLASSIC);
        PlayerPrefs.DeleteKey(HS_TIMER);
        PlayerPrefs.Save();
        RefreshHighTexts();
        Debug.Log(">>> High Scores cleared (Classic + Timer)");
    }

    /// ��������. ��ó���⸦ �̿��� ������ �ƴҶ� ����.
    public void exitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
