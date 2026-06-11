using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shenxiao.Editor.Laya3D
{
    /// <summary>
    /// LayaAir .lm(LAYAMODEL)网格解析,1:1 移植 yu-resource-tool/python/lm_parser.py
    /// (该 python 实现经 Electron 3D 预览渲染验证,是本实现的参考规格与校验基准)。
    /// 支持 LAYAMODEL:05 / 0501 / COMPRESSION_05 / COMPRESSION_0501。
    /// </summary>
    public static class LmParser
    {
        private static readonly Dictionary<string, int> ATTR_SIZES = new Dictionary<string, int>
        {
            { "POSITION", 12 }, { "NORMAL", 12 }, { "COLOR", 16 }, { "UV", 8 },
            { "UV1", 8 }, { "BLENDWEIGHT", 16 }, { "BLENDINDICES", 4 }, { "TANGENT", 16 },
        };

        public static int ComputeStride(string flags)
        {
            int total = 0;
            foreach (string f in flags.Split(','))
            {
                if (!ATTR_SIZES.TryGetValue(f, out int size))
                {
                    throw new ArgumentException(".lm 未知顶点属性: " + f);
                }
                total += size;
            }
            return total;
        }

        public static int AttrOffset(string flags, string attr)
        {
            int off = 0;
            foreach (string f in flags.Split(','))
            {
                if (f == attr) return off;
                off += ATTR_SIZES[f];
            }
            return -1;
        }

        public static LmMesh Parse(byte[] data)
        {
            var reader = new LayaByteReader(data);
            string version = reader.ReadUtfString();
            bool supported = version == "LAYAMODEL:05" || version == "LAYAMODEL:0501"
                || version == "LAYAMODEL:COMPRESSION_05" || version == "LAYAMODEL:COMPRESSION_0501";
            if (!supported)
            {
                throw new ArgumentException("不支持的 .lm 版本: " + version);
            }
            bool isCompressed = version.Contains("COMPRESSION");
            bool is0501 = version.EndsWith("0501");

            var mesh = new LmMesh();

            uint dataOffset = reader.GetUint32();
            reader.GetUint32(); // data_size,未使用

            int blockCount = reader.GetUint16();
            var blockStarts = new uint[blockCount];
            for (int i = 0; i < blockCount; i++)
            {
                blockStarts[i] = reader.GetUint32();
                reader.GetUint32(); // block length,未使用
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
                reader.Pos = (int)blockStarts[bi];
                string blockName = strings[reader.GetUint16()];
                if (blockName == "MESH")
                {
                    ReadMesh(reader, data, (int)dataOffset, strings, mesh, isCompressed, is0501);
                }
                else if (blockName == "SUBMESH")
                {
                    ReadSubMesh(reader, data, (int)dataOffset, mesh);
                }
            }
            return mesh;
        }

        private static void ReadMesh(LayaByteReader reader, byte[] raw, int dataOffset, string[] strings,
            LmMesh mesh, bool isCompressed, bool is0501)
        {
            mesh.Name = strings[reader.GetUint16()];
            int vbCount = reader.GetInt16();

            for (int vb = 0; vb < vbCount; vb++)
            {
                int vbStart = dataOffset + (int)reader.GetUint32();
                int vertexCount = (int)reader.GetUint32();
                string vertexFlag = strings[reader.GetUint16()];
                int stride = ComputeStride(vertexFlag);

                mesh.VertexFlag = vertexFlag;
                mesh.VertexCount = vertexCount;
                mesh.VertexStride = stride;

                if (isCompressed)
                {
                    var vertexData = new byte[stride * vertexCount];
                    int savePos = reader.Pos;
                    reader.Pos = vbStart;
                    string[] subFlags = vertexFlag.Split(',');
                    for (int j = 0; j < vertexCount; j++)
                    {
                        int verOffset = j * stride;
                        foreach (string flag in subFlags)
                        {
                            switch (flag)
                            {
                                case "POSITION":
                                    WriteF(vertexData, verOffset, reader.GetHalf());
                                    WriteF(vertexData, verOffset + 4, reader.GetHalf());
                                    WriteF(vertexData, verOffset + 8, reader.GetHalf());
                                    verOffset += 12;
                                    break;
                                case "NORMAL":
                                    WriteF(vertexData, verOffset, reader.GetUint8() / 127.5f - 1f);
                                    WriteF(vertexData, verOffset + 4, reader.GetUint8() / 127.5f - 1f);
                                    WriteF(vertexData, verOffset + 8, reader.GetUint8() / 127.5f - 1f);
                                    verOffset += 12;
                                    break;
                                case "COLOR":
                                    for (int c = 0; c < 4; c++) WriteF(vertexData, verOffset + c * 4, reader.GetUint8() / 255f);
                                    verOffset += 16;
                                    break;
                                case "UV":
                                case "UV1":
                                    WriteF(vertexData, verOffset, reader.GetHalf());
                                    WriteF(vertexData, verOffset + 4, reader.GetHalf());
                                    verOffset += 8;
                                    break;
                                case "BLENDWEIGHT":
                                    for (int c = 0; c < 4; c++) WriteF(vertexData, verOffset + c * 4, reader.GetUint8() / 255f);
                                    verOffset += 16;
                                    break;
                                case "BLENDINDICES":
                                    for (int c = 0; c < 4; c++) vertexData[verOffset + c] = reader.GetUint8();
                                    verOffset += 4;
                                    break;
                                case "TANGENT":
                                    for (int c = 0; c < 4; c++) WriteF(vertexData, verOffset + c * 4, reader.GetUint8() / 127.5f - 1f);
                                    verOffset += 16;
                                    break;
                            }
                        }
                    }
                    reader.Pos = savePos;
                    mesh.VertexData = vertexData;
                }
                else
                {
                    var vertexData = new byte[vertexCount * stride];
                    Buffer.BlockCopy(raw, vbStart, vertexData, 0, vertexData.Length);
                    mesh.VertexData = vertexData;
                }
            }

            int ibStart = dataOffset + (int)reader.GetUint32();
            int ibLength = (int)reader.GetUint32();
            if (mesh.VertexCount > 65535)
            {
                mesh.IndexFormat32 = true;
                int count = ibLength / 4;
                mesh.IndexData = new int[count];
                for (int i = 0; i < count; i++) mesh.IndexData[i] = BitConverter.ToInt32(raw, ibStart + i * 4);
            }
            else
            {
                mesh.IndexFormat32 = false;
                int count = ibLength / 2;
                mesh.IndexData = new int[count];
                for (int i = 0; i < count; i++) mesh.IndexData[i] = BitConverter.ToUInt16(raw, ibStart + i * 2);
            }

            if (is0501)
            {
                mesh.BoundsMin = new Vector3(reader.GetFloat32(), reader.GetFloat32(), reader.GetFloat32());
                mesh.BoundsMax = new Vector3(reader.GetFloat32(), reader.GetFloat32(), reader.GetFloat32());
            }

            int boneCount = reader.GetUint16();
            for (int i = 0; i < boneCount; i++) mesh.BoneNames.Add(strings[reader.GetUint16()]);

            int bpStart = (int)reader.GetUint32();
            int bpLength = (int)reader.GetUint32();
            int matrixCount = bpLength / 64; // 16 floats
            for (int m = 0; m < matrixCount; m++)
            {
                var mat = new Matrix4x4();
                for (int i = 0; i < 16; i++)
                {
                    // .lm 逆绑定矩阵与 glTF 同为列主序;Unity Matrix4x4[index] 也是列主序,直填
                    mat[i] = BitConverter.ToSingle(raw, dataOffset + bpStart + m * 64 + i * 4);
                }
                mesh.InverseBindPoses.Add(mat);
            }
        }

        private static void ReadSubMesh(LayaByteReader reader, byte[] raw, int dataOffset, LmMesh mesh)
        {
            var sub = new LmSubMesh();
            reader.GetInt16(); // 未使用
            sub.IndexStart = (int)reader.GetUint32();
            sub.IndexCount = (int)reader.GetUint32();

            int drawCount = reader.GetUint16();
            for (int d = 0; d < drawCount; d++)
            {
                int subIdxStart = (int)reader.GetUint32();
                int subIdxCount = (int)reader.GetUint32();
                int boneDicOffset = (int)reader.GetUint32();
                int boneDicCount = (int)reader.GetUint32();
                int boneN = boneDicCount / 2;
                var bones = new ushort[boneN];
                for (int i = 0; i < boneN; i++)
                {
                    bones[i] = BitConverter.ToUInt16(raw, dataOffset + boneDicOffset + i * 2);
                }
                sub.BoneIndicesList.Add(bones);
                sub.DrawCallRanges.Add((subIdxStart, subIdxCount));
            }
            mesh.SubMeshes.Add(sub);
        }

        private static void WriteF(byte[] buf, int offset, float v)
        {
            byte[] b = BitConverter.GetBytes(v);
            buf[offset] = b[0]; buf[offset + 1] = b[1]; buf[offset + 2] = b[2]; buf[offset + 3] = b[3];
        }

        // ---- 属性提取 ----

        public static Vector3[] GetVec3(LmMesh mesh, string attr)
        {
            int off = AttrOffset(mesh.VertexFlag, attr);
            if (off < 0) return null;
            var result = new Vector3[mesh.VertexCount];
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                int p = i * mesh.VertexStride + off;
                result[i] = new Vector3(
                    BitConverter.ToSingle(mesh.VertexData, p),
                    BitConverter.ToSingle(mesh.VertexData, p + 4),
                    BitConverter.ToSingle(mesh.VertexData, p + 8));
            }
            return result;
        }

        public static Vector2[] GetVec2(LmMesh mesh, string attr)
        {
            int off = AttrOffset(mesh.VertexFlag, attr);
            if (off < 0) return null;
            var result = new Vector2[mesh.VertexCount];
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                int p = i * mesh.VertexStride + off;
                result[i] = new Vector2(
                    BitConverter.ToSingle(mesh.VertexData, p),
                    BitConverter.ToSingle(mesh.VertexData, p + 4));
            }
            return result;
        }

        public static Vector4[] GetVec4(LmMesh mesh, string attr)
        {
            int off = AttrOffset(mesh.VertexFlag, attr);
            if (off < 0) return null;
            var result = new Vector4[mesh.VertexCount];
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                int p = i * mesh.VertexStride + off;
                result[i] = new Vector4(
                    BitConverter.ToSingle(mesh.VertexData, p),
                    BitConverter.ToSingle(mesh.VertexData, p + 4),
                    BitConverter.ToSingle(mesh.VertexData, p + 8),
                    BitConverter.ToSingle(mesh.VertexData, p + 12));
            }
            return result;
        }

        /// <summary>BLENDINDICES:每顶点 4 个 uint8(draw call 局部骨骼下标,需经 SubMesh 表映射到全局)。</summary>
        public static byte[][] GetBlendIndices(LmMesh mesh)
        {
            int off = AttrOffset(mesh.VertexFlag, "BLENDINDICES");
            if (off < 0) return null;
            var result = new byte[mesh.VertexCount][];
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                int p = i * mesh.VertexStride + off;
                result[i] = new[] { mesh.VertexData[p], mesh.VertexData[p + 1], mesh.VertexData[p + 2], mesh.VertexData[p + 3] };
            }
            return result;
        }
    }
}
