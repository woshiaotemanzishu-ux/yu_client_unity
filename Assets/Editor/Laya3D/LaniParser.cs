using System;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// LayaAir .lani(LAYAANIMATION)动画解析,1:1 移植 yu-resource-tool/python/lani_parser.py。
    /// 支持 LAYAANIMATION:03 / 04 / COMPRESSION_04。事件段读取但不导出(战斗特效阶段再接)。
    /// </summary>
    public static class LaniParser
    {
        public static LaniClip Parse(byte[] data)
        {
            var reader = new LayaByteReader(data);
            string version = reader.ReadUtfString();
            if (!version.StartsWith("LAYAANIMATION"))
            {
                throw new ArgumentException("不是 LAYAANIMATION 文件: " + version);
            }
            bool isCompressed = version.Contains("COMPRESSION");

            var clip = new LaniClip { Version = version };

            uint dataOffset = reader.GetUint32();
            reader.GetUint32(); // data_size

            int blockCount = reader.GetUint16();
            for (int i = 0; i < blockCount; i++)
            {
                reader.GetUint32(); // block start(.lani 实际文件块体紧随头部,start 仅元数据)
                reader.GetUint32(); // block length
            }

            uint strRelOffset = reader.GetUint32();
            int strCount = reader.GetUint16();
            int savePos = reader.Pos;
            reader.Pos = (int)(strRelOffset + dataOffset);
            var strings = new string[strCount];
            for (int i = 0; i < strCount; i++) strings[i] = reader.ReadUtfString();
            reader.Pos = savePos;

            for (int bi = 0; bi < blockCount; bi++)
            {
                string blockName = strings[reader.GetUint16()];
                if (blockName == "ANIMATIONS")
                {
                    ReadAnimations(reader, strings, clip, isCompressed);
                }
            }
            return clip;
        }

        private static float ReadF(LayaByteReader reader, bool compressed)
        {
            return compressed ? reader.GetHalf() : reader.GetFloat32();
        }

        private static void ReadAnimations(LayaByteReader reader, string[] strings, LaniClip clip, bool compressed)
        {
            int timeCount = reader.GetUint16();
            var times = new float[timeCount];
            for (int i = 0; i < timeCount; i++) times[i] = reader.GetFloat32();

            clip.Name = strings[reader.GetUint16()];
            clip.Duration = reader.GetFloat32();
            clip.IsLooping = reader.GetUint8() != 0;
            clip.FrameRate = reader.GetInt16();
            int nodeCount = reader.GetInt16();

            for (int n = 0; n < nodeCount; n++)
            {
                var node = new LaniNode { Type = reader.GetUint8() };

                int pathLen = reader.GetUint16();
                node.OwnerPath = new string[pathLen];
                for (int i = 0; i < pathLen; i++) node.OwnerPath[i] = strings[reader.GetUint16()];

                node.PropertyOwner = strings[reader.GetUint16()];

                int propLen = reader.GetUint16();
                node.Properties = new string[propLen];
                for (int i = 0; i < propLen; i++) node.Properties[i] = strings[reader.GetUint16()];

                int kfCount = reader.GetUint16();
                int components = node.Type == 0 ? 1 : node.Type == 2 ? 4 : 3;
                for (int k = 0; k < kfCount; k++)
                {
                    var kf = new LaniKeyframe
                    {
                        Time = times[reader.GetUint16()],
                        InTangent = new float[components],
                        OutTangent = new float[components],
                        Value = new float[components],
                    };
                    for (int c = 0; c < components; c++) kf.InTangent[c] = ReadF(reader, compressed);
                    for (int c = 0; c < components; c++) kf.OutTangent[c] = ReadF(reader, compressed);
                    for (int c = 0; c < components; c++) kf.Value[c] = ReadF(reader, compressed);
                    node.Keyframes.Add(kf);
                }
                clip.Nodes.Add(node);
            }
            // 事件段:顺序消费保证流位置正确,内容暂不导出
            int eventCount = reader.GetUint16();
            for (int e = 0; e < eventCount; e++)
            {
                reader.GetFloat32();            // time
                reader.GetUint16();             // event name idx
                int paramCount = reader.GetUint16();
                for (int p = 0; p < paramCount; p++)
                {
                    int etype = reader.GetUint8();
                    switch (etype)
                    {
                        case 0: reader.GetUint8(); break;
                        case 1: reader.GetInt32(); break;
                        case 2: reader.GetFloat32(); break;
                        case 3: reader.GetUint16(); break;
                    }
                }
            }
        }
    }
}
