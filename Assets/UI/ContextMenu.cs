using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ContextMenu : MonoBehaviour
{
    public GameObject buttonPrefab; // Assign in the inspector, a prefab of the button to be used as menu option
    public List<GameObject> Destroyables;
    private void Awake()
    {
    }

    public void Show(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        ClearOptions();
    }

    // Method to add a new option to the context menu
    public void AddOption(string optionName, UnityEngine.Events.UnityAction action)
    {
        Vector3 offset = new Vector3(0, -100*Destroyables.Count,0);
        
        GameObject buttonObject = Instantiate(buttonPrefab, transform);
        buttonObject.transform.localPosition += offset;
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);
        button.GetComponentInChildren<TextMeshProUGUI>().text = optionName;
        Destroyables.Add(button.gameObject);
    }

    // Clear all options from the context menu
    public void ClearOptions()
    {
        foreach (GameObject button in Destroyables)
        {
            Destroy(button);
        }
        Destroyables.Clear();
    }
}
