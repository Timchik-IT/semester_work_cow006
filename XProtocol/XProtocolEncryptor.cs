namespace XProtocol
{
    public static class XProtocolEncryptor
    {
        private static string Key => "11-208";

        public static byte[] Encrypt(byte[] data) => RijndaelHandler.Encrypt(data, Key);

        public static byte[] Decrypt(byte[] data) => RijndaelHandler.Decrypt(data, Key);
    }
}