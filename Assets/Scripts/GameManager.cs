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

    // n�� 1~11�ܰ� ���. [0]�� �̻��
    private bool[] firstCreatedStage = new bool[12];

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // �� ���� ���� ���� �ʱ�ȭ
        ResetScoreState();
        Time.timeScale = 1f; // Ȥ�� �� �ܿ� Ÿ�ӽ����� ����
        IsGameOver = false;
    }

    // === ���� API ===
    public void AddScore(int v)
    {
        if (IsGameOver) return;
        score += v;
        UpdateScoreUI();
        Debug.Log($"Score: {score}");
    }

    public int GetScore() => score;

    /// <summary>
    /// �������� n�ܰ�(1~11)�� ���� ������� �� ȣ���� "�󸶸� ������" ���.
    /// �⺻: 3*(n-1) + (�̹� ���ӿ��� ���� ���� �ܰ��� 3^n ���ʽ�)
    /// </summary>
    public int ComputeMergeScoreForStageN(int n)
    {
        if (n < 1 || n > 11) return 0;

        int baseScore = 3 * (n - 1);
        int bonus = 0;

        if (!firstCreatedStage[n])
        {
            bonus = Pow3(n);          // ���� ���� ���ʽ�
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

    // === ���� ���� ===
    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // 1) ���� ��� ����
        AudioManager.Instance?.StopBgm();

        // 2) �����÷��� ��� �ϵ�-���� (����/�Է� ��� ����)
        StopGameplayImmediately();

        // 3) Ÿ�ӽ����� 0 (�ڷ�ƾ/�ִϸ� �� �Ϲ� �帧�� ����)
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

    // === ��� ���� ��ƾ ===
    void StopGameplayImmediately()
    {
        // ������ ��Ȱ��ȭ �� �Է�/��� ���� ����
        var spawners = FindObjectsByType<Spawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++)
            spawners[i].enabled = false;

        // ��� 2D ������ٵ� ���� ����
        var rbs = FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None);
        for (int i = 0; i < rbs.Length; i++)
            rbs[i].simulated = false; // ���� ��� OFF
    }
}
