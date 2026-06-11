using System.Collections.Generic;
using Shenxiao.Framework.Net;

namespace Shenxiao.Common.Proto
{
    /// <summary>
    /// 角色外观协议块,schema 1:1 镜像 yu_client FigureProtoVo.pro_list
    /// (h5/src/common/FigureProtoVo.ts)。列表字段带 u16 计数前缀。
    /// 改 schema 必须与老客户端同步,否则字节流错位殃及同包后续字段。
    /// 常用字段强类型展开,全量进 Raw 供后续系统取用。
    /// </summary>
    public sealed class FigureProto
    {
        public string name = "";
        public byte sex;
        public byte realm;
        public byte career;
        public ushort level;
        public byte turn;

        /// <summary>schema 全字段(列表字段为 List&lt;Dictionary&gt;)。</summary>
        public readonly Dictionary<string, object> Raw = new Dictionary<string, object>();

        private readonly struct Field
        {
            public readonly string Name;
            public readonly string Fmt;                       // 简单字段格式字符
            public readonly (string name, string fmt)[] Sub;  // 列表字段子结构(带 u16 计数)

            public Field(string name, string fmt) { Name = name; Fmt = fmt; Sub = null; }
            public Field(string name, (string, string)[] sub) { Name = name; Fmt = null; Sub = sub; }
        }

        private static readonly Field[] SCHEMA =
        {
            new Field("name", "s"),
            new Field("sex", "c"),
            new Field("realm", "c"),
            new Field("career", "c"),
            new Field("level", "h"),
            new Field("GM", "c"),
            new Field("vip_flag", "c"),
            new Field("is_hide_vip", "c"),
            new Field("touxian", "c"),
            new Field("level_model_list", new[] { ("part_pos", "c"), ("level_model_id", "i") }),
            new Field("fashion_model_list", new[] { ("part_pos", "c"), ("fashion_model_id", "i"), ("fashion_chartlet_id", "c") }),
            new Field("picture", "s"),
            new Field("prcture_ver", "i"),
            new Field("guild_id", "l"),
            new Field("guild_name", "s"),
            new Field("position", "c"),
            new Field("position_name", "s"),
            new Field("dsgt_id", "i"),
            new Field("liveness_id", "i"),
            new Field("turn", "c"),
            new Field("turn_stage", "c"),
            new Field("grade_id", "c"),
            new Field("is_marriage", "c"),
            new Field("marriage_id", "l"),
            new Field("marriage_name", "s"),
            new Field("escort_state", "i"),
            new Field("block_id", "i"),
            new Field("house_id", "i"),
            new Field("house_lv", "h"),
            new Field("figure_list", new[] { ("figure_type", "c"), ("figure_id", "i"), ("figure_chartlet_id", "i") }),
            new Field("figure_ride_list", new[] { ("figure_type", "c"), ("is_ride", "c") }),
            new Field("achv_lv", "h"),
            new Field("medal_id", "h"),
            new Field("fazhen_id", "i"),
            new Field("dress_list", new[] { ("dress_type", "c"), ("dress_id", "i") }),
            new Field("god_id", "i"),
            new Field("revelation_suit", "i"),
            new Field("demon_id", "i"),
            new Field("supreme_vip", "c"),
            new Field("title_id", "i"),
            new Field("mask_id", "c"),
            new Field("seaCamp", "c"),
            new Field("brick_id", "c"),
            new Field("dummy_type", "c"),
            new Field("suit_fashion_id", "c"),
            new Field("collect_state", "c"),
        };

        public static FigureProto Read(NetReader reader)
        {
            var figure = new FigureProto();
            foreach (Field field in SCHEMA)
            {
                if (field.Sub != null)
                {
                    int count = reader.ReadU16();
                    var list = new List<Dictionary<string, object>>(count);
                    for (int i = 0; i < count; i++)
                    {
                        var item = new Dictionary<string, object>(field.Sub.Length);
                        foreach ((string subName, string subFmt) in field.Sub)
                        {
                            item[subName] = ReadOne(reader, subFmt);
                        }
                        list.Add(item);
                    }
                    figure.Raw[field.Name] = list;
                }
                else
                {
                    figure.Raw[field.Name] = ReadOne(reader, field.Fmt);
                }
            }

            figure.name = (string)figure.Raw["name"];
            figure.sex = (byte)figure.Raw["sex"];
            figure.realm = (byte)figure.Raw["realm"];
            figure.career = (byte)figure.Raw["career"];
            figure.level = (ushort)figure.Raw["level"];
            figure.turn = (byte)figure.Raw["turn"];
            return figure;
        }

        private static object ReadOne(NetReader reader, string fmt)
        {
            switch (fmt)
            {
                case "c": return reader.ReadU8();
                case "C": return reader.ReadI8();
                case "h": return reader.ReadU16();
                case "H": return reader.ReadI16();
                case "i": return reader.ReadU32();
                case "I": return reader.ReadI32();
                case "l": return reader.ReadU64();
                case "L": return reader.ReadI64();
                case "s": return reader.ReadString();
                default: throw new System.ArgumentException("FigureProto 未知格式: " + fmt);
            }
        }
    }
}
