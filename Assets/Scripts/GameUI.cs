using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUI : MonoBehaviour
{
    // ===== Screens / Groups (Hierarchy�� �ִ� �гε� ����) =====
    [Header("Screens")]
    [SerializeField] private GameObject titleGroup;        // Canvas/Title
    [SerializeField] private GameObject chooseModeGroup;   // Canvas/ChooseMode
    [SerializeField] private GameObject playingGroup;      // Canvas/Playing (���� HUD)
    [SerializeField] private GameObject classicGroup;      // Canvas/ClassicMode (Ŭ���� ���� UI)
    [SerializeField] private GameObject timerGroup;        // Canvas/TimerMode  (Ÿ�̸�/���� UI)
    [SerializeField] private GameObject pauseGroup;        // Canvas/Pause-Playing/Panel
    [SerializeField] private GameObject settingTitleGroup; // Canvas/Setting-Title/Panel
    [SerializeField] private GameObject gameOverGroup;     // Canvas/GameOver/Panel

    // ===== Title =====
    [Header("Title")]
    [SerializeField] private Button startBTN;     // Title/StartBTN
    [SerializeField] private Button settingBTN;   // Title/SettingBTN
    [SerializeField] private Button exitBTN;      // Title/ExitBTN

    // ===== Choose Mode =====
    [Header("Choose Mode")]
    [SerializeField] private Button classicBTN;   // ChooseMode/ClassicBTN
    [SerializeField] private Button timeBTN;      // ChooseMode/TimerBTN
    [SerializeField] private Button chooseBackBTN;// ChooseMode/BackBTN

    // ===== In-Game (HUD) =====
    [Header("Playing HUD")]
    [SerializeField] private Button pauseBTN;             // Playing/PauseBTN
    [SerializeField] private TextMeshProUGUI highScoreUI; // Playing/HighUI (���̽��ھ� ǥ��)

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
    [SerializeField] private Slider pauseBgmSlider;        // Pause-Playing/Slider_BGM
    [SerializeField] private Slider pauseSfxSlider;        // Pause-Playing/Slider_SE
    [SerializeField] private Button pauseRestartBTN;       // Pause-Playing/RestartBTN
    [SerializeField] private Button pauseHomeBTN;          // Pause-Playing/HomeBTN
    [SerializeField] private Button pauseBackBTN;          // Pause-Playing/BackBTN (�ݱ�)

    // ===== Game Over Panel =====
    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText; // GameOver/Score_Final
    [SerializeField] private Button goRestartBTN;            // GameOver/RestartBTN
    [SerializeField] private Button goHomeBTN;               // GameOver/HomeBTN
    [SerializeField] private Button goExitBTN;               // GameOver/ExitBTN

    // ===== Title Setting Panel =====
    [Header("Title Setting")]
    [SerializeField] private Slider titleBgmSlider;        // Setting-Title/Slider_BGM
    [SerializeField] private Slider titleSfxSlider;        // Setting-Title/Slider_SE
    [SerializeField] private Button settingBackBTN;        // Setting-Title/BackBTN

    // ===== State =====
    private enum Flow { Title, ChooseMode, PlayingClassic, PlayingTimer, Paused, GameOver }
    private Flow state = Flow.Title;

    private float timerRemain;
    private Spawner[] spawners;

    // PlayerPrefs Ű (��庰 ���̽��ھ� ����)
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
        if (exitBTN) exitBTN.onClick.AddListener(Application.Quit); //���� ����

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
            pauseBgmSlider.value = 0.5f;
        }
        if (pauseSfxSlider)
        {
            pauseSfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
            pauseSfxSlider.value = 0.5f;
        }

        // Game Over Panel
        if (goRestartBTN) goRestartBTN.onClick.AddListener(RestartScene);
        if (goHomeBTN) goHomeBTN.onClick.AddListener(GoHomeScene);
        if (goExitBTN) goExitBTN.onClick.AddListener(Application.Quit); //���� ����

        // Title Setting Panel
        if (settingBackBTN) settingBackBTN.onClick.AddListener(CloseTitleSetting);
        if (titleBgmSlider)
        {
            titleBgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));
            titleBgmSlider.value = 0.5f;
        }
        if (titleSfxSlider)
        {
            titleSfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
            titleSfxSlider.value = 0.5f;
        }

        if (RestartHelper.ModeIsTimer.HasValue)
        {
            BeginGameplay(RestartHelper.ModeIsTimer.Value);
            RestartHelper.ModeIsTimer = null; // �ʱ�ȭ
        }
        else
        {
            ShowTitle();
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // ���� ���� (��庰 UI �и�)
        int score = GameManager.Instance.GetScore();
        if (state == Flow.PlayingClassic && classicScoreText) classicScoreText.text = score.ToString();
        if (state == Flow.PlayingTimer && timerScoreText) timerScoreText.text = score.ToString();

        // ���̽��ھ� ǥ�� (�÷��� �� ��� ����)
        if (highScoreUI)
        {
            int hs = GetCurrentHighScore();
            highScoreUI.text = hs.ToString();
        }

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
    }

    void ShowChooseMode()
    {
        state = Flow.ChooseMode;
        ToggleGroups(title: true, choose: true, playing: false, classic: false, timer: false, pause: false, settingTitle: false, over: false);
        SetSpawnersEnabled(false);
    }

    void BeginGameplay(bool timerMode)
    {
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
    }

    void OpenPause()
    {
        if (state != Flow.PlayingClassic) return;
        state = Flow.Paused;

        ToggleGroups(title: false, choose: false, playing: true, classic: true, timer: false,
                     pause: true, settingTitle: false, over: false);

        // ����/��� �Է� ����
        SetSpawnersEnabled(false);
        Time.timeScale = 0f;
    }

    void ClosePause()
    {
        // ���� ���� ����
        bool wasTimer = (timerGroup != null && timerGroup.activeSelf);
        state = wasTimer ? Flow.PlayingTimer : Flow.PlayingClassic;

        ToggleGroups(title: false, choose: false, playing: true,
                     classic: !wasTimer, timer: wasTimer, pause: false, settingTitle: false, over: false);

        SetSpawnersEnabled(true);
        Time.timeScale = 1f;
    }


    void OpenGameOverUI(int finalScore)
    {
        bool wasTimer = (timerGroup != null && timerGroup.activeSelf);
        state = wasTimer ? Flow.PlayingTimer : Flow.PlayingClassic;

        state = Flow.GameOver;
        ToggleGroups(title: false, choose: false, playing: true, classic: !wasTimer, timer: wasTimer, pause: false, settingTitle: false, over: true);
        SetSpawnersEnabled(false);

        // ���̽��ھ� ����
        SaveHighScore(finalScore);

        if (finalScoreText) finalScoreText.text = finalScore.ToString();
    }

    void RestartScene()
    {
        // ���� ��� ���
        bool wasTimer = (timerGroup != null && timerGroup.activeSelf);

        // �� �ٽ� �ε�
        RestartHelper.ModeIsTimer = wasTimer;   //�ε� �Ŀ��� ��� ����
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }
    void GoHomeScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OpenTitleSetting()
    {
        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, pause: false, settingTitle: true, over: false);
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
    int GetCurrentHighScore()
    {
        bool isTimer = (state == Flow.PlayingTimer || (timerGroup && timerGroup.activeSelf && !gameOverGroup.activeSelf));
        string key = isTimer ? HS_TIMER : HS_CLASSIC;
        return PlayerPrefs.GetInt(key, 0);
    }

    void SaveHighScore(int newScore)
    {
        // ���ӿ��� ��, ���� � ��忴���� ȭ�鿡�� �Ǵ�
        bool wasTimer = timerGroup && !timerGroup.activeSelf == false && gameOverGroup && gameOverGroup.activeSelf;
        // �� ������ ��Ȳ�� ���� ��ȣ�ϸ�, state ������ε� ó��:
        if (state == Flow.GameOver)
            wasTimer = timerGroup && timerGroup.activeSelf; // GameOver ������ Ȱ�� ȭ�� ����

        string key = wasTimer ? HS_TIMER : HS_CLASSIC;
        int prev = PlayerPrefs.GetInt(key, 0);
        if (newScore > prev)
        {
            PlayerPrefs.SetInt(key, newScore);
            PlayerPrefs.Save();
        }
    }
}
