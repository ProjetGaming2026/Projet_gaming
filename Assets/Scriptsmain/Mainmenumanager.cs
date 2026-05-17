using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject titleText;
    public GameObject titleText2;
    public GameObject zombie;

    void Start()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
        titleText.SetActive(true);
        titleText2.SetActive(true);
        zombie.SetActive(true);
    }

    public void NewGame()
    {
        SceneManager.LoadScene("selectscene");
    }

    public void OpenOptions()
    {
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
        titleText.SetActive(false);
        titleText2.SetActive(false);
        zombie.SetActive(false);
    }

    public void BackToMenu()
    {
        optionsPanel.SetActive(false);
        mainPanel.SetActive(true);
        titleText.SetActive(true);
        titleText2.SetActive(true);
        zombie.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}