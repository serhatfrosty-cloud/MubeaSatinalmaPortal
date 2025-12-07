namespace MubeaSatinalmaPortal.Services
{
    /// <summary>
    /// Şifreleri güvenli bir şekilde hash'lemek ve doğrulamak için yardımcı sınıf
    /// </summary>
    public class PasswordHasher
    {
        /// <summary>
        /// Düz metni (plain text) şifreyi hash'ler
        /// </summary>
        /// <param name="password">Kullanıcının girdiği şifre</param>
        /// <returns>Hash'lenmiş şifre</returns>
        public static string HashPassword(string password)
        {
            // BCrypt ile şifreyi hash'liyoruz
            // WorkFactor = 12 (güvenlik seviyesi, 10-12 arası öneriliyor)
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <summary>
        /// Kullanıcının girdiği şifreyi veritabanındaki hash ile karşılaştırır
        /// </summary>
        /// <param name="password">Kullanıcının girdiği şifre</param>
        /// <param name="hashedPassword">Veritabanındaki hash'lenmiş şifre</param>
        /// <returns>Eşleşiyorsa true, değilse false</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // BCrypt ile şifreyi doğruluyoruz
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                // Hash geçersizse veya hata varsa false döner
                return false;
            }
        }

        /// <summary>
        /// Şifrenin güçlü olup olmadığını kontrol eder
        /// En az 8 karakter, bir büyük harf, bir küçük harf, bir rakam
        /// </summary>
        public static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // En az 8 karakter
            if (password.Length < 8)
                return false;

            // En az bir büyük harf
            if (!password.Any(char.IsUpper))
                return false;

            // En az bir küçük harf
            if (!password.Any(char.IsLower))
                return false;

            // En az bir rakam
            if (!password.Any(char.IsDigit))
                return false;

            return true;
        }
    }
}