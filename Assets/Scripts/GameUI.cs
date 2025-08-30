using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUI : MonoBehaviour
{
    // ===== Screens / Groups =====
    [Header("Screens")]
    [SerializeField] private GameObject titleGroup;       // Canvas/Title
    [SerializeField] private GameObject chooseModeGroup;  // Canvas/ChooseMode
    [SerializeField] private GameObject playingGroup;     // Canvas/Playing (공통 HUD)
    [SerializeField] private GameObject classicGroup;     // Canvas/ClassicMode (클래식용 점수 UI 묶음)
    [SerializeField] private GameObject timerGroup;       // Canvas/TimerMode  (타이머용 점수/타이머 UI 묶음)
    [SerializeField] private GameObject gameOverGroup;    // Canvas/GameOver (또는 Pause-Playing의 패널)

    // ===== Title =====
    [Header("Title")]
    [SerializeField] private Button startBTN;
    [SerializeField] private Button settingBTN; // 선택
    [SerializeField] private Button exitBTN;    // 선택

    // ===== Choose Mode =====
    [Header("Choose Mode")]
    [SerializeField] private Button classicBTN;
    [SerializeField] private Button timeBTN;
    [SerializeField] private Button chooseBackBTN;

    // ===== Score UI (분리된 2세트) =====
    [Header("Score UI (Separated)")]
    [SerializeField] private TextMeshProUGUI classicScoreText; // ClassicMode/Score_Classic
    [SerializeField] private TextMeshProUGUI timerScoreText;   // TimerMode/Score_Timer

    // ===== Timer Mode =====
    [Header("Time Attack")]
    [SerializeField] private TextMeshProUGUI timeText; // TimerMode/TimerUI
    [SerializeField] private float timeAttackSeconds = 60f;

    // ===== Game Over =====
    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText; // GameOver/Score_Final
    [SerializeField] private Button restartBTN;              // RestartBTN
    [SerializeField] private Button exitBTN_InGame;          // ExitBTN (게임오버 패널 안)

    // ===== Audio (옵션) =====
    [Header("Audio (Optional)")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // ===== State =====
    private enum Flow { Title, ChooseMode, PlayingClassic, PlayingTimer, GameOver }
    private Flow state = Flow.Title;
    private float timerRemain;
    private Spawner[] spawners;

    void Awake()
    {
        spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        SetSpawnersEnabled(false);  // 타이틀에서 비활성
        Time.timeScale = 1f;
    }

    void Start()
    {
        ShowTitle();

        // Title
        if (startBTN) startBTN.onClick.AddListener(ShowChooseMode);
        if (exitBTN) exitBTN.onClick.AddListener(Application.Quit);

        // Choose
        if (classicBTN) classicBTN.onClick.AddListener(() => BeginGameplay(false));
        if (timeBTN) timeBTN.onClick.AddListener(() => BeginGameplay(true));
        if (chooseBackBTN) chooseBackBTN.onClick.AddListener(ShowTitle);

        // GameOver
        if (restartBTN) restartBTN.onClick.AddListener(RestartScene);
        if (exitBTN_InGame) exitBTN_InGame.onClick.AddListener(Application.Quit);

        // Audio sliders (옵션)
        if (bgmSlider)
        {
            bgmSlider.value = 0.5f;
            bgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));
        }
        if (sfxSlider)
        {
            sfxSlider.value = 0.8f;
            sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));
        }
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        // 분리된 점수 UI 갱신
        int score = GameManager.Instance.GetScore();
        if (state == Flow.PlayingClassic && classicScoreText) classicScoreText.text = score.ToString();
        if (state == Flow.PlayingTimer && timerScoreText) timerScoreText.text = score.ToString();

        // 타임어택 카운트다운
        if (state == Flow.PlayingTimer && !GameManager.Instance.IsGameOver)
        {
            timerRemain -= Time.deltaTime;
            if (timerRemain < 0f) timerRemain = 0f;
            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();

            if (timerRemain <= 0f)
                GameManager.Instance.GameOver(); // 타임업
        }

        // 게임오버 패널 오픈
        if (GameManager.Instance.IsGameOver && state != Flow.GameOver)
            OpenGameOverUI();
    }

    // ===== Flow =====
    void ShowTitle()
    {
        state = Flow.Title;
        ToggleGroups(title: true, choose: false, playing: false, classic: false, timer: false, over: false);
        SetSpawnersEnabled(false);
        Time.timeScale = 1f;
    }

    void ShowChooseMode()
    {
        state = Flow.ChooseMode;
        ToggleGroups(title: false, choose: true, playing: false, classic: false, timer: false, over: false);
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
                     classic: !timerMode, timer: timerMode, over: false);

        SetSpawnersEnabled(true);
        AudioManager.Instance?.EnsureBgmPlaying();
    }

    void OpenGameOverUI()
    {
        state = Flow.GameOver;
        ToggleGroups(title: false, choose: false, playing: true, classic: false, timer: false, over: true);
        SetSpawnersEnabled(false);

        if (finalScoreText && GameManager.Instance != null)
            finalScoreText.text = GameManager.Instance.GetScore().ToString();
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ===== Utils =====
    void ToggleGroups(bool title, bool choose, bool playing, bool classic, bool timer, bool over)
    {
        if (titleGroup) titleGroup.SetActive(title);
        if (chooseModeGroup) chooseModeGroup.SetActive(choose);
        if (playingGroup) playingGroup.SetActive(playing);
        if (classicGroup) classicGroup.SetActive(classic);
        if (timerGroup) timerGroup.SetActive(timer);
        if (gameOverGroup) gameOverGroup.SetActive(over);
    }

    void SetSpawnersEnabled(bool on)
    {
        if (spawners == null) return;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].enabled = on;
    }
}
