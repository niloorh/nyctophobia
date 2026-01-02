using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject MainButtons;
    public GameObject DifficultyButtons;

    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.SetInt("d", 1);
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
}
