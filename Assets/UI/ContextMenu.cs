using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ContextMenu : MonoBehaviour
{
    // Prefab for each menu option button. Must have a Button component and optional TextMeshProUGUI child for labeling.
    public GameObject buttonPrefab;

    // List of spawned buttons that should be destroyed when the menu is cleared/hidden.
    public List<GameObject> Destroyables = new List<GameObject>();

    [SerializeField] private float horizontalPadding = 24f; // Extra width added to the text width so the button comfortably fits its label.

    // Cached references to position the menu within its parent Canvas.
    private RectTransform _rect;
    private Canvas _canvas;
    private float _maxButtonWidth = 0f; // Track the widest button so all options share the same width.
    private void Awake()
    {
        // Ensure list is initialized to avoid null refs when adding/removing.
        if (Destroyables == null) Destroyables = new List<GameObject>();

        // Warn if the menu cannot spawn buttons (developer misconfiguration).
        if (buttonPrefab == null)
        {
            Debug.LogWarning("ContextMenu is missing buttonPrefab assignment.", this);
        }

        // Cache layout references for screen-to-rect positioning.
        _rect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogWarning("ContextMenu has no parent Canvas; it may not render correctly.", this);
        }
    }

    /// <summary>
    /// Positions the menu at the given screen-space point and shows it. If a Canvas is present,
    /// converts screen coordinates into the canvas space so the menu anchors correctly.
    /// </summary>
    public void Show(Vector3 position)
    {
        if (_rect != null && _canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                position,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var localPoint);
            _rect.anchoredPosition = localPoint;
        }
        else
        {
            transform.position = position;
        }
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the menu and clears all instantiated options so next show starts fresh.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        ClearOptions();
    }

    /// <summary>
    /// Adds a new button to the menu with the provided label and click action.
    /// Buttons are stacked downward by adjusting anchoredPosition based on prefab height.
    /// </summary>
    public void AddOption(string optionName, UnityEngine.Events.UnityAction action)
    {
        if (buttonPrefab == null)
        {
            Debug.LogError("ContextMenu cannot add option: buttonPrefab not set.", this);
            return;
        }

        GameObject buttonObject = Instantiate(buttonPrefab, transform);
        var btnRect = buttonObject.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.anchorMin = new Vector2(0, 1);
            btnRect.anchorMax = new Vector2(0, 1);
            btnRect.pivot = new Vector2(0, 1);
            float yOffset = -(btnRect.sizeDelta.y* 4 * Destroyables.Count);
            btnRect.anchoredPosition = new Vector2(0, yOffset);
        }

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);
        var label = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        float preferredWidth = 0f;
        if (label != null)
        {
            label.text = optionName;
            preferredWidth = label.GetPreferredValues(optionName).x + horizontalPadding;
        }
        else
        {
            Debug.LogWarning("ContextMenu button prefab has no TextMeshProUGUI child.", buttonObject);
        }
        Destroyables.Add(button.gameObject);

        // Enforce uniform width large enough for the longest label seen so far.
        float targetWidth = preferredWidth > 0f ? preferredWidth : (btnRect != null ? btnRect.sizeDelta.x : 0f);
        _maxButtonWidth = Mathf.Max(_maxButtonWidth, targetWidth);
        UpdateButtonWidths();
    }

    /// <summary>
    /// Destroys all spawned option buttons and empties the list to prevent leaks/duplicates.
    /// </summary>
    public void ClearOptions()
    {
        foreach (GameObject button in Destroyables)
        {
            Destroy(button);
        }
        Destroyables.Clear();
        _maxButtonWidth = 0f;
    }

    // Resize all spawned buttons to match the widest label seen, keeping height untouched.
    private void UpdateButtonWidths()
    {
        if (_maxButtonWidth <= 0f) return;
        foreach (var go in Destroyables)
        {
            if (go == null) continue;
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;
            var size = rt.sizeDelta;
            size.x = _maxButtonWidth;
            rt.sizeDelta = size;
        }
    }
}
