# Halo 路线静态审计与增量修复

## 收口边界

- 路线：`mainui.halo`
- schema 6：89 节点、74 叶子。
- 叶子：21 `blocked`、53 `needs-runtime-verify`。
- 父级回卷后：35 `blocked`、54 `needs-runtime-verify`，无 `done`、无静态冒充真实 Web。
- 未启动 Unity、浏览器或前台程序；未登录账号；未发送购买、续费、51401、51402。

## 完整控件树

- `mainui.halo`
  - `entry`：MainUI 光环入口与红点（MainUI 跨岛，只读枚举）。
  - `window`
    - `ready`：Activity 层居中页、背景、遮罩与 ready 状态。
    - `close`：关闭按钮、背景返回链。
    - `effect`
      - `visual`：`UI_305414`，宿主 300×200、位置 `(340,15)`、scale 8、循环动态。
      - `illusion-tips`：点击宿主打开 `38065414` 的共享 `IllusionTipsFlow`。
    - `list`
      - `structure`：`ScrollRect -> Viewport(RectMask2D) -> Content(GridLayoutGroup + ContentSizeFitter)`，2 列，单元 284×172，间距 5×5。
      - `scroll`：真实拖动、裁剪、第五行与第 9 项可达。
      - `row-1`：图 `101`；条件 lv1；奖励 `38065414×1 / 34×880 / 35×880`；背景、3 奖励格、领取、锁遮罩、新解锁旗、无说明按钮。
      - `row-2`：图 `102`；条件 lv1；奖励 `38370001×3 / 38370003×3 / 38160001×3`；同六控件，含说明弹窗。
      - `row-3`：图 `103`；条件 lv120；奖励 `32060124×1 / 32060119×10 / 38040044×5`；同六控件，含说明弹窗。
      - `row-4`：图 `104`；条件 lv200；奖励 `38340001×1 / 16020002×5 / 17020002×5`；同六控件，无说明按钮。
      - `row-5`：图 `105`；条件 lv270；奖励 `36255028×10 / 38040030×10 / 32010056×20`；同六控件，含说明弹窗。
      - `row-6`：图 `106`；条件 lv320；奖励 `38370002×3 / 32010316×1 / 32010570×1`；同六控件，含说明弹窗。
      - `row-7`：图 `107`；条件 lv360；奖励 `38040019×1 / 38040018×100 / 38040018×50`；同六控件，含说明弹窗。
      - `row-8`：图 `108`；条件 lv371；奖励 `34010079×1 / 32060124×1 / 35×380`；同六控件，无说明按钮。
      - `row-9`：图 `109`；条件 lv450；奖励 `7305002×1 / 7301001×5 / 7301003×5`；同六控件，含说明弹窗。
    - `purchase`
      - `visibility`：未购买/过期显示；剩余大于 5 天隐藏；小于等于 5 天显示。
      - `label`：立即购买/立即续费。
      - `original-price`：key `5140002`，当前 198。
      - `current-price`：老端 product 154 当前 88；Unity 缺 `config_recharge_product`，Prefab 保存 88 并在代码明确固定来源。
      - `submit`：平台支付或 15804 购买/续费事务。
    - `countdown`：有效期秒级倒计时；无效/过期隐藏。
    - `shared`
      - `base-award-item`：27 个奖励槽统一复用 `Common/BaseAwardItem`。
      - `illusion-tips`：复用 `Common/IllusionTipsFlow`。
      - `team-small-desc`：老端 `TeamSmallDescView`，Unity 当前未落地。
      - `setting-arena`：51402，ArenaSweep=3。
      - `setting-dungeon-equip`：51402，DungeonSweep=5 / Equip。
      - `setting-dungeon-dragon`：51402，DungeonSweep=5 / Dragon。
      - `setting-godbeast`：51402，GodBeastComposite=6。

每个 row 的六个直接子节点均在 `route-manifest.json` 中独立建叶；未用一条“列表通过”替代逐格枚举。

## 已落地的静态修复

- `HaloConfigs` 将 9 条配置解析为强类型条目，解析条件与 27 个奖励元组并按 weight 排序。
- `HaloModel` 增加领奖态读取；51400 全量刷新前清空旧 setting，避免服务端删除项在客户端残留。
- 新增 `HaloItem`，按 51400/等级/任务条件显示领取、已领、遮罩，动态加载 101～109，复用 3 个 `BaseAwardItem`；51401 点击只记录 blocked。
- `HaloMainView` 构建 9 行、监听 Halo/角色/任务刷新、秒级倒计时、购买/续费状态；支付点击只记录 blocked。
- 按老端真实调用参数接入 `UI_305414`，点击复用 `IllusionTipsFlow.Show(38065414)`；动态和足迹仍未用静态宣称完成。
- Prefab 将模板脚本改为页面专属 `HaloItem`，补完整滚动内容结构；现价从错误的 198 修为 88；特效宿主改为运行态 300×200 / `(340,15)`。
- 长说明不以 Toast 冒充 `TeamSmallDescView`；因共享弹窗缺失而明确 blocked。

## Blocked 叶子（21）

- MainUI 入口/红点真实点击 1：跨岛且本轮禁止运行。
- 9 个 51401 领取事务：禁止账号写；未发协议。
- 6 个长说明弹窗：Unity 缺共享 `TeamSmallDescView`，且 Common 不在本岛写权限内。
- 购买/续费 1：禁止平台支付与 15804。
- 外部 51402 特权设置 4：Arena、Equip Dungeon、Dragon Dungeon、GodBeast 均禁止账号写且属跨岛消费者。

## Needs-runtime-verify 叶子（53）

- 页背景/ready、关闭与返回链。
- `UI_305414` 的真实出帧、300×200 足迹、循环动态、关闭清理及 Editor/WebGL 合成。
- `IllusionTips` 的具体 View 身份、遮罩层级、模型出帧和关闭链。
- ScrollRect 真实 GraphicRaycaster 拖动、裁剪、回顶、末项可达。
- 9 行的 101～109 图、27 个真实物品格、奖励图标/品质/数量、锁遮罩、已领/可领/未购买/等级不足、新旗显隐。
- banner 三态、购买/续费文案、两种价格、倒计时秒跳与过期状态。
- `BaseAwardItem` 在 Halo 缩放宿主中的共享身份、点击详情与品质特效裁剪。
- cold/warm 打开、同账号老 H5 对比、720×1280 与宽屏、关闭重开即时一致性。

## 静态验证

- 官方 `route_ledger.py init/apply/validate`：通过。
- 独立 output-only `Halo.StaticCompile.csproj`：0 warning / 0 error；未构建全 Core、Unity 或 WebGL。
- `git diff --check`（Halo C# 与 Halo Prefab）：通过。
- Prefab 74 个组件引用均有声明；HaloItem GUID、GridLayoutGroup、ContentSizeFitter、RectMask2D、现价 88 均通过静态断言。
- 配置断言：9 行、每行 3 奖励，共 27 奖励槽。

## 文件边界

仅修改 Halo 模块/Prefab，并新增本路线 output；未写 MainUI、Role、Equip、Pet、Activity、Common、Generated、Proto、Addressables、Docs 或项目文件。未建分支/worktree，未暂存、未提交。
