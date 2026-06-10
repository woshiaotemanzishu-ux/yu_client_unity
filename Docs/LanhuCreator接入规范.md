# LanhuCreator 接入规范

## 目标

蓝湖只作为视觉和切图来源；Unity 侧以 `lanhu_manifest.json + assets/` 作为稳定导入包，生成：

- `Assets/_App/UI/{View}.prefab`：包内 Loading 等极简首屏 UI
- `Assets/Prefabs/UI/{Module}/{View}.prefab`：远端业务 UI
- `Assets/Scripts/Generated/UI/{Module}/{View}Bind.cs`：自动绑定字段
- `_LanhuReport.md`：缺图、待挂载组件、警告

## 首批范围

| 页面 | 模块 | local | 输出 | 说明 |
|---|---|:---:|---|---|
| AppLoadingView | App | true | `Assets/_App/UI/AppLoadingView.prefab` | 包内首屏，只能用轻量本地图片或纯色/TMP 文本 |
| LoginView | Login | false | `Assets/Prefabs/UI/Login/LoginView.prefab` | Remote UI，图片进入 `Assets/GameRes`，运行时走 CDN/Addressables |

Loading 不能依赖远端 `GameRes`，否则首屏会在 Catalog 初始化前缺图。Login 可以依赖远端资源。

## 导入包结构

```
LanhuPackage/Login/
├── lanhu_manifest.json
└── assets/
    ├── loading_bg.png
    ├── login_bg.jpg
    └── btn_login.png
```

`lanhu_manifest.json` 示例：

```json
{
  "module": "login",
  "assetRoot": "assets",
  "views": [
    {
      "name": "AppLoadingView",
      "local": true,
      "layer": "Loading",
      "width": 720,
      "height": 1280,
      "nodes": [
        {
          "name": "_img_bg",
          "type": "Image",
          "x": 0,
          "y": 0,
          "width": 720,
          "height": 1280,
          "image": "ui/loading/loading_bg",
          "source": "loading_bg.png"
        },
        {
          "name": "_txt_progress",
          "type": "Text",
          "x": 180,
          "y": 980,
          "width": 360,
          "height": 40,
          "text": "加载中",
          "fontSize": 24,
          "color": "#FFFFFFFF"
        },
        {
          "name": "_bar_progress",
          "type": "ProgressBar",
          "x": 160,
          "y": 1030,
          "width": 400,
          "height": 18
        }
      ]
    },
    {
      "name": "LoginView",
      "local": false,
      "layer": "Window",
      "width": 720,
      "height": 1280,
      "nodes": [
        {
          "name": "_img_bg",
          "type": "Image",
          "x": 0,
          "y": 0,
          "width": 720,
          "height": 1280,
          "image": "resource/game/login/texture/login_bg",
          "source": "login_bg.jpg"
        },
        {
          "name": "_input_account",
          "type": "TextInput",
          "x": 160,
          "y": 760,
          "width": 400,
          "height": 56
        },
        {
          "name": "_btn_login",
          "type": "Button",
          "x": 210,
          "y": 880,
          "width": 300,
          "height": 78,
          "image": "resource/game/login/texture/btn_login",
          "source": "btn_login.png",
          "text": "进入游戏"
        }
      ]
    }
  ]
}
```

## 节点规则

节点名以 `_` 开头会生成 Bind 字段。常用前缀：

| 前缀 | Unity 类型 |
|---|---|
| `_btn_` | `Button` |
| `_img_` | `Image` |
| `_txt_` / `_lb_` / `_lab_` | `TextMeshProUGUI` |
| `_input_` / `_ti_` | `TMP_InputField` |
| `_list_` | `ScrollRect` |
| `_tab_` | `ToggleGroup` |
| `_chk_` | `Toggle` |
| `_bar_` | `Slider` |
| `_dd_` | `TMP_Dropdown` |
| `_clip_` | `RectMask2D` |
| `_panel_` / `_box_` | `RectTransform` |

## 图片规则

- `image` 是运行时资源 key，必须可被 `ResourcePath.Normalize()` 归一化。
- `source` 是导入包 `assets/` 下的切图文件名；缺省时用 `image` 或文件名推断。
- `local=true` 的 View 图片复制到 `Assets/_App/UI/Textures/`。
- `local=false` 的 View 图片复制到 `Assets/GameRes/`。
- 找不到图片时不静默替换，报告写入 `_LanhuReport.md`。

## 操作流程

1. 从蓝湖导出 Loading 和 Login 的切图资源。
2. 按规范生成或整理 `lanhu_manifest.json`。
3. Unity 菜单执行：`神霄/UI/蓝湖/导入 Package...`。
4. 第一次导入后等待 Unity 编译生成的 `*Bind.cs`。
5. 再导入一次，让工具挂载 Bind 组件并写入字段引用。
6. 查看 `_LanhuReport.md`，缺图交给 UI/美术补资源。
7. 跑 `神霄/资源/Addressable 自动分组`，把 Login prefab 和图片纳入远端资源。
