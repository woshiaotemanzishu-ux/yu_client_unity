# Shenxiao AI 编码红线(精简版,完整规范见 Docs/Shenxiao编码规范.md)

## 总原则
- 做游戏就是做工具:能写配置不硬编码,能做工具不手工;数值/路径/资源名进配置。
- 业务行为对齐老客户端 = 查 yu_client TS/PHP 源码,禁止凭记忆猜。
- 端到端验证一个真实样本,再铺开;核心没跑通前不写防护/辅助代码。

## 目录与命名
- 落点决定树/asmdef 归属/命名空间:见编码规范 §1;新增 asmdef 必须先报告。
- 类 PascalCase / 私有字段 _camelCase / 常量 UPPER_SNAKE;Prefab `XxxView.prefab`。

## 禁止
- ❌ `transform.Find` 取节点(用 Bind 字段;唯一豁免 `_tpl_*` 模板内部)
- ❌ 业务代码改 UI 样式(颜色/尺寸/字体/描边)——改模板/默认表/源头
- ❌ `Graphic.enabled=false` 隐藏可点元素(用 color 透明度,enabled 会关掉点击)
- ❌ 绕过 ResManager 加载资源 / 字符串拼 Addressable 路径(走 GameResPath)
- ❌ 绕过 NetManager 收发协议 / 自己写字节解码(格式串照抄 yu_client)
- ❌ EventDispatcher 用 lambda 注册(无法 Off);On/Off 必须配对
- ❌ 空 catch / 吞异常;async void(事件订阅唯一例外)
- ❌ mock/假数据/假接口(除非用户明确要求临时验证)

## UI(LayaUI 主路线)
- 业务 View 继承生成的 `{Name}Bind`;模块合并 prefab 由流程类 Show/Hide 子窗口。
- 交互统一 `UIUtil.AddClick`;动态换图统一 `ResManager.SetImageAsync`。
- 转换产物问题优先修转换器/分析器(通用规则),不点杀单个界面。
