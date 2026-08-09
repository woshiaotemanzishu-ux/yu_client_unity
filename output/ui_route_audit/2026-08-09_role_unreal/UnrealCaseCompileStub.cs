namespace Shenxiao.EditorTools
{
    internal static class CliVerify
    {
        internal sealed class Pkt
        {
            internal Pkt H(long value) => this;
            internal Pkt I(long value) => this;
            internal Pkt C(long value) => this;
            internal Pkt L(long value) => this;
            internal Pkt S(string value) => this;
            internal byte[] Bytes() => System.Array.Empty<byte>();
        }
    }
}
