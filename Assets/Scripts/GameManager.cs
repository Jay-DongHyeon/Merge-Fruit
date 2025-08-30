using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameOver { get; private set; }

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private int score;

    // n은 1~11단계 사용. [0]은 미사용
    private bool[] firstCreatedStage = new bool[12];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // 새 게임 시작 상태 초기화
        ResetScoreState();
        Time.timeScale = 1f; // 혹시 모를 잔여 타임스케일 복구
        IsGameOver = false;
    }

    // === 점수 API ===
    public void AddScore(int v)
    {
        if (IsGameOver) return;
        score += v;
        UpdateScoreUI();
        Debug.Log($"Score: {score}");
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
    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // 1) 사운드 즉시 정지
        AudioManager.Instance?.StopBgm();

        // 2) 게임플레이 즉시 하드-스톱 (물리/입력 모두 차단)
        StopGameplayImmediately();

        // 3) 타임스케일 0 (코루틴/애니메 등 일반 흐름도 정지)
        Time.timeScale = 0f;

        Debug.Log("GAME OVER - Press R to Restart");
    }

    void Update()
    {
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
            spawners[i].enabled = false;

        // 모든 2D 리지드바디 물리 정지
        var rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < rbs.Length; i++)
            rbs[i].simulated = false; // 물리 즉시 OFF
    }
}
