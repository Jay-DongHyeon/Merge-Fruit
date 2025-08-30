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
            rbs[i].simulated = false; // �� ���� ��� OFF (���� ������ ��ٸ��� ����)

        // �ʿ� �� ��ƼŬ/�ִϸ��̼ǵ� ��� ���߰� �ʹٸ� ���⼭ ó�� ����
    }
}
