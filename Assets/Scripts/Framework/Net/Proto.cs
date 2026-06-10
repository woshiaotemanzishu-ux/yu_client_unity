namespace Shenxiao.Framework.Net
{
    /// <summary>
    /// Protocol id constants. Mirrors yu_server protocol numbers.
    /// Naming: CS_* = client to server, SC_* = server to client.
    /// Append new entries here, do not scatter ids.
    /// </summary>
    public static class Proto
    {
        // ----- Login (1xxxx) -----
        public const int CS_LOGIN = 11001;
        public const int SC_LOGIN = 11002;

        // ----- Heartbeat -----
        public const int CS_HEARTBEAT = 10001;
        public const int SC_HEARTBEAT = 10002;

        // ----- Role (12xxx) -----
        public const int CS_ROLE_INFO = 12001;
        public const int SC_ROLE_INFO = 12002;

        // Append more protocol ids here. Keep request/response adjacent.
    }
}
