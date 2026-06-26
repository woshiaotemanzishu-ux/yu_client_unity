export const meta = {
  name: 'port-view-bindings',
  description: '并行把多个 Laya view 的绑定逻辑移植成 data-only Unity View(绑数据到烤好的 prefab 节点)',
  whenToUse: '一批 view 已经烤成 prefab + 回填组件后,批量把老客户端 TS 的视图逻辑移植成 Unity 绑定代码。',
  phases: [{ title: 'Port', detail: '每个 view 一个 agent,并行;纯文件读写不碰 Unity' }],
}

// args: { module, oldTsDir, viewsDir, prefabDir, bindDir, reference, views: [name,...] }
const a = (typeof args === 'string' ? JSON.parse(args) : args) || {}
log('args 收到: ' + JSON.stringify(a).slice(0, 300))
const module = a.module || 'Login'
const oldTsDir = a.oldTsDir || 'd:/git_res/yu_client/h5/src/login'
const viewsDir = a.viewsDir || `d:/git_res/yu_client_unity/Assets/Scripts/Module/Core/${module}/Views`
const prefabDir = a.prefabDir || `d:/git_res/yu_client_unity/Assets/Prefabs/UI/${module}`
const bindDir = a.bindDir || `d:/git_res/yu_client_unity/Assets/Scripts/Generated/UI/${module}`
const reference = a.reference || 'LoginCreateRoleView'
// 默认写到 staging(不覆盖 live Views,避免半迁移期碰坏在跑的 monolith 视图);wire 时再人工/逐屏移过去
const outDir = a.outDir || `d:/git_res/yu_client_unity/Tools/Conversion/ported/${module}`
const views = a.views || []

if (!views.length) {
  log('没有要移植的 view(args.views 为空)')
  return { ported: [] }
}

const RESULT = {
  type: 'object',
  required: ['view', 'status', 'summary'],
  properties: {
    view: { type: 'string' },
    status: { type: 'string', enum: ['written', 'skipped', 'failed'] },
    file: { type: 'string' },
    summary: { type: 'string' },
    risks: { type: 'array', items: { type: 'string' } },
  },
}

phase('Port')
const results = await parallel(views.map(view => () =>
  agent(
`把 Laya(LayaAir/TS)的视图 ${view} 移植成 Unity 的 data-only View——只绑数据到【已经烤进 prefab 的真实节点】,绝不在运行时 new 节点或摆位置。

读这些(全是文件读写,别碰 Unity / 别编译):
- 老端逻辑(真相): ${oldTsDir}/${view}.ts  (以及同名 Item:${oldTsDir}/${view.replace(/View$/, '')}Item.ts 若存在)
- 烤出的 prefab(节点结构 + 已绑字段): ${prefabDir}/${view}.prefab  (Unity YAML,看 m_Name 找节点层级)
- 生成的 Bind(可用的序列化字段): ${bindDir}/${view}Bind.cs
- 参考样板(已手工移植、验证过的范式): ${viewsDir}/${reference}.cs  —— 严格照它的写法

移植规则(照样板 ${reference}):
1. 类 ${view} : ${view}Bind,生命周期 OnInit/OnShow/OnHide/OnDispose。
2. 删掉任何"运行时创建节点 / 摆位置"的代码——这些结构已经在 prefab 里。
3. 数据驱动列表:不要 Instantiate 模板,改成【找到 prefab 里已烤好的那几个项节点,给每个绑数据】(参考样板 BindBakedCareers:遍历容器下同名子节点,FindImage/FindText 按路径取内部图/文字,绑名字/图标/点击)。
4. 点击监听每次 OnShow 重绑前先 UIUtil.ClearClicks 再 AddClick(防叠加,样板里有)。
5. 容器字段若没绑上(prefab 里有但 Bind 字段为 null),用 transform.Find 兜底(样板 HeadCon()/ModelCon())。
6. 资源/换肤走 ResManager.SetImageAsync(path, nativeSize:false);文字走 TMP_Text。
7. 真·运行时的东西(3D 模型、网络、特效)保留原样,别动。
8. 数据来源(配置/model)照老端 TS 的语义移植,沿用项目已有的 LoginConfigs/LoginModel/LoginController 等(看样板和现有 ${view}.cs 里在用什么)。

产出:把移植后的完整 C# 写到 ${outDir}/${view}.cs(用 Write,staging 目录,不要碰 live 的 ${viewsDir})。保持 using/namespace 与样板一致。
然后返回结构化结果:status=written/skipped/failed,file=写的路径,summary=一句话说改了啥,risks=你拿不准、需要人工/编译验证的点(尤其是 prefab 节点路径假设、数据字段名)。

注意:你只写代码,不编译、不进游戏。编译和 diff 验收由主流程串行做(Unity 单编辑器)。`,
    { label: `port:${view}`, schema: RESULT }
  ).then(r => r || { view, status: 'failed', summary: 'agent 无返回' }))
)

const written = results.filter(r => r && r.status === 'written')
log(`移植完成 ${written.length}/${views.length};接下来主流程串行:Unity 编译 → 按需 diff 抽查 → /fix-view 收尾`)
return { ported: results }
