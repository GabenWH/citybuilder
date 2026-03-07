using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Builds clickable tool buttons with hotkey labels and highlights the active tool.
    /// </summary>
    public class ToolHotbar : MonoBehaviour
    {
        [SerializeField] private ToolManager manager; // Tool source to build buttons from.
        [SerializeField] private Button buttonPrefab; // Prefab cloned for each tool entry.
        [SerializeField] private Transform container; // Parent for instantiated buttons.
        [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 1f, 0.8f); // Highlight for active tool.
        [SerializeField] private Color inactiveColor = Color.white; // Default button color.
        [SerializeField] private float buttonSpacing = 120f; // Used when no layout group is present.
        [SerializeField] private Vector2 startOffset = Vector2.zero; // Manual offset for button positioning when not using layouts.

        private readonly List<Button> _buttons = new List<Button>();

        private void OnEnable()
        {
            BuildButtons();
            Subscribe();
            RefreshHighlight();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (manager != null)
            {
                manager.ActiveToolChanged += OnToolChanged;
            }
        }

        private void Unsubscribe()
        {
            if (manager != null)
            {
                manager.ActiveToolChanged -= OnToolChanged;
            }
        }

        private void BuildButtons()
        {
            ClearButtons();
            if (manager == null || buttonPrefab == null || container == null){ Debug.Log("Something is missing on the toolbar!");return;}

            var tools = manager.Tools;
            var keys = manager.Hotkeys;

            for (int i = 0; i < tools.Count; i++)
            {
                var btn = Instantiate(buttonPrefab, container);
                _buttons.Add(btn);
                PositionButton(btn, i);
                string keyLabel = i < keys.Length ? keys[i].ToString().Replace("Alpha", "") : string.Empty;
                string toolName = tools[i] != null ? tools[i].name : $"Tool {i}";

                var text = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{keyLabel} {toolName}".Trim();
                }
                else Debug.Log("No text object found in: " + toolName + " button");
                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    if (idx < manager.Tools.Count)
                    {
                        manager.ActivateTool(manager.Tools[idx]);
                    }
                });
            }
        }

        private void ClearButtons()
        {
            foreach (var btn in _buttons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            _buttons.Clear();
        }

        private void OnToolChanged(MonoBehaviour tool)
        {
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            if (manager == null) return;
            var active = manager.ActiveTool;
            for (int i = 0; i < _buttons.Count; i++)
            {
                var btn = _buttons[i];
                if (btn == null) continue;
                var colors = btn.colors;
                colors.normalColor = (i < manager.Tools.Count && manager.Tools[i] == active) ? activeColor : inactiveColor;
                btn.colors = colors;
            }
        }

        private void PositionButton(Button btn, int index)
        {
            // If a layout group exists on the container, let it manage positioning.
            bool hasLayout = container != null && (container.GetComponent<HorizontalOrVerticalLayoutGroup>() != null || container.GetComponent<GridLayoutGroup>() != null);
            if (hasLayout) return;

            var rt = btn.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = new Vector2(0, 1); // top-left
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = startOffset + new Vector2(buttonSpacing * index, 0f);
        }
    }
}
