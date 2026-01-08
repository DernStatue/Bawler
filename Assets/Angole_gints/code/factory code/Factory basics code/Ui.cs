using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FactoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform buildingButtonContainer;
    public GameObject buildingButtonPrefab;
    public TextMeshProUGUI moneyText;
    public GameObject buildingInfoPanel;
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingCostText;
    public TextMeshProUGUI buildingDescriptionText;
    public Image buildingIconImage;

    [Header("Colors")]
    public Color canAffordColor = Color.white;
    public Color cannotAffordColor = Color.red;

    private MachinePlacer machinePlacer;
    private Button[] buildingButtons;

    public void InitializeUI(MachinePlacer placer)
    {
        machinePlacer = placer;
        CreateBuildingButtons();
        UpdateMoneyDisplay();

        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.SetActive(false);
        }
    }

    void CreateBuildingButtons()
    {
        if (buildingButtonContainer == null || buildingButtonPrefab == null) return;

        // Clear existing buttons
        foreach (Transform child in buildingButtonContainer)
        {
            Destroy(child.gameObject);
        }

        buildingButtons = new Button[machinePlacer.buildings.Length];

        // Create button for each building
        for (int i = 0; i < machinePlacer.buildings.Length; i++)
        {
            MachinePlacer.BuildingData building = machinePlacer.buildings[i];
            int index = i; // Capture for closure

            GameObject buttonObj = Instantiate(buildingButtonPrefab, buildingButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            buildingButtons[i] = button;

            // Set button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{building.name}\n${building.cost}";
            }

            // Set button icon if available
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && building.icon != null)
            {
                iconImage.sprite = building.icon;
            }

            // Add click listener
            button.onClick.AddListener(() => OnBuildingButtonClicked(index));

            // Add hover listener for info panel
            AddHoverListeners(buttonObj, index);
        }
    }

    void AddHoverListeners(GameObject buttonObj, int index)
    {
        UnityEngine.EventSystems.EventTrigger trigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // On pointer enter
        UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => ShowBuildingInfo(index));
        trigger.triggers.Add(entryEnter);

        // On pointer exit
        UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => HideBuildingInfo());
        trigger.triggers.Add(entryExit);
    }

    void OnBuildingButtonClicked(int index)
    {
        machinePlacer.SelectBuilding(index);
        HighlightButton(index);
    }

    void ShowBuildingInfo(int index)
    {
        if (buildingInfoPanel == null) return;

        MachinePlacer.BuildingData building = machinePlacer.buildings[index];

        buildingInfoPanel.SetActive(true);

        if (buildingNameText != null)
            buildingNameText.text = building.name;

        if (buildingCostText != null)
        {
            buildingCostText.text = $"Cost: ${building.cost}";
            buildingCostText.color = machinePlacer.playerMoney >= building.cost ? canAffordColor : cannotAffordColor;
        }

        if (buildingDescriptionText != null)
            buildingDescriptionText.text = building.description;

        if (buildingIconImage != null && building.icon != null)
            buildingIconImage.sprite = building.icon;
    }

    void HideBuildingInfo()
    {
        if (buildingInfoPanel != null)
        {
            buildingInfoPanel.SetActive(false);
        }
    }

    void HighlightButton(int index)
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                ColorBlock colors = buildingButtons[i].colors;
                colors.normalColor = (i == index) ? Color.yellow : Color.white;
                buildingButtons[i].colors = colors;
            }
        }
    }

    public void DeselectAllButtons()
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                ColorBlock colors = buildingButtons[i].colors;
                colors.normalColor = Color.white;
                buildingButtons[i].colors = colors;
            }
        }
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${machinePlacer.playerMoney}";
        }

        // Update button colors based on affordability
        UpdateButtonAffordability();
    }

    void UpdateButtonAffordability()
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (buildingButtons[i] != null)
            {
                bool canAfford = machinePlacer.playerMoney >= machinePlacer.buildings[i].cost;
                buildingButtons[i].interactable = canAfford;
            }
        }
    }

    void Update()
    {
        // Continuously update money display
        UpdateMoneyDisplay();
    }
}