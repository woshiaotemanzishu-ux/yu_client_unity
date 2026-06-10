using System.Collections.Generic;

namespace Shenxiao.EditorTools.ConfigGen
{
    /// <summary>
    /// JSON-deserialized schema describing a single config table.
    /// Schemas live under {ProjectRoot}/Schemas/configs/*.schema.json (outside Assets).
    /// </summary>
    [System.Serializable]
    public class ConfigSchema
    {
        public string table;          // e.g. "config_skill" -> ConfigSkill / SkillCfg
        public string vo_name;        // optional override; default = derived from `table`
        public string source;         // optional Addressable key; default = "resource/config/server/{table}"
        public string key_field;      // primary key field name
        public string key_type;       // "int" | "long" | "string"
        public List<FieldDef> fields = new List<FieldDef>();
        public Dictionary<string, List<FieldDef>> nested_types = new Dictionary<string, List<FieldDef>>();
        public string comment;
    }

    [System.Serializable]
    public class FieldDef
    {
        public string name;
        public string type;           // int | long | float | double | bool | string | int[] | float[] | <NestedType>[] | <NestedType>
        public string @default;       // string-encoded default value
        public string comment;
        public bool required;
    }
}
