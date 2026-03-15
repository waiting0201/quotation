using System.Security.Cryptography;
using System.Text;

namespace QuotationApi.Helpers;

public static class PasswordHelper
{
    private const string Salt = "weypro168";

    /// <summary>
    /// 計算密碼的 SHA1 hash：SHA1("weypro168" + password)，輸出大寫 hex
    /// 相容原系統 FormsAuthentication.HashPasswordForStoringInConfigFile(salt + password, "SHA1")
    /// </summary>
    public static string HashPassword(string password)
    {
        string salted = Salt + password;
        using var sha1 = SHA1.Create();
        byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(salted));
        return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
    }

    /// <summary>
    /// 驗證明文密碼是否符合已儲存的 hash
    /// </summary>
    public static bool VerifyPassword(string plainPassword, string storedHash)
    {
        string computed = HashPassword(plainPassword);
        // 使用常數時間比較，防止 timing attack（統一轉大寫比對）
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed.ToUpperInvariant()),
            Encoding.UTF8.GetBytes((storedHash ?? string.Empty).ToUpperInvariant()));
    }
}
