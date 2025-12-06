using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isRunning = false;

    public TextMeshProUGUI oreText;
    public TextMeshProUGUI ironText;
    public TextMeshProUGUI steelText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI moneyText;

    public Button playPauseButton;
    public TextMeshProUGUI playPauseButtonText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (ResourceManager.Instance != null)
        {
            oreText.text = "Ore: " + ResourceManager.Instance.resources["ore"];
            ironText.text = "Iron: " + ResourceManager.Instance.resources["iron"];
            steelText.text = "Steel: " + ResourceManager.Instance.resources["steel"];
            gearText.text = "Gears: " + ResourceManager.Instance.resources["gear"];
            moneyText.text = "Money: $" + ResourceManager.Instance.resources["money"];
        }
    }

    public void TogglePlayPause()
    {
        isRunning = !isRunning;
        playPauseButtonText.text = isRunning ? "Pause" : "Play";
    }

    public void ResetGame()
    {
        isRunning = false;
        playPauseButtonText.text = "Play";

        // Reset resources
        ResourceManager.Instance.resources["ore"] = 50;
        ResourceManager.Instance.resources["iron"] = 0;
        ResourceManager.Instance.resources["steel"] = 0;
        ResourceManager.Instance.resources["gear"] = 0;
        ResourceManager.Instance.resources["money"] = 100;

        // Destroy all machines
        Machine[] machines = FindObjectsOfType<Machine>();
        foreach (Machine m in machines)
        {
            Destroy(m.gameObject);
        }
    }
}