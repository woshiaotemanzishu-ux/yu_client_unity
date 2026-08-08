# 共享物品槽与物品详情静态结果

## 范围

- 本轮只修改共享物品槽、普通物品详情和装备详情。
- `SuitModule`/共鸣页面的布局、业务和中央展示不在修改范围；它只作为共享槽代表消费者之一留待运行抽查。
- 未启动 Unity，未使用前台鼠标键盘，未执行真实账号写事务。

## 已落实现状

- `BaseAwardItem`：品质底板、图标、数量、绑定锁、配置限时、`effect_id` 品质流光；父页失活释放句柄，同数据重开恢复。
- `EquipmentItem`：在上述基础上恢复阶数、四星、劣质标、实例品质、强化、实例限时；品质流光与共鸣/套装槽位流光使用独立句柄。
- `BagItemRenderer`：不再在填数据后统一隐藏覆盖件；恢复实例限时、穿戴限制、同部位评分升降。
- `BagEquipmentIcon`：已穿戴槽传入实例品质、强化、绑定和限时；共鸣槽位流光仍只在当前穿戴实例满足状态时 opt-in。
- `GoodsTooltips`：`getway_url` 按老端顺序读取 `GoodsSourceConfig`；详情、来源和按钮按 TMP preferred height 排布，面板限制在 450～680，来源与按钮保留 8px 间距。
- `EquipToolTips`：实例品质/强化/限时及静态阶星统一通过共享 `EquipmentItem` setter；普通装备不打开专用品阶背景。

## 静态资源与编译门禁

- 老端 `GoodsSourceConfig.json` 与 Unity `goodssourceconfig.json`：`TrimEnd()` 后逐字符相等，`PASS`。
- Addressables：GUID `32174530d7bf4fab9fb4c2452a888ecf` 与地址 `resource/config/client/goodssourceconfig` 各一条。
- 六档流光映射保持老端：`1004..1009` 分别对应 `ui_goods_orange/ui_goods_red/ui_goods_gold/ui_goods_pink/UI_1309/UI_1310`；六个根 Prefab 均存在且已进入 `Remote_effect`。
- 共享消费者静态枚举：`BaseAwardItem` 80 个 Prefab 文件，`EquipmentItem` 81 个 Prefab 文件；完整清单与可重跑命令见 `direct-consumers.md`。
- `dotnet build Shenxiao.Module.Core.csproj --no-restore -v:minimal`：0 error。
- `dotnet build Shenxiao.Editor.csproj --no-restore -v:minimal`：0 error；仅 2 条既有 warning。
- `git diff --check`：无 whitespace error；只有工作树既有 LF/CRLF 提示。

## 运行抽样矩阵（尚未执行）

| 形态 | 代表宿主 | 重点状态 |
|---|---|---|
| 普通列表格 | `BagModule/BagItemRenderer` | 空/有数据、绑定、限时、流光开/关、装备禁用与升/降、回收复用 |
| 已穿戴装备槽 | `BagEquipmentIcon` | 空/有装备、品质/强化/阶星、实例限时、共鸣流光开/关、关闭重开 |
| 两类详情 | `CommonModule` | 普通/装备、短/长文案、有/无来源、单/双按钮、滚动、背景包围 |
| 共鸣共享槽 | `SuitModule/EquipSuitPosItem` | 品质流光属于物品槽；中央展示不误加槽位流光 |

以上样本任一失败时才扩大同组范围；当前不能把静态/编译结果写成真实像素 `done`。
