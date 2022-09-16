using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public class LoginInfo
{
    public readonly string EncText;
    public readonly string Salt;
    public readonly string InitVector;

    private const string _plainText = "theCakeIsALie";

    public LoginInfo(string password)
    {
        byte[] salt = new byte[8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);
        Rfc2898DeriveBytes rfc = new(password, salt);
        Aes encAlg = Aes.Create();
        encAlg.Key = rfc.GetBytes(16);
        MemoryStream encryptionStream = new();
        CryptoStream encrypt = new(encryptionStream, encAlg.CreateEncryptor(), CryptoStreamMode.Write);
        byte[] utfData = new UTF8Encoding(false).GetBytes(_plainText);
        encrypt.Write(utfData, 0, utfData.Length);
        encrypt.FlushFinalBlock();
        encrypt.Close();
        byte[] encData = encryptionStream.ToArray();
        rfc.Reset();
        EncText = BitConverter.ToString(encData).Replace("-", "");
        Salt = BitConverter.ToString(salt).Replace("-", "");
        InitVector = BitConverter.ToString(encAlg.IV).Replace("-", "");
    }
    public LoginInfo(string encText, string salt, string initVector)
    {
        EncText = encText;
        Salt = salt;
        InitVector = initVector;
    }

    public bool IsPasswordCorrect(string password)
    {
        try
        {
            byte[] encData = new byte[EncText.Length / 2];
            for (int i = 0; i < encData.Length; i++)
                encData[i] = byte.Parse(EncText.Substring(i * 2, 2), NumberStyles.HexNumber);
            byte[] salt = new byte[Salt.Length / 2];
            for (int i = 0; i < salt.Length; i++)
                salt[i] = byte.Parse(Salt.Substring(i * 2, 2), NumberStyles.HexNumber);
            byte[] initVector = new byte[InitVector.Length / 2];
            for (int i = 0; i < initVector.Length; i++)
                initVector[i] = byte.Parse(InitVector.Substring(i * 2, 2), NumberStyles.HexNumber);
            Rfc2898DeriveBytes rfc = new(password, salt);
            Aes decAlg = Aes.Create();
            decAlg.Key = rfc.GetBytes(16);
            decAlg.IV = initVector;
            MemoryStream decryptionStream = new();
            CryptoStream decrypt = new(decryptionStream, decAlg.CreateDecryptor(), CryptoStreamMode.Write);
            decrypt.Write(encData, 0, encData.Length);
            decrypt.Flush();
            decrypt.Close();
            rfc.Reset();
            return _plainText == new UTF8Encoding(false).GetString(decryptionStream.ToArray());
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
