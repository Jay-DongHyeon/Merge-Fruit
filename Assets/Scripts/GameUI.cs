using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUI : MonoBehaviour
{
    // ====== Panels / Groups ======
    [Header("Screens")]
    [SerializeField] private GameObject titleGroup;      // Canvas/Title
    [SerializeField] private GameObject chooseModeGroup; // Canvas/ChooseMode
    [SerializeField] private GameObject playingGroup;    // Canvas/Playing (����/�Ͻ����� ��ư ��)
    [SerializeField] private GameObject timerUIGroup;    // Canvas/TimerMode (���� �ð� ǥ��)
    [SerializeField] private GameObject gameOverGroup;   // Canvas/GameOver or Canvas/Pause-Playing

    // ====== Title ======
    [Header("Title")]
    [SerializeField] private Button startBTN;   // StartBTN
    [SerializeField] private Button settingBTN; // ����
    [SerializeField] private Button exitBTN;    // ����

    // ====== Choose Mode ======
    [Header("Choose Mode")]
    [SerializeField] private Button classicBTN; // ClassicBTN
    [SerializeField] private Button timeBTN;    // TimerBTN
    [SerializeField] private Button chooseBackBTN; // BackBTN (�������� ����)

    // ====== In-Game (Common) ======
    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI scoreText; // Playing/ScoreUI (�Ǵ� ClassicMode/Score_Classic)
    [SerializeField] private Button pauseBTN;           // ����
    [SerializeField] private Button homeBTN;            // ���� (Ȩ����)

    // ====== Timer Mode ======
    [Header("Time Attack")]
    [SerializeField] private TextMeshProUGUI timeText;  // TimerMode/TimerUI
    [SerializeField] private float timeAttackSeconds = 60f;

    // ====== Game Over ======
    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI finalScoreText; // GameOver/Score_Final
    [SerializeField] private Button restartBTN;               // RestartBTN
    [SerializeField] private Button exitBTN_InGame;           // ExitBTN(���ӿ��� �г� ��)

    // ====== Audio (����) ======
    [Header("Audio (Optional)")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // ====== State ======
    private enum Flow { Title, ChooseMode, Playing_Classic, Playing_Timer, GameOver }
    private Flow state = Flow.Title;
    private float timerRemain;
    private Spawner[] spawners;

    void Awake()
    {
        spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        SetSpawnersEnabled(false); // Ÿ��Ʋ������ �Է�/��� ��Ȱ��
        Time.timeScale = 1f;       // Ȥ�� 0���� �������� ���
    }

    void Start()
    {
        // �ʱ� ȭ��
        ShowTitle();

        // Title buttons
        if (startBTN) startBTN.onClick.AddListener(() => ShowChooseMode());
        if (exitBTN) exitBTN.onClick.AddListener(Application.Quit);

        // ChooseMode buttons
        if (classicBTN) classicBTN.onClick.AddListener(() => BeginGameplay(false));
        if (timeBTN) timeBTN.onClick.AddListener(() => BeginGameplay(true));
        if (chooseBackBTN) chooseBackBTN.onClick.AddListener(ShowTitle);

        // In-game common
        if (homeBTN) homeBTN.onClick.AddListener(ShowTitle);

        // GameOver
        if (restartBTN) restartBTN.onClick.AddListener(RestartScene);
        if (exitBTN_InGame) exitBTN_InGame.onClick.AddListener(Application.Quit);

        // Audio sliders
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
        // ���� ǥ��
        if (scoreText && GameManager.Instance != null)
            scoreText.text = GameManager.Instance.GetScore().ToString();

        // Ÿ�Ӿ��� ī��Ʈ�ٿ�
        if (state == Flow.Playing_Timer && GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            timerRemain -= Time.deltaTime;
            if (timerRemain < 0f) timerRemain = 0f;

            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();

            if (timerRemain <= 0f)
            {
                GameManager.Instance.GameOver(); // Ÿ�Ӿ� �� ���ӿ���
            }
        }

        // ���ӿ��� �г� ����
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver && state != Flow.GameOver)
        {
            OpenGameOverUI();
        }
    }

    // ====== Flow helpers ======
    void ShowTitle()
    {
        state = Flow.Title;
        ToggleGroups(title: true, choose: false, playing: false, timerUI: false, over: false);
        SetSpawnersEnabled(false);
        Time.timeScale = 1f;
    }

    void ShowChooseMode()
    {
        state = Flow.ChooseMode;
        ToggleGroups(title: false, choose: true, playing: false, timerUI: false, over: false);
        SetSpawnersEnabled(false);
    }

    void BeginGameplay(bool timerMode)
    {
        state = timerMode ? Flow.Playing_Timer : Flow.Playing_Classic;
        ToggleGroups(title: false, choose: false, playing: true, timerUI: timerMode, over: false);
        SetSpawnersEnabled(true);

        // Ÿ�Ӿ��� ����
        if (timerMode)
        {
            timerRemain = timeAttackSeconds;
            if (timeText) timeText.text = Mathf.CeilToInt(timerRemain).ToString();
        }

        // ��/����� ù Ŭ�� ���� BGM ����
        AudioManager.Instance?.EnsureBgmPlaying();
    }

    void OpenGameOverUI()
    {
        state = Flow.GameOver;
        ToggleGroups(title: false, choose: false, playing: true, timerUI: false, over: true);
        SetSpawnersEnabled(false);

        if (finalScoreText && GameManager.Instance != null)
            finalScoreText.text = GameManager.Instance.GetScore().ToString();
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ====== Utilities ======
    void ToggleGroups(bool title, bool choose, bool playing, bool timerUI, bool over)
    {
        if (titleGroup) titleGroup.SetActive(title);
        if (chooseModeGroup) chooseModeGroup.SetActive(choose);
        if (playingGroup) playingGroup.SetActive(playing);
        if (timerUIGroup) timerUIGroup.SetActive(timerUI);
        if (gameOverGroup) gameOverGroup.SetActive(over);
    }

    void SetSpawnersEnabled(bool on)
    {
        if (spawners == null) return;
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].enabled = on;
    }
}
