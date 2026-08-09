# LevelReward 独立 QA 静态补充

- `_gp_get` 内全部 `Graphic` 统一通过 `UIGrayStyle.Apply` 切换灰阶：`received=0/3/4` 灰态，`received=1` 正常，`received=2` 继续隐藏领取面。
- `ClearRows/DestroyRow` 在 `Destroy` 或 `DestroyImmediate` 前先 `SetActive(false)`，避免同一帧事件重建时旧行与新行短暂重叠。
- 奖励配置、性别/限量奖励格和页面打开时主动请求 41700 仍因未公开配置契约或 RushGift 跨文件岛保持 `blocked`；本补充未修改 RushGift。
- 仍未运行 Unity、浏览器或真实 Web；灰阶玩家像素、41700 到达时同帧列表替换和 hide/reopen 保持 `needs-runtime-verify`。
