namespace XProtocol
{
    public static class XProtocolEncryptor
    {
        private static string Key => "14022004";

        public static byte[] Encrypt(byte[] data) => RijndaelHandler.Encrypt(data, Key);

        public static byte[] Decrypt(byte[] data) => RijndaelHandler.Decrypt(data, Key);
    }
}