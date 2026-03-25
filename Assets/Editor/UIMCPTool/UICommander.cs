using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 通用 UI 操作工具。
/// 通过 Temp/ui_command.json 接收指令，结果写入 Temp/ui_result.txt。
///
/// 指令格式:
/// {
///   "commands": [
///     {"action": "scan"},
///     {"action": "scan", "filter": "LobbyPanel"},
///     {"action": "click", "name": "LoginBtn"},
///     {"action": "click", "text": "进入"},
///     {"action": "click", "path": "LobbyPanel(Clone)/EnterMap"},
///     {"action": "input", "name": "Account", "value": "123"},
///     {"action": "input", "placeholder": "请输入帐号", "value": "123"},
///     {"action": "wait", "panel": "LobbyPanel", "state": "opened", "timeout": 1},
///     {"action": "wait", "panel": "LobbyPanel", "element": "EnterMap", "timeout": 1},
///     {"action": "wait", "panel": "LoginPanel", "state": "closed", "timeout": 1}
///   ]
/// }
///
/// 匹配规则:
/// - name: GameObject 名称精确匹配
/// - text: 按钮子对象 Text 组件的文字包含匹配
/// - path: GameObject 完整路径包含匹配
/// - placeholder: InputField 的 placeholder 文字包含匹配
/// - 以上条件可组合，全部满足才匹配
///
/// wait 规则:
/// - panel + state:"opened" → Panel(Clone) 存在且 active
/// - panel + state:"closed" → Panel(Clone) 不存在或 inactive
/// - panel + element → Panel(Clone) 下指定 name 的 GO 存在且 active
/// - timeout 默认 1 秒
/// </summary>
public static class UICommander
{
    private const string CommandFile = "Temp/ui_command.json";
    private const string ResultFile = "Temp/ui_result.txt";
    private const string PresetDir = "Assets/Editor/UIMCPTool/Presets";
    private const float DefaultTimeout = 1f;
    private const float DefaultDelay = 0.5f;

    private static Command[] m_Commands;
    private static int m_CmdIndex;
    private static StringBuilder m_Log;
    private static float m_WaitStart;
    private static float m_WaitTimeout;
    private static bool m_WaitActive;
    private static float m_DelayUntil;
    private static bool m_Running;

    [MenuItem("Tools/UI Commander")]
    public static void Execute()
    {
        if (m_Running)
        {
            Debug.LogWarning("[UICommander] Already running");
            return;
        }

        if (!Application.isPlaying)
        {
            WriteResult("ERROR: Not in play mode\nDONE");
            return;
        }

        if (!File.Exists(CommandFile))
        {
            WriteResult("ERROR: Command file not found\nDONE");
            return;
        }

        string json = File.ReadAllText(CommandFile);
        var batch = JsonUtility.FromJson<CommandBatch>(json);

        // 支持 preset: 从预设文件加载命令
        if (!string.IsNullOrEmpty(batch.preset))
        {
            string presetPath = $"{PresetDir}/{batch.preset}.json";
            if (!File.Exists(presetPath))
            {
                WriteResult($"ERROR: Preset not found: {presetPath}\nDONE");
                return;
            }
            string presetJson = File.ReadAllText(presetPath);
            batch = JsonUtility.FromJson<CommandBatch>(presetJson);
        }

        if (batch.commands == null || batch.commands.Length == 0)
        {
            WriteResult("ERROR: No commands found\nDONE");
            return;
        }

        m_Commands = batch.commands;
        m_CmdIndex = 0;
        m_Log = new StringBuilder(512);
        m_Running = true;

        // 先写一个 RUNNING 状态，外部可以据此判断是否还在执行
        WriteResult("RUNNING");

        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Application.isPlaying)
        {
            Finish("ERROR: Play mode exited");
            return;
        }

        float now = Time.realtimeSinceStartup;

        // 延迟等待中
        if (m_DelayUntil > 0f && now < m_DelayUntil)
        {
            return;
        }
        m_DelayUntil = 0f;

        if (m_CmdIndex >= m_Commands.Length)
        {
            Finish(null);
            return;
        }

        var cmd = m_Commands[m_CmdIndex];

        // wait 命令：初始化或轮询
        if (cmd.action == "wait")
        {
            if (!m_WaitActive)
            {
                // 首次进入，初始化
                m_WaitStart = now;
                m_WaitTimeout = cmd.timeout > 0 ? cmd.timeout : DefaultTimeout;
                m_WaitActive = true;
            }

            if (CheckWaitCondition(cmd))
            {
                float elapsed = now - m_WaitStart;
                m_Log.AppendLine($"WAIT: {FormatWaitDesc(cmd)} OK ({elapsed * 1000:F0}ms)");
                m_WaitActive = false;
                m_CmdIndex++;
                ScheduleDelay(cmd);
            }
            else if (now - m_WaitStart > m_WaitTimeout)
            {
                m_Log.AppendLine($"TIMEOUT: {FormatWaitDesc(cmd)} ({m_WaitTimeout:F1}s)");
                m_WaitActive = false;
                m_CmdIndex++;
            }
            return;
        }

        // 同步命令
        ExecuteSync(cmd);
        m_CmdIndex++;
        ScheduleDelay(cmd);
    }

    /// <summary>
    /// 根据命令的 delay 字段安排延迟，默认 0.5 秒。
    /// delay 未指定(0) 用默认值，delay > 0 用指定值（最小 0.01）。
    /// </summary>
    private static void ScheduleDelay(Command cmd)
    {
        // JsonUtility 未指定的 float 字段为 0，视为"用默认值"
        float delay = cmd.delay > 0 ? Mathf.Max(cmd.delay, 0.01f) : DefaultDelay;
        m_DelayUntil = Time.realtimeSinceStartup + delay;
    }

    private static void ExecuteSync(Command cmd)
    {
        switch (cmd.action)
        {
            case "scan":
                DoScan(m_Log, cmd.filter);
                break;
            case "click":
                DoClick(m_Log, cmd);
                break;
            case "input":
                DoInput(m_Log, cmd);
                break;
            default:
                m_Log.AppendLine($"ERROR: Unknown action '{cmd.action}'");
                break;
        }
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        m_Running = false;

        if (!string.IsNullOrEmpty(error))
            m_Log.AppendLine(error);
        m_Log.AppendLine("DONE");

        WriteResult(m_Log.ToString());

        m_Commands = null;
        m_Log = null;
    }

    private static void WriteResult(string content)
    {
        File.WriteAllText(ResultFile, content, Encoding.UTF8);
    }

    // ==================== WAIT ====================

    private static bool CheckWaitCondition(Command cmd)
    {
        if (string.IsNullOrEmpty(cmd.panel)) return true;

        string cloneName = cmd.panel + "Panel(Clone)";
        // 也兼容直接写 "LoginPanel" 而非 "Login"
        if (cmd.panel.EndsWith("Panel"))
            cloneName = cmd.panel + "(Clone)";

        GameObject panelGO = FindPanelClone(cloneName);

        // state: closed
        if (cmd.state == "closed")
            return panelGO == null || !panelGO.activeInHierarchy;

        // state: opened (default)
        if (string.IsNullOrEmpty(cmd.element))
            return panelGO != null && panelGO.activeInHierarchy;

        // panel + element
        if (panelGO == null || !panelGO.activeInHierarchy) return false;
        return FindChildByName(panelGO.transform, cmd.element) != null;
    }

    /// <summary>
    /// 精准查找 Panel Clone，不遍历所有 GO。
    /// YIUI 的 Panel 挂在固定的 Layer 节点下，用 Transform.Find 逐层定位。
    /// 退化方案：在 Layer 子节点中按名称查找。
    /// </summary>
    private static GameObject FindPanelClone(string cloneName)
    {
        // 尝试已知的 YIUI 层级路径
        // Global/YIUIRoot/YIUICanvasRoot/YIUILayerRoot/Layer{N}-{Type}/{PanelName}(Clone)
        var yiuiRoot = GameObject.Find("Global/YIUIRoot/YIUICanvasRoot/YIUILayerRoot");
        if (yiuiRoot == null) return null;

        // 遍历 Layer 节点（通常只有 5-7 个）
        var layerRoot = yiuiRoot.transform;
        for (int i = 0; i < layerRoot.childCount; i++)
        {
            var layer = layerRoot.GetChild(i);
            // 在每个 Layer 下找目标 Panel（通常每个 Layer 下只有 0-3 个 Panel）
            var found = layer.Find(cloneName);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 在 Panel 下递归查找指定名称的 active 子对象。
    /// 限制深度避免在复杂 UI 中卡顿。
    /// </summary>
    private static GameObject FindChildByName(Transform parent, string name, int maxDepth = 8)
    {
        return FindChildRecursive(parent, name, 0, maxDepth);
    }

    private static GameObject FindChildRecursive(Transform parent, string name, int depth, int maxDepth)
    {
        if (depth > maxDepth) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            if (child.name == name) return child.gameObject;
            var found = FindChildRecursive(child, name, depth + 1, maxDepth);
            if (found != null) return found;
        }
        return null;
    }

    private static string FormatWaitDesc(Command cmd)
    {
        if (!string.IsNullOrEmpty(cmd.element))
            return $"{cmd.panel}/{cmd.element}";
        return $"{cmd.panel} {cmd.state ?? "opened"}";
    }

    // ==================== SCAN ====================

    private static void DoScan(StringBuilder sb, string filter)
    {
        sb.AppendLine($"--- SCAN {(string.IsNullOrEmpty(filter) ? "(all)" : filter)} ---");

        bool hasFilter = !string.IsNullOrEmpty(filter);
        var allRT = Resources.FindObjectsOfTypeAll<RectTransform>();
        var compSb = new StringBuilder(128);

        for (int i = 0; i < allRT.Length; i++)
        {
            var go = allRT[i].gameObject;
            if (go.scene.name == null || go.hideFlags != HideFlags.None) continue;

            string path = null;
            if (hasFilter)
            {
                if (!MightMatchFilter(go.transform, filter)) continue;
                path = BuildPath(go);
                if (!path.Contains(filter)) continue;
            }
            else
            {
                path = BuildPath(go);
            }

            compSb.Clear();
            var comps = go.GetComponents<Component>();
            bool hasClickHandler = false;
            bool hasInputField = false;

            for (int j = 0; j < comps.Length; j++)
            {
                var c = comps[j];
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "RectTransform" || n == "CanvasRenderer") continue;
                if (compSb.Length > 0) compSb.Append(',');
                compSb.Append(n);
                if (c is IPointerClickHandler) hasClickHandler = true;
                if (c is InputField) hasInputField = true;
            }

            string extra = "";
            var textComp = go.GetComponent<Text>();
            if (textComp != null) extra = $" text=\"{textComp.text}\"";

            var inputComp = go.GetComponent<InputField>();
            if (inputComp != null)
            {
                string ph = "";
                if (inputComp.placeholder != null)
                {
                    var phText = inputComp.placeholder.GetComponent<Text>();
                    if (phText != null) ph = phText.text;
                }
                extra = $" value=\"{inputComp.text}\" placeholder=\"{ph}\"";
            }

            if (compSb.Length == 0 && string.IsNullOrEmpty(extra) && go.transform.childCount > 0)
                continue;

            sb.Append(path);
            if (compSb.Length > 0) { sb.Append(" ["); sb.Append(compSb); sb.Append(']'); }
            if (hasClickHandler && !hasInputField) sb.Append(" [CLICKABLE]");
            if (!string.IsNullOrEmpty(extra)) sb.Append(extra);
            sb.AppendLine();
        }
    }

    private static bool MightMatchFilter(Transform t, string filter)
    {
        while (t != null)
        {
            if (t.name.Contains(filter)) return true;
            t = t.parent;
        }
        return false;
    }

    // ==================== CLICK ====================

    private static void DoClick(StringBuilder sb, Command cmd)
    {
        // 按需构建查找数据（轻量，只在 click 时构建）
        var clickables = BuildClickables();

        // 策略1: name 精确匹配
        if (!string.IsNullOrEmpty(cmd.name))
        {
            for (int i = 0; i < clickables.Count; i++)
            {
                var entry = clickables[i];
                if (entry.go.name != cmd.name) continue;
                if (!string.IsNullOrEmpty(cmd.path) && !BuildPath(entry.go).Contains(cmd.path)) continue;
                if (!string.IsNullOrEmpty(cmd.text))
                {
                    var t = entry.go.GetComponentInChildren<Text>();
                    if (t == null || !t.text.Contains(cmd.text)) continue;
                }
                sb.AppendLine($"CLICK: {BuildPath(entry.go)}");
                SimulateClick(entry.handler);
                return;
            }
        }

        // 策略2: path 包含匹配
        if (!string.IsNullOrEmpty(cmd.path) && string.IsNullOrEmpty(cmd.name))
        {
            for (int i = 0; i < clickables.Count; i++)
            {
                var entry = clickables[i];
                if (!BuildPath(entry.go).Contains(cmd.path)) continue;
                if (!string.IsNullOrEmpty(cmd.text))
                {
                    var t = entry.go.GetComponentInChildren<Text>();
                    if (t == null || !t.text.Contains(cmd.text)) continue;
                }
                sb.AppendLine($"CLICK: {BuildPath(entry.go)}");
                SimulateClick(entry.handler);
                return;
            }
        }

        // 策略3: text 匹配
        if (!string.IsNullOrEmpty(cmd.text))
        {
            // 先在 clickable 的子 Text 中找
            if (string.IsNullOrEmpty(cmd.name) && string.IsNullOrEmpty(cmd.path))
            {
                for (int i = 0; i < clickables.Count; i++)
                {
                    var entry = clickables[i];
                    var t = entry.go.GetComponentInChildren<Text>();
                    if (t != null && t.text.Contains(cmd.text))
                    {
                        sb.AppendLine($"CLICK: {BuildPath(entry.go)}");
                        SimulateClick(entry.handler);
                        return;
                    }
                }
            }

            // 从 Text 向上找 clickable 祖先
            var texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                var txt = texts[i];
                if (txt.gameObject.scene.name == null) continue;
                if (!txt.text.Contains(cmd.text)) continue;

                Transform t = txt.transform.parent;
                while (t != null)
                {
                    var handler = t.GetComponent<IPointerClickHandler>();
                    if (handler != null && t.GetComponent<InputField>() == null)
                    {
                        sb.AppendLine($"CLICK (via text): {BuildPath(t.gameObject)}");
                        SimulateClick(handler);
                        return;
                    }
                    t = t.parent;
                }
            }
        }

        sb.AppendLine($"WARNING: Click target not found (name={cmd.name}, text={cmd.text}, path={cmd.path})");
    }

    private static List<ClickableEntry> BuildClickables()
    {
        var all = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        var result = new List<ClickableEntry>(32);
        var seen = new HashSet<int>();

        for (int i = 0; i < all.Length; i++)
        {
            var mb = all[i];
            if (mb == null) continue;
            if (!(mb is IPointerClickHandler handler)) continue;
            var go = mb.gameObject;
            if (go.scene.name == null || go.hideFlags != HideFlags.None) continue;
            int id = go.GetInstanceID();
            if (!seen.Add(id)) continue;
            if (go.GetComponent<InputField>() != null) continue;
            result.Add(new ClickableEntry { go = go, handler = handler });
        }
        return result;
    }

    // ==================== INPUT ====================

    private static void DoInput(StringBuilder sb, Command cmd)
    {
        var inputFields = Resources.FindObjectsOfTypeAll<InputField>();

        for (int i = 0; i < inputFields.Length; i++)
        {
            var field = inputFields[i];
            var go = field.gameObject;
            if (go.scene.name == null || go.hideFlags != HideFlags.None) continue;

            if (!string.IsNullOrEmpty(cmd.name) && go.name != cmd.name) continue;
            if (!string.IsNullOrEmpty(cmd.path) && !BuildPath(go).Contains(cmd.path)) continue;
            if (!string.IsNullOrEmpty(cmd.placeholder))
            {
                if (field.placeholder == null) continue;
                var phText = field.placeholder.GetComponent<Text>();
                if (phText == null || !phText.text.Contains(cmd.placeholder)) continue;
            }

            if (string.IsNullOrEmpty(cmd.name) && string.IsNullOrEmpty(cmd.path) && string.IsNullOrEmpty(cmd.placeholder))
            {
                sb.AppendLine("WARNING: input command has no matching condition");
                return;
            }

            field.text = cmd.value ?? "";
            sb.AppendLine($"INPUT: {BuildPath(go)} = \"{cmd.value}\"");
            return;
        }

        sb.AppendLine($"WARNING: InputField not found (name={cmd.name}, placeholder={cmd.placeholder}, path={cmd.path})");
    }

    // ==================== UTILS ====================

    struct ClickableEntry
    {
        public GameObject go;
        public IPointerClickHandler handler;
    }

    private static void SimulateClick(IPointerClickHandler handler)
    {
        var eventData = new PointerEventData(EventSystem.current);
        eventData.button = PointerEventData.InputButton.Left;
        handler.OnPointerClick(eventData);
    }

    private static string BuildPath(GameObject go)
    {
        var parts = g_Parts;
        parts.Clear();
        Transform t = go.transform;
        while (t != null) { parts.Add(t.name); t = t.parent; }

        var sb = g_PathBuilder;
        sb.Clear();
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            if (sb.Length > 0) sb.Append('/');
            sb.Append(parts[i]);
        }
        return sb.ToString();
    }

    private static readonly List<string> g_Parts = new List<string>(16);
    private static readonly StringBuilder g_PathBuilder = new StringBuilder(256);

    // ==================== DATA ====================

    [System.Serializable]
    public class CommandBatch
    {
        public string preset;      // 预设名称，从 Temp/ui_presets/{preset}.json 加载
        public Command[] commands;
    }

    [System.Serializable]
    public class Command
    {
        public string action;      // scan, click, input, wait
        public string filter;      // scan: path filter
        public string name;        // click/input: GameObject name (exact)
        public string text;        // click: button text (contains)
        public string path;        // click/input: path (contains)
        public string placeholder; // input: placeholder text (contains)
        public string value;       // input: value to set
        public string panel;       // wait: panel name (e.g. "Login" or "LoginPanel")
        public string state;       // wait: "opened" (default) or "closed"
        public string element;     // wait: element name under panel
        public float timeout;      // wait: timeout in seconds (default 1)
        public float delay;         // 执行后延迟秒数 (0=默认0.5s, >0=指定秒数, 最小0.01)
    }
}
