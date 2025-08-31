using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 상태
    public bool IsGameOver { get; private set; }
    private bool waitingFreezeClick = false; // 클릭 후 정지 모드일 때 true

    [Header("Score UI (선택)")]
    [SerializeField] private TextMeshProUGUI scoreText;

    // 점수
    private int score;

    // n은 1~11단계 사용. [0]은 미사용 — 최초 생성 보너스 체크
    private bool[] firstCreatedStage = new bool[12];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        ResetScoreState();
        Time.timeScale = 1f;
        IsGameOver = false;
    }

    void Start()
    {
        // 씬 시작 시 BGM이 멈춰있다면 재생 보장 (WebGL/모바일 첫 입력 이후 등 케이스 대응)
        AudioManager.Instance?.EnsureBgmPlaying();
    }

    // === 점수 API ===
    public void AddScore(int v)
    {
        if (IsGameOver) return;
        score += v;
        UpdateScoreUI();
    }

    public int GetScore() => score;

    /// <summary>
    /// 병합으로 n단계(1~11)가 새로 만들어질 때 호출해 "얼마를 더할지" 계산.
    /// 기본: 3*(n-1) + (이번 게임에서 최초 생성 단계라면 3^n 보너스)
    /// </summary>
    public int ComputeMergeScoreForStageN(int n)
    {
        if (n < 1 || n > 11) return 0;

        int baseScore = 3 * (n - 1);
        int bonus = 0;

        if (!firstCreatedStage[n])
        {
            bonus = Pow3(n);          // 최초 등장 보너스
            firstCreatedStage[n] = true;
        }

        return baseScore + bonus;
    }

    private int Pow3(int n)
    {
        int r = 1;
        for (int i = 0; i < n; i++) r *= 3;
        return r;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();
    }

    public void ResetScoreState()
    {
        score = 0;
        for (int i = 0; i < firstCreatedStage.Length; i++) firstCreatedStage[i] = false;
        UpdateScoreUI();
    }

    // === 게임 오버 ===
    /// <summary>
    /// waitForClick = true  → 즉시 게임오버 상태로 전환하되 "클릭 시" Time.timeScale=0
    /// waitForClick = false → 즉시 Time.timeScale=0 (즉시 종료)
    /// </summary>
    public void GameOver(bool waitForClick = false)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // 1) 사운드 즉시 정지
        AudioManager.Instance?.StopBgm();

        // 2) 게임플레이 즉시 하드-스톱 (물리/입력 모두 차단)
        StopGameplayImmediately();

        // 3) 타임스케일 처리
        waitingFreezeClick = waitForClick;
        if (!waitingFreezeClick)
        {
            Time.timeScale = 0f; // ★ 즉시 종료
        }

        Debug.Log(waitForClick ? "GAME OVER - Click to freeze" : "GAME OVER - Frozen immediately");
    }

    /// <summary>
    /// 다른 스크립트에서 "무조건 즉시 종료"를 명확하게 호출하고 싶을 때 사용.
    /// (GameOver(false)와 동일)
    /// </summary>
    public void GameOverImmediate() => GameOver(false);

    /// <summary>
    /// 씬 재시작/홈 이동 전에 호출해서 타임스케일이 0으로 남는 이슈 방지
    /// </summary>
    public void ResetTimescaleIfPaused()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    void Update()
    {
        // 클릭 대기 모드: 클릭하면 그 시점에 TimeScale=0
        if (IsGameOver && waitingFreezeClick)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                Time.timeScale = 0f;
                waitingFreezeClick = false;
            }
        }

        // R로 리스타트(임시)
        if (IsGameOver && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // === 즉시 정지 루틴 ===
    void StopGameplayImmediately()
    {
        // 스포너 비활성화 → 입력/드롭 완전 차단
        var spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
            if (spawners[i]) spawners[i].enabled = false;

        // 모든 2D 리지드바디 물리 정지
        var rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < rbs.Length; i++)
            rbs[i].simulated = false; // 물리 즉시 OFF
    }
}
