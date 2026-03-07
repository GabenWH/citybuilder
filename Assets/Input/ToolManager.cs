using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder.Roads
{
    /// <summary>
    /// Manages which tool is active. Assign tools in the inspector; only one is enabled at a time.
    /// </summary>
    public class ToolManager : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> tools = new List<MonoBehaviour>(); // Auto-populated from children that implement ITool.
        [SerializeField] private int defaultToolIndex = 0;
        [SerializeField] private KeyCode[] hotkeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };

        private MonoBehaviour _activeTool;
        public MonoBehaviour ActiveTool => _activeTool;
        public IReadOnlyList<MonoBehaviour> Tools => tools;
        public KeyCode[] Hotkeys => hotkeys;
        public event Action<MonoBehaviour> ActiveToolChanged;

        private static readonly KeyCode[] NumberKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };

        private void Start()
        {
            RefreshToolsFromHierarchy();
            if (tools.Count == 0) return;
            EnsureHotkeys();
            int idx = Mathf.Clamp(defaultToolIndex, 0, tools.Count - 1);
            ActivateTool(tools[idx]);
        }

        private void Update()
        {
            RefreshToolsFromHierarchy();
            EnsureHotkeys();
            for (int i = 0; i < hotkeys.Length && i < tools.Count; i++)
            {
                if (Input.GetKeyDown(hotkeys[i]))
                {
                    ActivateTool(tools[i]);
                }
            }
        }

        private void OnValidate()
        {
            RefreshToolsFromHierarchy();
            EnsureHotkeys();
        }

        public void ActivateTool(MonoBehaviour tool)
        {
            if (tool == _activeTool) return;

            foreach (var t in tools)
            {
                if (t == null) continue;
                bool enable = t == tool;
                if (t.enabled != enable)
                {
                    t.enabled = enable;
                    if (t is ITool lifecycle)
                    {
                        if (enable) lifecycle.OnToolActivated();
                        else lifecycle.OnToolDeactivated();
                    }
                }
            }

            _activeTool = tool;
            ActiveToolChanged?.Invoke(_activeTool);
        }

        private void EnsureHotkeys()
        {
            if (hotkeys == null) hotkeys = NumberKeys;

            // If fewer hotkeys than tools, expand using number keys.
            if (hotkeys.Length < tools.Count)
            {
                int needed = Mathf.Min(NumberKeys.Length, tools.Count);
                var expanded = new KeyCode[needed];
                for (int i = 0; i < needed; i++)
                {
                    expanded[i] = NumberKeys[i];
                }
                hotkeys = expanded;
            }
        }

        private void RefreshToolsFromHierarchy()
        {
            tools.Clear();
            var found = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in found)
            {
                if (mb == null || mb == this) continue;
                if (mb is ITool) tools.Add(mb);
            }
        }
    }
}
