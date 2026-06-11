using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// Laya 二进制读取器(小端),1:1 对标 yu-resource-tool/python/lm_parser.ByteReader。
    /// .lm/.lani 均为小端;半精度浮点用于 COMPRESSION 版本。
    /// </summary>
    public sealed class LayaByteReader
    {
        public readonly byte[] Data;
        public int Pos;

        public LayaByteReader(byte[] data)
        {
            Data = data;
            Pos = 0;
        }

        public byte GetUint8() => Data[Pos++];

        public ushort GetUint16()
        {
            ushort v = (ushort)(Data[Pos] | (Data[Pos + 1] << 8));
            Pos += 2;
            return v;
        }

        public short GetInt16() => (short)GetUint16();

        public uint GetUint32()
        {
            uint v = (uint)(Data[Pos] | (Data[Pos + 1] << 8) | (Data[Pos + 2] << 16) | (Data[Pos + 3] << 24));
            Pos += 4;
            return v;
        }

        public int GetInt32() => (int)GetUint32();

        public float GetFloat32()
        {
            float v = System.BitConverter.ToSingle(Data, Pos);
            Pos += 4;
            return v;
        }

        public float GetHalf() => Mathf.HalfToFloat(GetUint16());

        public string ReadUtfString()
        {
            int len = GetUint16();
            string s = Encoding.UTF8.GetString(Data, Pos, len);
            Pos += len;
            return s;
        }
    }

    /// <summary>.lm 解析结果(对标 lm_parser.LmMesh,顶点数据已解压为 float32 布局)。</summary>
    public sealed class LmMesh
    {
        public string Name = "";
        public string VertexFlag = "";
        public int VertexCount;
        public int VertexStride;
        public byte[] VertexData;            // 解压后的逐顶点字节
        public int[] IndexData;              // 统一升为 int
        public bool IndexFormat32;
        public Vector3? BoundsMin;
        public Vector3? BoundsMax;
        public readonly List<string> BoneNames = new List<string>();
        public readonly List<Matrix4x4> InverseBindPoses = new List<Matrix4x4>();
        public readonly List<LmSubMesh> SubMeshes = new List<LmSubMesh>();
    }

    public sealed class LmSubMesh
    {
        public int IndexStart;
        public int IndexCount;
        public readonly List<ushort[]> BoneIndicesList = new List<ushort[]>();   // 每 draw call 的局部→全局骨骼映射
        public readonly List<(int start, int count)> DrawCallRanges = new List<(int, int)>();
    }

    /// <summary>.lani 解析结果(对标 lani_parser.LaniClip)。</summary>
    public sealed class LaniClip
    {
        public string Version = "";
        public string Name = "";
        public float Duration;
        public bool IsLooping;
        public int FrameRate = 30;
        public readonly List<LaniNode> Nodes = new List<LaniNode>();
    }

    /// <summary>一条动画轨:骨骼路径 + 属性 + 关键帧。type: 0=float 1/3/4=vec3 2=quat。</summary>
    public sealed class LaniNode
    {
        public int Type;
        public string[] OwnerPath = System.Array.Empty<string>();
        public string PropertyOwner = "";
        public string[] Properties = System.Array.Empty<string>();
        public readonly List<LaniKeyframe> Keyframes = new List<LaniKeyframe>();

        public string Path => string.Join("/", OwnerPath);
        public string PropertyName => Properties.Length > 0 ? Properties[0] : "";
    }

    /// <summary>关键帧:value/inTangent/outTangent 按 type 取 1/3/4 个分量。</summary>
    public sealed class LaniKeyframe
    {
        public float Time;
        public float[] InTangent;
        public float[] OutTangent;
        public float[] Value;
    }
}
