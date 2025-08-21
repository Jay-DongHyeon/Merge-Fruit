using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private int score;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void AddScore(int v)
    {
        score += v;
        // UI 팀이 나중에 표시 연결 예정. 지금은 로그로 확인.
        Debug.Log($"Score: {score}");
    }

    public void GameOver()
    {
        if (Time.timeScale == 0f) return;
        Time.timeScale = 0f;
        Debug.Log("GAME OVER - Press R to Restart");
    }

    void Update()
    {
        if (Time.timeScale == 0f && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
