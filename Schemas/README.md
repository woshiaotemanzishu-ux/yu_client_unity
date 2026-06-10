# Schemas/configs

存放策划配置表的 Schema 定义。文件名约定：`{table_name}.schema.json`。

每张表一个 Schema，描述字段、类型、主键。**不放在 Assets 下**（避免 Unity 误导入）。

## 工作流

1. 编辑或新增 `*.schema.json`
2. Unity 菜单：`Shenxiao/Config/Generate All`
3. 工具产出：
   - `Assets/Scripts/Generated/Config/{Name}Cfg.cs` — Vo 类（`BaseVo` 子类）
   - `Assets/Scripts/Generated/Config/Config{Name}.cs` — 静态读取器（`LoadAsync` / `Get` / `All`）

## Schema 字段

| 字段 | 必填 | 说明 |
|------|:----:|------|
| `table` | ✓ | 配表名，例如 `config_skill` |
| `vo_name` | | Vo 类名，省略时按 `table` 推导 |
| `source` | | Addressable key，省略时 = `resource/config/server/{table}` |
| `key_field` | ✓ | 主键字段名 |
| `key_type` | ✓ | `int` / `long` / `string` |
| `fields` | ✓ | 字段定义数组 |
| `nested_types` | | 嵌套结构体（map of name -> field list）|
| `comment` | | 类注释 |

## 字段类型

- 标量：`int` / `long` / `float` / `double` / `bool` / `string`
- 数组：在类型后加 `[]` 即可，例如 `int[]` / `string[]` / `int[][]`
- 嵌套：直接写 `nested_types` 中定义的名字，例如 `ParticleCfg` / `ParticleCfg[]`

示例见 `client_attention.schema.json`。