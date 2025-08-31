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
    [SerializeField] private GameObject playingGroup;      // 공통 HUD
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

    // ===== Score UI (모드별 분리) =====
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
    private bool currentIsTimer = false; // 이번 라운드 모드

    // PlayerPrefs 키
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
        // 초기 화면
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

        // 리스타트로 진입 시 모드 유지
        if (RestartHelper.ModeIsTimer.HasValue)
        {
            BeginGameplay(RestartHelper.ModeIsTimer.Value);
            RestartHelper.ModeIsTimer = null;
        }
        else
        {
            ShowTitle();
        }

        // 시작 시 한 번 전체 HS 텍스트 동기화
        RefreshHighTexts();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // === 디버그: 하이스코어 초기화 단축키 ===
        if (Input.GetKeyDown(KeyCode.F12))
            ResetHighScores();

        // 점수 갱신 (모드별)
        int score = GameManager.Instance.GetScore();
        if (state == Flow.PlayingClassic && classicScoreText) classicScoreText.text = score.ToString();
        if (state == Flow.PlayingTimer && timerScoreText) timerScoreText.text = score.ToString();

        // 모드별 HS 텍스트 상시 갱신(활성된 쪽만 보임)
        if (state == Flow.PlayingClassic || state == Flow.PlayingTimer)
            RefreshHighTexts();

        // 타임어택 카운트다운
        if (state == Flow.PlayingTimer && !GameManager.Instance.IsGameOver)
        {
            timerRemain -= Time.deltaTime;
            if (timerRemain < 0f) timerRemain = 0f;
            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();
            if (timerRemain <= 0f) GameManager.Instance.GameOver();
        }

        // 게임오버 패널 오픈 + 하이스코어 저장
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

    // ★ 모든 스포너에 입력 억제 신호 보내기
    void SuppressPointerOnAllSpawners()
    {
        if (spawners == null) return;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].SuppressNextPointerRelease();
    }

    void OpenPause()
    {
        if (state != Flow.PlayingClassic && state != Flow.PlayingTimer) return; // 양쪽 모드 지원
        state = Flow.Paused;

        // ★ 패널 열기 전에 먼저 억제 신호(같은 프레임 드롭 방지)
        SuppressPointerOnAllSpawners();

        ToggleGroups(title: false, choose: false, playing: true, classic: !currentIsTimer, timer: currentIsTimer,
                     pause: true, settingTitle: false, over: false);

        SetSpawnersEnabled(false);
        Time.timeScale = 0f;

        // 패널 열릴 때 볼륨 슬라이더 최신값 반영
        if (pauseBgmSlider) pauseBgmSlider.value = AudioManager.Instance ? AudioManager.Instance.GetBgmVolume() : pauseBgmSlider.value;
        if (pauseSfxSlider) pauseSfxSlider.value = AudioManager.Instance ? AudioManager.Instance.GetSfxVolume() : pauseSfxSlider.value;
    }

    void ClosePause()
    {
        bool wasTimer = currentIsTimer;
        state = wasTimer ? Flow.PlayingTimer : Flow.PlayingClassic;

        // ★ 재개 버튼 클릭도 잔여 입력 억제
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

        // ★ 게임오버 패널 오픈 시에도 잔여 입력 억제
        SuppressPointerOnAllSpawners();

        // 화면 토글 (HUD는 남겨둔 상태에서 Over만 On)
        ToggleGroups(title: false, choose: false, playing: true, classic: !wasTimer, timer: wasTimer, pause: false, settingTitle: false, over: true);
        SetSpawnersEnabled(false);

        // 하이스코어 저장 + 텍스트 갱신
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
        // ★ 타이틀 세팅 패널도 열자마자 억제(혹시 타이틀에서 드롭 입력 쓰는 구조 대비)
        SuppressPointerOnAllSpawners();

        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, pause: false, settingTitle: true, over: false);

        // 패널 열릴 때 슬라이더 최신값 반영
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
        // 현재 저장된 최고 기록을 각 UI에 반영
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

    /// 게임종료. 전처리기를 이용해 에디터 아닐때 종료.
    public void exitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
