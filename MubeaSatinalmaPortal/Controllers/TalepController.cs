using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MubeaSatinalmaPortal.Models;
using MubeaSatinalmaPortal.Filters;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System.Globalization;



namespace MubeaSatinalmaPortal.Controllers
{
    public class TalepController : Controller
    {
        private readonly IConfiguration _config;

        public TalepController(IConfiguration config)
        {
            _config = config;
        }

        // 🔹 Yeni Talep No üret (MP00000001, MP00000002, ...)
        // 🔹 Yeni Talep No üret (MP00000001, MP00000002, ...)
        private async Task<string> GenerateNewTalepNo()
        {
            string connStr = _config.GetConnectionString("MubeaDB");
            string lastTalepNo = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync(); // ← async

                string query = @"
            SELECT TOP 1 TalepNo
            FROM TBL_SatinAlmaTalepHeader
            WHERE TalepNo LIKE 'MP%'
            ORDER BY TalepID DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    object result = await cmd.ExecuteScalarAsync(); // ← async
                    if (result != null && result != DBNull.Value)
                    {
                        lastTalepNo = result.ToString();
                    }
                }
            }

            int newNumber = 1;

            if (!string.IsNullOrEmpty(lastTalepNo) && lastTalepNo.Length > 2)
            {
                string numericPart = lastTalepNo.Substring(2);
                if (int.TryParse(numericPart, out int n))
                    newNumber = n + 1;
            }

            return "MP" + newNumber.ToString("D8");
        }

        // 🔹 Combobox verilerini dolduran yardımcı metot
        // 🔹 Combobox verilerini dolduran yardımcı metot
        private async Task LoadDropDowns()
        {
            string connStr = _config.GetConnectionString("MubeaDB");

            var departmanList = new List<SelectListItem>();
            var costCenterList = new List<SelectListItem>();
            var hizmetTipiList = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync(); // ← async

                // Departman
                string departmanQuery = @"
            SELECT DepartmanID, DepartmanKodu, DepartmanAciklamasi
            FROM TBL_Departman
            WHERE DepartmanStatus = 0
            ORDER BY DepartmanKodu";

                using (SqlCommand cmd = new SqlCommand(departmanQuery, conn))
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync()) // ← async
                {
                    while (await dr.ReadAsync()) // ← async
                    {
                        departmanList.Add(new SelectListItem
                        {
                            Value = dr["DepartmanID"].ToString(),
                            Text = $"{dr["DepartmanKodu"]} - {dr["DepartmanAciklamasi"]}"
                        });
                    }
                }

                // CostCenter
                string costCenterQuery = @"
            SELECT CostCenterID, CostCenterCode, CostCenterDescription
            FROM TBL_CostCenter
            WHERE CostCenterStatus = 0
            ORDER BY CostCenterCode";

                using (SqlCommand cmd = new SqlCommand(costCenterQuery, conn))
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync()) // ← async
                {
                    while (await dr.ReadAsync()) // ← async
                    {
                        costCenterList.Add(new SelectListItem
                        {
                            Value = dr["CostCenterID"].ToString(),
                            Text = $"{dr["CostCenterCode"]} - {dr["CostCenterDescription"]}"
                        });
                    }
                }

                // Hizmet Tipi
                string hizmetQuery = @"
            SELECT HizmetID, HizmetKodu, HizmetAciklamasi
            FROM TBL_HizmetTipi
            WHERE HizmetStatus = 0
            ORDER BY HizmetKodu";

                using (SqlCommand cmd = new SqlCommand(hizmetQuery, conn))
                using (SqlDataReader dr = await cmd.ExecuteReaderAsync()) // ← async
                {
                    while (await dr.ReadAsync()) // ← async
                    {
                        hizmetTipiList.Add(new SelectListItem
                        {
                            Value = dr["HizmetID"].ToString(),
                            Text = $"{dr["HizmetKodu"]} - {dr["HizmetAciklamasi"]}"
                        });
                    }
                }
            }

            ViewBag.DepartmanList = departmanList;
            ViewBag.CostCenterList = costCenterList;
            ViewBag.HizmetTipiList = hizmetTipiList;
        }

        // 🔹 GET: /Talep/Create
        [HttpGet]
        [SessionAuthorize]  // ← YENİ: Sadece bu satırı ekledik!
        public async Task<IActionResult> Create() // ← async Task<IActionResult>
        {
            // ❌ Artık bu kodlara gerek yok - silebiliriz:
            // if (HttpContext.Session.GetString("User") == null)
            // {
            //     TempData["LoginError"] = "Talep oluşturmak için önce giriş yapmalısınız.";
            //     return RedirectToAction("Index", "Home");
            // }

            ViewBag.TalepNo = await GenerateNewTalepNo(); // ← await
            await LoadDropDowns(); // ← await
            return View();
        }

        // 🔹 POST: /Talep/Create
        [HttpPost]
        [SessionAuthorize]
        public async Task<IActionResult> Create(string TalepNo, int DepartmanID, int CostCenterID, int HizmetID)
        {
            if (DepartmanID == 0 || CostCenterID == 0 || HizmetID == 0)
            {
                ViewBag.Error = "Departman, Masraf Merkezi ve Hizmet Tipi seçilmelidir.";
                ViewBag.TalepNo = string.IsNullOrEmpty(TalepNo) ? await GenerateNewTalepNo() : TalepNo;
                await LoadDropDowns();
                return View();
            }

            string connStr = _config.GetConnectionString("MubeaDB");
            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync(); // ← async
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    // 1) HEADER INSERT
                    int newTalepId;

                    string insertHeaderSql = @"
                INSERT INTO TBL_SatinAlmaTalepHeader
                    (TalepNo, DepartmanID, CostCenterID, HizmetID, CreatedByUser, TalepStatus, TalepIsDeleted, CreatedDate, TalepStatusLastUpdate)
                VALUES
                    (@TalepNo, @DepartmanID, @CostCenterID, @HizmetID, @CreatedByUser, 0, 0, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd = new SqlCommand(insertHeaderSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@TalepNo", TalepNo);
                        cmd.Parameters.AddWithValue("@DepartmanID", DepartmanID);
                        cmd.Parameters.AddWithValue("@CostCenterID", CostCenterID);
                        cmd.Parameters.AddWithValue("@HizmetID", HizmetID);
                        cmd.Parameters.AddWithValue("@CreatedByUser", currentUser);

                        newTalepId = (int)await cmd.ExecuteScalarAsync(); // ← async
                    }

                    // 2) DETAILS INSERT
                    var adetList = Request.Form["TalepAdet"];
                    var birimList = Request.Form["TalepBirim"];
                    var malzList = Request.Form["TalepMalzeme"];
                    var gerekceList = Request.Form["TalepGerekce"];
                    var tedarikciList = Request.Form["TavsiyeTedarikci"];
                    var fiyatList = Request.Form["TalepFiyat"];
                    var pbList = Request.Form["FiyatParaBirimi"];
                    var urunKodList = Request.Form["TedarikciUrunKodu"];
                    var tipList = Request.Form["TalepTipi"];
                    var terminList = Request.Form["TalepTermin"];
                    var edenList = Request.Form["TalepEdenKisi"];

                    int rowCount = adetList.Count;

                    for (int i = 0; i < rowCount; i++)
                    {
                        bool isAllEmpty =
                            string.IsNullOrWhiteSpace(adetList[i]) &&
                            string.IsNullOrWhiteSpace(malzList[i]) &&
                            string.IsNullOrWhiteSpace(gerekceList[i]) &&
                            string.IsNullOrWhiteSpace(tedarikciList[i]) &&
                            string.IsNullOrWhiteSpace(fiyatList[i]);

                        if (isAllEmpty)
                            continue;

                        decimal? adet = null;
                        if (decimal.TryParse(adetList[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var adetDec) ||
                            decimal.TryParse(adetList[i], NumberStyles.Any, CultureInfo.CurrentCulture, out adetDec))
                        {
                            adet = adetDec;
                        }

                        decimal? fiyat = null;
                        if (decimal.TryParse(fiyatList[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var fiyatDec) ||
                            decimal.TryParse(fiyatList[i], NumberStyles.Any, CultureInfo.CurrentCulture, out fiyatDec))
                        {
                            fiyat = fiyatDec;
                        }

                        DateTime? termin = null;
                        if (DateTime.TryParse(terminList[i], out var dt))
                        {
                            termin = dt;
                        }

                        string insertDetailSql = @"
                    INSERT INTO TBL_SatinAlmaTalepDetails
                    (
                        TalepID,
                        TalepDetailsRows,
                        TalepAdet,
                        TalepBirim,
                        TalepMalzeme,
                        TalepGerekce,
                        TavsiyeTedarikci,
                        TalepFiyat,
                        FiyatParaBirimi,
                        TedarikciUrunKodu,
                        TalepTipi,
                        TalepTermin,
                        TalepEdenKisi,
                        TalepSatirIsDeleted
                    )
                    VALUES
                    (
                        @TalepID,
                        @TalepDetailsRows,
                        @TalepAdet,
                        @TalepBirim,
                        @TalepMalzeme,
                        @TalepGerekce,
                        @TavsiyeTedarikci,
                        @TalepFiyat,
                        @FiyatParaBirimi,
                        @TedarikciUrunKodu,
                        @TalepTipi,
                        @TalepTermin,
                        @TalepEdenKisi,
                        0
                    );";

                        using (SqlCommand cmd = new SqlCommand(insertDetailSql, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@TalepID", newTalepId);
                            cmd.Parameters.AddWithValue("@TalepDetailsRows", i + 1);

                            cmd.Parameters.AddWithValue("@TalepAdet", (object?)adet ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TalepBirim", (object?)(birimList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TalepMalzeme", (object?)(malzList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TalepGerekce", (object?)(gerekceList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TavsiyeTedarikci", (object?)(tedarikciList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TalepFiyat", (object?)fiyat ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FiyatParaBirimi", (object?)(pbList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TedarikciUrunKodu", (object?)(urunKodList[i]?.ToUpper()) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TalepTipi", (object?)(tipList[i]?.ToUpper()) ?? DBNull.Value);

                            if (termin.HasValue)
                                cmd.Parameters.AddWithValue("@TalepTermin", termin.Value);
                            else
                                cmd.Parameters.AddWithValue("@TalepTermin", DBNull.Value);

                            cmd.Parameters.AddWithValue("@TalepEdenKisi", (object?)(edenList[i]?.ToUpper()) ?? DBNull.Value);

                            await cmd.ExecuteNonQueryAsync(); // ← async
                        }
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["SaveError"] = "Talep kaydedilirken bir hata oluştu. Lütfen tekrar deneyin.";

                    ViewBag.TalepNo = string.IsNullOrEmpty(TalepNo) ? await GenerateNewTalepNo() : TalepNo;
                    await LoadDropDowns();
                    return View();
                }
            }

            TempData["SaveSuccess"] = $"Talebiniz başarıyla oluşturuldu. Talep No: {TalepNo}";
            return RedirectToAction("Index", "Home");
        }

        // 🔹 GET: /Talep/Index  -> Kullanıcının kendi oluşturduğu talepler
        [HttpGet]
        [SessionAuthorize]
        public async Task<IActionResult> Index() // ← async Task<IActionResult>
        {
            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";
            string connStr = _config.GetConnectionString("MubeaDB");

            var talepList = new List<TalepListItemViewModel>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync(); // ← async

                string query = @"
            SELECT 
                H.TalepID,
                H.TalepNo,
                H.CreatedDate,
                D.DepartmanKodu,
                C.CostCenterCode,
                HT.HizmetKodu,
                S.TalepStatus,
                S.TalepStatusAciklamasi
            FROM TBL_SatinAlmaTalepHeader H
            LEFT JOIN TBL_Departman D
                ON H.DepartmanID = D.DepartmanID
            LEFT JOIN TBL_CostCenter C
                ON H.CostCenterID = C.CostCenterID
            LEFT JOIN TBL_HizmetTipi HT
                ON H.HizmetID = HT.HizmetID
            LEFT JOIN TBL_TalepOnayAciklama S
                ON H.TalepStatus = S.TalepStatusId
            WHERE 
                H.TalepIsDeleted = 0
                AND H.CreatedByUser = @CreatedByUser
            ORDER BY H.CreatedDate DESC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CreatedByUser", currentUser);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync()) // ← async
                    {
                        while (await dr.ReadAsync()) // ← async
                        {
                            talepList.Add(new TalepListItemViewModel
                            {
                                TalepID = Convert.ToInt32(dr["TalepID"]),
                                TalepNo = dr["TalepNo"].ToString(),
                                CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                                Departman = dr["DepartmanKodu"]?.ToString(),
                                CostCenter = dr["CostCenterCode"]?.ToString(),
                                HizmetTipi = dr["HizmetKodu"]?.ToString(),
                                TalepStatusKisa = dr["TalepStatus"]?.ToString(),
                                TalepStatusAciklama = dr["TalepStatusAciklamasi"]?.ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.CurrentUser = currentUser;
            return View(talepList);
        }

        // 🔹 GET: /Talep/OnayBekleyenler  -> Mevcut kullanıcı rolüne göre bekleyen onaylar
        [HttpGet]
        [SessionAuthorize(MinLevel = 1)]  // ← YENİ: En az Departman Yöneticisi olmalı!
                                          // ✅ YENİ:
        public async Task<IActionResult> OnayBekleyenler()
        {
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Bekleyen onayları görmek için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";
            string connStr = _config.GetConnectionString("MubeaDB");

            int userLevel = 0;
            int? kullaniciDepartmanId = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string userQuery = @"
                    SELECT UserLevel, DepartmanID
                    FROM TBL_Kullanici
                    WHERE [User] = @User
                      AND UserStatus = 0;";

                using (SqlCommand cmd = new SqlCommand(userQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@User", currentUser);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            if (dr["UserLevel"] != DBNull.Value)
                                userLevel = Convert.ToInt32(dr["UserLevel"]);

                            if (dr["DepartmanID"] != DBNull.Value)
                                kullaniciDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                        }
                        else
                        {
                            ViewBag.InfoMessage = "Kullanıcı kaydı bulunamadı veya pasif.";
                            return View(new List<TalepListItemViewModel>());
                        }
                    }
                }
            }

            if (userLevel < 1 || userLevel > 3)
            {
                ViewBag.InfoMessage = "Bu ekranda sadece onay yetkisine sahip kullanıcılar (yönetici, satın alma, genel müdür) bekleyen talepleri görebilir.";
                ViewBag.CurrentUser = currentUser;
                ViewBag.UserLevel = userLevel;
                return View(new List<TalepListItemViewModel>());
            }

            int targetStatus = -1;
            bool departmanFiltreGerekli = false;

            if (userLevel == 1)
            {
                targetStatus = 0;
                departmanFiltreGerekli = true;
            }
            else if (userLevel == 2)
            {
                targetStatus = 1;
            }
            else if (userLevel == 3)
            {
                targetStatus = 2;
            }

            var bekleyenList = new List<TalepListItemViewModel>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.OpenAsync();

                string query = @"
                    SELECT 
                        H.TalepID,
                        H.TalepNo,
                        H.CreatedDate,
                        D.DepartmanKodu,
                        C.CostCenterCode,
                        HT.HizmetKodu,
                        S.TalepStatus,
                        S.TalepStatusAciklamasi
                    FROM TBL_SatinAlmaTalepHeader H
                    LEFT JOIN TBL_Departman D
                        ON H.DepartmanID = D.DepartmanID
                    LEFT JOIN TBL_CostCenter C
                        ON H.CostCenterID = C.CostCenterID
                    LEFT JOIN TBL_HizmetTipi HT
                        ON H.HizmetID = HT.HizmetID
                    LEFT JOIN TBL_TalepOnayAciklama S
                        ON H.TalepStatus = S.TalepStatusId
                    WHERE 
                        H.TalepIsDeleted = 0
                        AND H.TalepStatus = @TargetStatus";

                if (departmanFiltreGerekli && kullaniciDepartmanId.HasValue)
                {
                    query += " AND H.DepartmanID = @DepartmanID";
                }

                query += " ORDER BY H.CreatedDate DESC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TargetStatus", targetStatus);

                    if (departmanFiltreGerekli && kullaniciDepartmanId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@DepartmanID", kullaniciDepartmanId.Value);
                    }

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            bekleyenList.Add(new TalepListItemViewModel
                            {
                                TalepID = Convert.ToInt32(dr["TalepID"]),
                                TalepNo = dr["TalepNo"].ToString(),
                                CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                                Departman = dr["DepartmanKodu"]?.ToString(),
                                CostCenter = dr["CostCenterCode"]?.ToString(),
                                HizmetTipi = dr["HizmetKodu"]?.ToString(),
                                TalepStatusKisa = dr["TalepStatus"]?.ToString(),
                                TalepStatusAciklama = dr["TalepStatusAciklamasi"]?.ToString()
                            });
                        }
                    }
                }
            }

            if (bekleyenList.Count == 0)
            {
                ViewBag.InfoMessage = "Şu anda üzerinde bekleyen herhangi bir onayınız bulunmamaktadır.";
            }

            ViewBag.CurrentUser = currentUser;
            ViewBag.UserLevel = userLevel;

            return View(bekleyenList);
        }

        // 🔹 GET: /Talep/Detay/5  -> Talep detay ekranı + ONAY/RED GEÇMİŞİ
        [HttpGet]
        [SessionAuthorize]  // ← EKLE
        public IActionResult Detay(int id)
        {
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Talep detayını görmek için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";
            int userLevel = HttpContext.Session.GetInt32("UserLevel") ?? 0;
            string connStr = _config.GetConnectionString("MubeaDB");

            TalepDetayViewModel model = null;
            int? talepDepartmanId = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // HEADER
                string headerSql = @"
                    SELECT 
                        H.TalepID,
                        H.TalepNo,
                        H.CreatedDate,
                        H.CreatedByUser,
                        H.TalepStatus,
                        H.DepartmanID,
                        D.DepartmanKodu,
                        D.DepartmanAciklamasi,
                        C.CostCenterCode,
                        C.CostCenterDescription,
                        HT.HizmetKodu,
                        HT.HizmetAciklamasi,
                        S.TalepStatus AS StatusKisa,
                        S.TalepStatusAciklamasi
                    FROM TBL_SatinAlmaTalepHeader H
                    LEFT JOIN TBL_Departman D
                        ON H.DepartmanID = D.DepartmanID
                    LEFT JOIN TBL_CostCenter C
                        ON H.CostCenterID = C.CostCenterID
                    LEFT JOIN TBL_HizmetTipi HT
                        ON H.HizmetID = HT.HizmetID
                    LEFT JOIN TBL_TalepOnayAciklama S
                        ON H.TalepStatus = S.TalepStatusId
                    WHERE H.TalepID = @TalepID
                      AND H.TalepIsDeleted = 0;";

                using (SqlCommand cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            talepDepartmanId = dr["DepartmanID"] != DBNull.Value
                                ? (int?)Convert.ToInt32(dr["DepartmanID"])
                                : null;

                            model = new TalepDetayViewModel
                            {
                                TalepID = Convert.ToInt32(dr["TalepID"]),
                                TalepNo = dr["TalepNo"].ToString(),
                                CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                                CreatedByUser = dr["CreatedByUser"]?.ToString(),
                                TalepStatusId = Convert.ToInt32(dr["TalepStatus"]),
                                DepartmanKodu = dr["DepartmanKodu"]?.ToString(),
                                DepartmanAciklama = dr["DepartmanAciklamasi"]?.ToString(),
                                CostCenterCode = dr["CostCenterCode"]?.ToString(),
                                CostCenterDescription = dr["CostCenterDescription"]?.ToString(),
                                HizmetKodu = dr["HizmetKodu"]?.ToString(),
                                HizmetAciklamasi = dr["HizmetAciklamasi"]?.ToString(),
                                TalepStatusKisa = dr["StatusKisa"]?.ToString(),
                                TalepStatusAciklama = dr["TalepStatusAciklamasi"]?.ToString()
                            };
                        }
                    }
                }

                if (model == null)
                {
                    TempData["SaveError"] = "Talep bulunamadı.";
                    return RedirectToAction("OnayBekleyenler");
                }

                // DETAY SATIRLARI
                string detailsSql = @"
                    SELECT 
                        TalepDetailsRows,
                        TalepAdet,
                        TalepBirim,
                        TalepMalzeme,
                        TalepGerekce,
                        TavsiyeTedarikci,
                        TalepFiyat,
                        FiyatParaBirimi,
                        TedarikciUrunKodu,
                        TalepTipi,
                        TalepTermin,
                        TalepEdenKisi
                    FROM TBL_SatinAlmaTalepDetails
                    WHERE TalepID = @TalepID
                      AND TalepSatirIsDeleted = 0
                    ORDER BY TalepDetailsRows;";

                using (SqlCommand cmd = new SqlCommand(detailsSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var satir = new TalepDetaySatirViewModel
                            {
                                TalepDetailsRows = Convert.ToInt32(dr["TalepDetailsRows"]),
                                TalepAdet = dr["TalepAdet"] != DBNull.Value ? (decimal?)Convert.ToDecimal(dr["TalepAdet"]) : null,
                                TalepBirim = dr["TalepBirim"]?.ToString(),
                                TalepMalzeme = dr["TalepMalzeme"]?.ToString(),
                                TalepGerekce = dr["TalepGerekce"]?.ToString(),
                                TavsiyeTedarikci = dr["TavsiyeTedarikci"]?.ToString(),
                                TalepFiyat = dr["TalepFiyat"] != DBNull.Value ? (decimal?)Convert.ToDecimal(dr["TalepFiyat"]) : null,
                                FiyatParaBirimi = dr["FiyatParaBirimi"]?.ToString(),
                                TedarikciUrunKodu = dr["TedarikciUrunKodu"]?.ToString(),
                                TalepTipi = dr["TalepTipi"]?.ToString(),
                                TalepTermin = dr["TalepTermin"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(dr["TalepTermin"]) : null,
                                TalepEdenKisi = dr["TalepEdenKisi"]?.ToString()
                            };

                            model.Satirlar.Add(satir);
                        }
                    }
                }

                // 🔹 ONAY / RED LOG'LARINI OKU
                string logSql = @"
                    SELECT 
                        L.TalepOnayLogID,
                        L.EskiStatusId,
                        L.YeniStatusId,
                        L.IslemTipi,
                        L.IslemYapanUser,
                        L.IslemYapanLevel,
                        L.OnayNotu,
                        L.IslemTarihi,
                        S1.TalepStatus AS EskiStatusText,
                        S2.TalepStatus AS YeniStatusText
                    FROM TBL_TalepOnayLog L
                    LEFT JOIN TBL_TalepOnayAciklama S1
                        ON L.EskiStatusId = S1.TalepStatusId
                    LEFT JOIN TBL_TalepOnayAciklama S2
                        ON L.YeniStatusId = S2.TalepStatusId
                    WHERE L.TalepID = @TalepID
                    ORDER BY L.TalepOnayLogID ASC;";

                using (SqlCommand cmd = new SqlCommand(logSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var log = new TalepOnayLogViewModel
                            {
                                TalepOnayLogID = Convert.ToInt32(dr["TalepOnayLogID"]),
                                EskiStatusId = dr["EskiStatusId"] != DBNull.Value ? Convert.ToInt32(dr["EskiStatusId"]) : 0,
                                YeniStatusId = dr["YeniStatusId"] != DBNull.Value ? Convert.ToInt32(dr["YeniStatusId"]) : 0,
                                IslemTipi = dr["IslemTipi"]?.ToString(),
                                IslemYapanUser = dr["IslemYapanUser"]?.ToString(),
                                IslemYapanLevel = dr["IslemYapanLevel"] != DBNull.Value ? Convert.ToInt32(dr["IslemYapanLevel"]) : 0,
                                OnayNotu = dr["OnayNotu"]?.ToString(),
                                IslemTarihi = dr["IslemTarihi"] != DBNull.Value ? Convert.ToDateTime(dr["IslemTarihi"]) : DateTime.MinValue,
                                EskiStatusText = dr["EskiStatusText"]?.ToString(),
                                YeniStatusText = dr["YeniStatusText"]?.ToString()
                            };

                            model.OnayLoglari.Add(log);
                        }
                    }
                }

                // Kullanıcının departman bilgisi (özellikle userLevel=1 için)
                int? kullaniciDepartmanId = null;

                string userSql = @"
                    SELECT DepartmanID, UserLevel
                    FROM TBL_Kullanici
                    WHERE [User] = @User
                      AND UserStatus = 0;";

                using (SqlCommand cmd = new SqlCommand(userSql, conn))
                {
                    cmd.Parameters.AddWithValue("@User", currentUser);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            if (dr["DepartmanID"] != DBNull.Value)
                                kullaniciDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                        }
                    }
                }

                bool canApprove = (userLevel == 1 && model.TalepStatusId == 0 && talepDepartmanId.HasValue && kullaniciDepartmanId.HasValue && talepDepartmanId.Value == kullaniciDepartmanId.Value) ||
                                  (userLevel == 2 && model.TalepStatusId == 1) ||
                                  (userLevel == 3 && model.TalepStatusId == 2);

                model.KullaniciOnayVerebilirMi = canApprove;
            }

            return View(model);
        }

        // 🔹 POST: /Talep/Onayla/5
        [HttpPost]
        [SessionAuthorize(MinLevel = 1)]  // ← EKLE: En az Yönetici
        public IActionResult Onayla(int id, string onayNotu)
        {
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Onay işlemi için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";
            int userLevel = HttpContext.Session.GetInt32("UserLevel") ?? 0;
            string connStr = _config.GetConnectionString("MubeaDB");

            int eskiStatus = -1;
            int yeniStatus = -1;
            int? talepDepartmanId = null;
            int? kullaniciDepartmanId = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Talebin mevcut durumunu oku
                string headerSql = @"
                    SELECT TalepStatus, DepartmanID
                    FROM TBL_SatinAlmaTalepHeader
                    WHERE TalepID = @TalepID
                      AND TalepIsDeleted = 0;";

                using (SqlCommand cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            eskiStatus = Convert.ToInt32(dr["TalepStatus"]);
                            if (dr["DepartmanID"] != DBNull.Value)
                                talepDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                        }
                        else
                        {
                            TempData["SaveError"] = "Talep bulunamadı.";
                            return RedirectToAction("OnayBekleyenler");
                        }
                    }
                }

                // Kullanıcının departmanını oku
                string userSql = @"
                    SELECT DepartmanID
                    FROM TBL_Kullanici
                    WHERE [User] = @User
                      AND UserStatus = 0;";

                using (SqlCommand cmd = new SqlCommand(userSql, conn))
                {
                    cmd.Parameters.AddWithValue("@User", currentUser);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read() && dr["DepartmanID"] != DBNull.Value)
                            kullaniciDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                    }
                }

                // Rol + mevcut status'e göre yeni status belirle
                if (userLevel == 1 && eskiStatus == 0 && talepDepartmanId.HasValue && kullaniciDepartmanId.HasValue && talepDepartmanId.Value == kullaniciDepartmanId.Value)
                {
                    yeniStatus = 1; // Departman Yöneticisi -> Satın Alma
                }
                else if (userLevel == 2 && eskiStatus == 1)
                {
                    yeniStatus = 2; // Satın Alma -> Genel Müdür
                }
                else if (userLevel == 3 && eskiStatus == 2)
                {
                    yeniStatus = 3; // Genel Müdür -> Onaylandı
                }
                else
                {
                    TempData["SaveError"] = "Bu talep üzerinde onay yetkiniz bulunmuyor veya talep bu aşamada değil.";
                    return RedirectToAction("Detay", new { id = id });
                }

                // Transaction ile status güncelle + log
                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    // Header güncelle
                    string updateSql = @"
                        UPDATE TBL_SatinAlmaTalepHeader
                        SET TalepStatus = @YeniStatus,
                            TalepStatusLastUpdate = GETDATE()
                        WHERE TalepID = @TalepID;";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@YeniStatus", yeniStatus);
                        cmd.Parameters.AddWithValue("@TalepID", id);
                        cmd.ExecuteNonQuery();
                    }

                    // Log ekle
                    string insertLogSql = @"
                        INSERT INTO TBL_TalepOnayLog
                        (
                            TalepID,
                            EskiStatusId,
                            YeniStatusId,
                            IslemTipi,
                            IslemYapanUser,
                            IslemYapanLevel,
                            OnayNotu
                        )
                        VALUES
                        (
                            @TalepID,
                            @EskiStatusId,
                            @YeniStatusId,
                            @IslemTipi,
                            @IslemYapanUser,
                            @IslemYapanLevel,
                            @OnayNotu
                        );";

                    using (SqlCommand cmd = new SqlCommand(insertLogSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@TalepID", id);
                        cmd.Parameters.AddWithValue("@EskiStatusId", eskiStatus);
                        cmd.Parameters.AddWithValue("@YeniStatusId", yeniStatus);
                        cmd.Parameters.AddWithValue("@IslemTipi", "ONAY");
                        cmd.Parameters.AddWithValue("@IslemYapanUser", currentUser);
                        cmd.Parameters.AddWithValue("@IslemYapanLevel", userLevel);
                        cmd.Parameters.AddWithValue("@OnayNotu", (object?)onayNotu ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["SaveError"] = "Onay işlemi sırasında bir hata oluştu: " + ex.Message;
                    return RedirectToAction("Detay", new { id = id });
                }
            }

            TempData["SaveSuccess"] = "Talep başarıyla onaylandı.";
            return RedirectToAction("OnayBekleyenler");
        }

        // 🔹 POST: /Talep/Reddet/5
        [HttpPost]
        [SessionAuthorize(MinLevel = 1)]  // ← EKLE: En az Yönetici
        public IActionResult Reddet(int id, string onayNotu)
        {
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Red işlemi için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            string currentUser = HttpContext.Session.GetString("User") ?? "UNKNOWN";
            int userLevel = HttpContext.Session.GetInt32("UserLevel") ?? 0;
            string connStr = _config.GetConnectionString("MubeaDB");

            int eskiStatus = -1;
            int yeniStatus = 4; // REDDEDILDI
            int? talepDepartmanId = null;
            int? kullaniciDepartmanId = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string headerSql = @"
                    SELECT TalepStatus, DepartmanID
                    FROM TBL_SatinAlmaTalepHeader
                    WHERE TalepID = @TalepID
                      AND TalepIsDeleted = 0;";

                using (SqlCommand cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", id);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            eskiStatus = Convert.ToInt32(dr["TalepStatus"]);
                            if (dr["DepartmanID"] != DBNull.Value)
                                talepDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                        }
                        else
                        {
                            TempData["SaveError"] = "Talep bulunamadı.";
                            return RedirectToAction("OnayBekleyenler");
                        }
                    }
                }

                // Kullanıcının departmanını oku
                string userSql = @"
                    SELECT DepartmanID
                    FROM TBL_Kullanici
                    WHERE [User] = @User
                      AND UserStatus = 0;";

                using (SqlCommand cmd = new SqlCommand(userSql, conn))
                {
                    cmd.Parameters.AddWithValue("@User", currentUser);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read() && dr["DepartmanID"] != DBNull.Value)
                            kullaniciDepartmanId = Convert.ToInt32(dr["DepartmanID"]);
                    }
                }

                bool yetkiVar =
                    (userLevel == 1 && eskiStatus == 0 && talepDepartmanId.HasValue && kullaniciDepartmanId.HasValue && talepDepartmanId.Value == kullaniciDepartmanId.Value) ||
                    (userLevel == 2 && eskiStatus == 1) ||
                    (userLevel == 3 && eskiStatus == 2);

                if (!yetkiVar)
                {
                    TempData["SaveError"] = "Bu talep üzerinde red yetkiniz bulunmuyor veya talep bu aşamada değil.";
                    return RedirectToAction("Detay", new { id = id });
                }

                SqlTransaction tran = conn.BeginTransaction();

                try
                {
                    string updateSql = @"
                        UPDATE TBL_SatinAlmaTalepHeader
                        SET TalepStatus = @YeniStatus,
                            TalepStatusLastUpdate = GETDATE()
                        WHERE TalepID = @TalepID;";

                    using (SqlCommand cmd = new SqlCommand(updateSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@YeniStatus", yeniStatus);
                        cmd.Parameters.AddWithValue("@TalepID", id);
                        cmd.ExecuteNonQuery();
                    }

                    string insertLogSql = @"
                        INSERT INTO TBL_TalepOnayLog
                        (
                            TalepID,
                            EskiStatusId,
                            YeniStatusId,
                            IslemTipi,
                            IslemYapanUser,
                            IslemYapanLevel,
                            OnayNotu
                        )
                        VALUES
                        (
                            @TalepID,
                            @EskiStatusId,
                            @YeniStatusId,
                            @IslemTipi,
                            @IslemYapanUser,
                            @IslemYapanLevel,
                            @OnayNotu
                        );";

                    using (SqlCommand cmd = new SqlCommand(insertLogSql, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@TalepID", id);
                        cmd.Parameters.AddWithValue("@EskiStatusId", eskiStatus);
                        cmd.Parameters.AddWithValue("@YeniStatusId", yeniStatus);
                        cmd.Parameters.AddWithValue("@IslemTipi", "RED");
                        cmd.Parameters.AddWithValue("@IslemYapanUser", currentUser);
                        cmd.Parameters.AddWithValue("@IslemYapanLevel", userLevel);
                        cmd.Parameters.AddWithValue("@OnayNotu", (object?)onayNotu ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    TempData["SaveError"] = "Red işlemi sırasında bir hata oluştu: " + ex.Message;
                    return RedirectToAction("Detay", new { id = id });
                }
            }

            TempData["SaveSuccess"] = "Talep reddedildi.";
            return RedirectToAction("OnayBekleyenler");
        }
    }
}
