using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;

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
        // Home/Index.cshtml içindeki login formu buraya post ediyor:
        // <form asp-controller="Account" asp-action="Login" method="post">
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Boş değer kontrolü
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["LoginError"] = "Kullanıcı veya şifre boş olamaz.";
                return RedirectToAction("Index", "Home");
            }

            string connStr = _config.GetConnectionString("MubeaDB");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

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
                          AND UserStatus = 0;    -- 0 = aktif kullanıcı";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@User", username);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                string dbPassword = dr["UserPassword"]?.ToString() ?? string.Empty;

                                // Şimdilik düz şifre ile kontrol (ileride hash'e dönebiliriz)
                                if (dbPassword == password)
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
                                    HttpContext.Session.SetString("User", userCode);                 // Örn: SERHAT.AYAZ
                                    HttpContext.Session.SetInt32("UserLevel", userLevel);           // 0,1,2,3...
                                    HttpContext.Session.SetString("UserDisplayName", displayName);  // Örn: Serhat Ayaz
                                    HttpContext.Session.SetString("UserTitle", userTitle);          // Örn: IT Manager

                                    return RedirectToAction("Index", "Home");
                                }
                                else
                                {
                                    TempData["LoginError"] = "Kullanıcı veya şifre hatalı.";
                                    return RedirectToAction("Index", "Home");
                                }
                            }
                            else
                            {
                                TempData["LoginError"] = "Kullanıcı bulunamadı veya pasif durumda.";
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["LoginError"] = "Giriş yapılırken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/Logout
        // Navbar'daki "Çıkış Yap" buraya geliyor
        public IActionResult Logout()
        {
            // Tüm session değerlerini temizle
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }
    }
}
