// Scripts/GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameOver { get; private set; }

    private int score;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void AddScore(int v)
    {
        if (IsGameOver) return;
        score += v;
        Debug.Log($"Score: {score}");
    }

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
            rbs[i].simulated = false; // ★ 물리 즉시 OFF (다음 프레임 기다리지 않음)

        // 필요 시 파티클/애니메이션도 즉시 멈추고 싶다면 여기서 처리 가능
    }
}
