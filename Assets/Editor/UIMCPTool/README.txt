================================================================================
  UIMCPTool - 通用 UI 自动化操作工具
================================================================================

  用于 MCP (Model Context Protocol) 环境下，通过 JSON 指令驱动 YIUI 框架的
  运行时 UI 操作。支持点击按钮、输入文本、扫描 UI 层级、等待界面就绪。

================================================================================
  文件结构
================================================================================

  UIMCPTool/
  ├── UICommander.cs          核心脚本
  ├── README.txt              本文件
  └── Presets/                 预设流程
      └── enter_main_scene.json   登录并进入主场景

================================================================================
  使用方式
================================================================================

  1. 进入播放模式
  2. 写入指令到 Temp/ui_command.json
  3. 执行菜单 Tools/UI Commander
  4. 读取结果 Temp/ui_result.txt（RUNNING=执行中, DONE=完成）

  使用预设:
    写入 {"preset": "enter_main_scene"} 到 Temp/ui_command.json 即可。

================================================================================
  指令格式
================================================================================

  所有指令放在 commands 数组中，按顺序执行:

  {
    "commands": [
      {"action": "...", ...},
      {"action": "...", ...}
    ]
  }

  或使用预设:

  {
    "preset": "enter_main_scene"
  }

================================================================================
  指令类型
================================================================================

  ---- click (点击按钮) ----

  匹配条件（可组合，全部满足才匹配）:
    name          GameObject 名称精确匹配
    text          按钮子 Text 组件文字包含匹配
    path          完整路径包含匹配

  示例:
    {"action": "click", "name": "LoginBtn"}
    {"action": "click", "text": "进入"}
    {"action": "click", "path": "LobbyPanel(Clone)/EnterMap"}
    {"action": "click", "name": "LoginBtn", "path": "LoginPanel"}

  ---- input (输入文本) ----

  匹配条件（可组合）:
    name          GameObject 名称精确匹配
    path          完整路径包含匹配
    placeholder   占位符文字包含匹配

  参数:
    value         要输入的文本

  示例:
    {"action": "input", "name": "Account", "value": "123"}
    {"action": "input", "placeholder": "请输入帐号", "value": "test"}

  ---- wait (等待条件) ----

  等待条件:
    panel + state:"opened"    Panel(Clone) 存在且 active（默认）
    panel + state:"closed"    Panel(Clone) 不存在或 inactive
    panel + element           Panel 下指定名称的子对象存在且 active

  参数:
    timeout       超时秒数（默认 1 秒）

  panel 字段兼容两种写法: "Login" 或 "LoginPanel" 均可。

  示例:
    {"action": "wait", "panel": "LoginPanel", "element": "Account", "timeout": 15}
    {"action": "wait", "panel": "LobbyPanel", "state": "opened"}
    {"action": "wait", "panel": "LoginPanel", "state": "closed", "timeout": 3}

  ---- scan (扫描 UI) ----

  参数:
    filter        路径子串过滤（可选）

  示例:
    {"action": "scan"}
    {"action": "scan", "filter": "LobbyPanel"}

================================================================================
  通用参数: delay
================================================================================

  每个指令执行后默认等待 0.5 秒再执行下一条，便于肉眼观察。

  可通过 delay 字段覆盖:
    不写            使用默认 0.5 秒
    "delay": 1.5    等待 1.5 秒
    "delay": 0.01   最小延迟（高频操作用）

  示例:
    {"action": "click", "name": "Btn", "delay": 0.01}

================================================================================
  预设文件
================================================================================

  预设存放在 Assets/Editor/UIMCPTool/Presets/ 目录下，文件名即预设名。

  调用方式:
    {"preset": "enter_main_scene"}

  等价于读取 Presets/enter_main_scene.json 中的 commands 执行。

  ---- 当前预设 ----

  enter_main_scene    输入账号密码(123) -> 登录 -> 等待大厅 -> 进入主场景

================================================================================
  输出格式
================================================================================

  结果写入 Temp/ui_result.txt:

  执行中:   RUNNING
  成功:     INPUT: .../Account = "123"
            CLICK: .../LoginBtn
            WAIT: LobbyPanel/EnterMap OK (598ms)
            DONE
  超时:     TIMEOUT: LobbyPanel/EnterMap (1.0s)
  错误:     ERROR: Not in play mode
            WARNING: Click target not found (name=Xxx)

================================================================================
  技术说明
================================================================================

  - 使用 EditorApplication.update 驱动异步轮询，wait 跨帧不阻塞编辑器
  - 通过 Resources.FindObjectsOfTypeAll 查找 DontDestroyOnLoad 中的 UI 对象
  - YIUI 的按钮使用 UITaskEventBindClick (IPointerClickHandler)，非标准 Button
  - wait 通过 YIUI 固定层级路径精准查找，不做全量遍历
  - 命名遵循 YIUI/ET 代码规范（m_/g_ 前缀，显式 private）
