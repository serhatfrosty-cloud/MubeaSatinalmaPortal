using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MubeaSatinalmaPortal.Services; // YENİ: PasswordHasher için
using System;
// ✅ YENİ:
using Microsoft.Data.SqlClient;
using System.Threading.Tasks; // ← Bunu da ekleyin

namespace MubeaSatinalmaPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Boş değer kontrolü
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["LoginError"] = "Kullanıcı adı veya şifre boş olamaz.";
                return RedirectToAction("Index", "Home");
            }

            string connStr = _config.GetConnectionString("MubeaDB");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT 
                            [User],
                            UserPassword,
                            UserLevel,
                            UserName,
                            UserSurname,
                            UserTitle
                        FROM TBL_Kullanici
                        WHERE [User] = @User
                          AND UserStatus = 0;";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@User", username);

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                string dbPassword = dr["UserPassword"]?.ToString() ?? string.Empty;

                                // ====================================
                                // 🔐 YENİ: HASH KONTROLÜ
                                // ====================================
                                bool isPasswordValid = false;

                                // Önce hash'li şifre mi kontrol et
                                if (dbPassword.StartsWith("$2"))
                                {
                                    // BCrypt hash'i ($ ile başlar)
                                    isPasswordValid = PasswordHasher.VerifyPassword(password, dbPassword);
                                }
                                else
                                {
                                    // Eski sistem: düz metin şifre
                                    // (Geçiş dönemi için - sonra kaldırılacak)
                                    isPasswordValid = (dbPassword == password);

                                    // ⚠️ Eğer düz metin şifre ile giriş yaptıysa,
                                    // şifreyi otomatik hash'le ve güncelle
                                    if (isPasswordValid)
                                    {
                                        string hashedPassword = PasswordHasher.HashPassword(password);

                                        // Veritabanını güncelle
                                        using (SqlConnection connUpdate = new SqlConnection(connStr))
                                        {
                                            connUpdate.Open();
                                            string updateSql = @"
                                                UPDATE TBL_Kullanici 
                                                SET UserPassword = @HashedPassword 
                                                WHERE [User] = @User";

                                            using (SqlCommand cmdUpdate = new SqlCommand(updateSql, connUpdate))
                                            {
                                                cmdUpdate.Parameters.AddWithValue("@HashedPassword", hashedPassword);
                                                cmdUpdate.Parameters.AddWithValue("@User", username);
                                                await cmdUpdate.ExecuteNonQueryAsync();
                                            }
                                        }
                                    }
                                }

                                if (isPasswordValid)
                                {
                                    string userCode = dr["User"]?.ToString() ?? username;
                                    int userLevel = 0;

                                    if (dr["UserLevel"] != DBNull.Value)
                                        userLevel = Convert.ToInt32(dr["UserLevel"]);

                                    string userName = dr["UserName"]?.ToString() ?? "";
                                    string userSurname = dr["UserSurname"]?.ToString() ?? "";
                                    string userTitle = dr["UserTitle"]?.ToString() ?? "";

                                    string displayName = (userName + " " + userSurname).Trim();

                                    // 🔹 SESSION DEĞERLERİ
                                    HttpContext.Session.SetString("User", userCode);
                                    HttpContext.Session.SetInt32("UserLevel", userLevel);
                                    HttpContext.Session.SetString("UserDisplayName", displayName);
                                    HttpContext.Session.SetString("UserTitle", userTitle);

                                    return RedirectToAction("Index", "Home");
                                }
                                else
                                {
                                    TempData["LoginError"] = "Kullanıcı adı veya şifre hatalı.";
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                            else
                            {
                                TempData["LoginError"] = "Kullanıcı bulunamadı veya hesap pasif durumda.";
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["LoginError"] = "Giriş yapılırken bir hata oluştu. Lütfen sistem yöneticinize başvurun.";
                // Gerçek üretim ortamında: _logger.LogError(ex, "Login hatası");
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}