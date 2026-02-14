using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject MainButtons;
    public GameObject DifficultyButtons;
    public TMP_Text MotionText;

    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.SetInt("d", 1);
        PlayerPrefs.SetInt("m", 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void loadGame()
    {
        SceneManager.LoadScene("test");
    }

    public void closeGame()
    {
        Application.Quit();
    }

    public void loadDifficulties()
    {
        MainButtons.SetActive(false);
        DifficultyButtons.SetActive(true);
    }

    public void setDifficulty(int difficulty)
    {
        PlayerPrefs.SetInt("d", difficulty);
        MainButtons.SetActive(true);
        DifficultyButtons.SetActive(false);
    }

    public void toggleMotionSickness()
    {
        int current = PlayerPrefs.GetInt("m");
        PlayerPrefs.SetInt("m", 1 - current);

        if (current == 1) // Now it's 0
        {
            MotionText.text = "Motion Sickness helper\r\nOFF (Pointer)";
        }
        else // Now It's 1
        {
            MotionText.text = "Motion Sickness helper\r\nON (Pointer)";
        }
    }
}
