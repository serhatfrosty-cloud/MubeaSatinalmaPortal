using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MubeaSatinalmaPortal.Models;
using MubeaSatinalmaPortal.Filters;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using DevExpress.XtraReports.UI;
using MubeaSatinalmaPortal.Reports;
using System.IO;

namespace MubeaSatinalmaPortal.Controllers
{
    public class RaporController : Controller
    {
        private readonly IConfiguration _config;

        public RaporController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [SessionAuthorize(MinLevel = 1)]  // ← YENİ: Raporları sadece yöneticiler görebilir
        public IActionResult Index(DateTime? baslangic, DateTime? bitis)
        {
            //// Login kontrolü
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Raporları görüntülemek için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            var liste = new List<RaporTalepViewModel>();

            ViewBag.Baslangic = baslangic;
            ViewBag.Bitis = bitis;

            // İlk açılışta tarih seçilmemişse sadece ekranı göster, veri getirme
            if (!baslangic.HasValue || !bitis.HasValue)
            {
                ViewBag.InfoMessage = "Lütfen bir başlangıç ve bitiş tarihi seçip 'Listele' butonuna basınız.";
                return View(liste);
            }

            // Bitiş tarihini gün sonuna çek (23:59:59)
            DateTime bas = baslangic.Value.Date;
            DateTime son = bitis.Value.Date.AddDays(1).AddSeconds(-1);

            string connStr = _config.GetConnectionString("MubeaDB");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sql = @"
                    SELECT 
                        -- HEADER
                        H.TalepID,
                        H.TalepNo,
                        H.CreatedDate,
                        H.CreatedByUser,
                        H.TalepStatus,
                        D.DepartmanKodu,
                        D.DepartmanAciklamasi,
                        C.CostCenterCode,
                        C.CostCenterDescription,
                        HT.HizmetKodu,
                        HT.HizmetAciklamasi,
                        S.TalepStatus AS StatusKisa,
                        S.TalepStatusAciklamasi,

                        -- DETAILS
                        DT.TalepDetailsRows,
                        DT.TalepAdet,
                        DT.TalepBirim,
                        DT.TalepMalzeme,
                        DT.TalepGerekce,
                        DT.TavsiyeTedarikci,
                        DT.TalepFiyat,
                        DT.FiyatParaBirimi,
                        DT.TedarikciUrunKodu,
                        DT.TalepTipi,
                        DT.TalepTermin,
                        DT.TalepEdenKisi
                    FROM TBL_SatinAlmaTalepHeader H
                    LEFT JOIN TBL_Departman D
                        ON H.DepartmanID = D.DepartmanID
                    LEFT JOIN TBL_CostCenter C
                        ON H.CostCenterID = C.CostCenterID
                    LEFT JOIN TBL_HizmetTipi HT
                        ON H.HizmetID = HT.HizmetID
                    LEFT JOIN TBL_TalepOnayAciklama S
                        ON H.TalepStatus = S.TalepStatusId
                    LEFT JOIN TBL_SatinAlmaTalepDetails DT
                        ON H.TalepID = DT.TalepID
                        AND DT.TalepSatirIsDeleted = 0
                    WHERE 
                        H.TalepIsDeleted = 0
                        AND H.CreatedDate BETWEEN @Baslangic AND @Bitis
                    ORDER BY 
                        H.CreatedDate DESC,
                        DT.TalepDetailsRows ASC;";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Baslangic", bas);
                    cmd.Parameters.AddWithValue("@Bitis", son);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // Aynı TalepID için gruplama yapacağız
                        var dict = new Dictionary<int, RaporTalepViewModel>();

                        while (dr.Read())
                        {
                            int talepId = Convert.ToInt32(dr["TalepID"]);

                            if (!dict.TryGetValue(talepId, out var talep))
                            {
                                talep = new RaporTalepViewModel
                                {
                                    TalepID = talepId,
                                    TalepNo = dr["TalepNo"].ToString(),
                                    CreatedDate = Convert.ToDateTime(dr["CreatedDate"]),
                                    CreatedByUser = dr["CreatedByUser"]?.ToString(),

                                    TalepStatusId = dr["TalepStatus"] != DBNull.Value ? Convert.ToInt32(dr["TalepStatus"]) : 0,
                                    TalepStatusKisa = dr["StatusKisa"]?.ToString(),
                                    TalepStatusAciklama = dr["TalepStatusAciklamasi"]?.ToString(),

                                    DepartmanKodu = dr["DepartmanKodu"]?.ToString(),
                                    DepartmanAciklama = dr["DepartmanAciklamasi"]?.ToString(),
                                    CostCenterCode = dr["CostCenterCode"]?.ToString(),
                                    CostCenterDescription = dr["CostCenterDescription"]?.ToString(),
                                    HizmetKodu = dr["HizmetKodu"]?.ToString(),
                                    HizmetAciklamasi = dr["HizmetAciklamasi"]?.ToString()
                                };

                                dict.Add(talepId, talep);
                            }

                            // Detay satırı varsa ekleyelim
                            if (dr["TalepDetailsRows"] != DBNull.Value)
                            {
                                var satir = new RaporTalepSatirViewModel
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

                                talep.Satirlar.Add(satir);
                            }
                        }

                        liste.AddRange(dict.Values);
                    }
                }
            }

            if (liste.Count == 0)
            {
                ViewBag.InfoMessage = "Seçilen tarih aralığında herhangi bir talep bulunamadı.";
            }

            return View(liste);
        }

        // =========================================================
        // 2) TEK TALEBE AİT RAPOR DTO'SU (DEVEXPRESS İÇİN)
        // =========================================================

        /// <summary>
        /// Tek bir talebe ait rapor DTO'sunu DB'den okur.
        /// HEADER + SATIRLAR doldurulur.
        /// DevExpress XtraReport bu DTO'yu DataSource olarak kullanacak.
        /// </summary>
        private SatinAlmaTalepRaporDto RaporVerisiniGetir(int talepId)
        {
            var dto = new SatinAlmaTalepRaporDto();

            string connStr = _config.GetConnectionString("MubeaDB");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 1) HEADER BİLGİLERİ
                string headerSql = @"
                    SELECT 
                        H.TalepID,
                        H.TalepNo,
                        H.CreatedDate,
                        H.CreatedByUser,

                        D.DepartmanKodu,
                        D.DepartmanAciklamasi,

                        C.CostCenterCode,
                        C.CostCenterDescription,

                        S.TalepStatus AS DurumKisa,
                        S.TalepStatusAciklamasi AS DurumAciklama
                    FROM TBL_SatinAlmaTalepHeader H
                    LEFT JOIN TBL_Departman D
                        ON H.DepartmanID = D.DepartmanID
                    LEFT JOIN TBL_CostCenter C
                        ON H.CostCenterID = C.CostCenterID
                    LEFT JOIN TBL_TalepOnayAciklama S
                        ON H.TalepStatus = S.TalepStatusId
                    WHERE 
                        H.TalepID = @TalepID
                        AND H.TalepIsDeleted = 0;";

                using (SqlCommand cmd = new SqlCommand(headerSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", talepId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            dto.TalepID = Convert.ToInt32(dr["TalepID"]);
                            dto.TalepNo = dr["TalepNo"].ToString();
                            dto.Tarih = Convert.ToDateTime(dr["CreatedDate"]);
                            dto.OlusturanKullanici = dr["CreatedByUser"]?.ToString();

                            string depKod = dr["DepartmanKodu"]?.ToString();
                            string depAck = dr["DepartmanAciklamasi"]?.ToString();
                            dto.Departman = $"{depKod} - {depAck}".Trim(' ', '-');

                            string ccKod = dr["CostCenterCode"]?.ToString();
                            string ccAck = dr["CostCenterDescription"]?.ToString();
                            dto.MasrafMerkezi = $"{ccKod} - {ccAck}".Trim(' ', '-');

                            dto.DurumKisa = dr["DurumKisa"]?.ToString();
                            dto.DurumAciklama = dr["DurumAciklama"]?.ToString();
                        }
                    }
                }

                // Header hiç bulunamadıysa null dönelim
                if (dto.TalepID == 0)
                    return null;

                // 2) SATIRLAR
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
                    WHERE 
                        TalepID = @TalepID
                        AND TalepSatirIsDeleted = 0
                    ORDER BY TalepDetailsRows;";

                using (SqlCommand cmd = new SqlCommand(detailsSql, conn))
                {
                    cmd.Parameters.AddWithValue("@TalepID", talepId);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var satir = new SatinAlmaTalepSatirDto();

                            satir.SatirNo = Convert.ToInt32(dr["TalepDetailsRows"]);

                            if (dr["TalepAdet"] != DBNull.Value)
                                satir.TalepAdet = Convert.ToDecimal(dr["TalepAdet"], CultureInfo.InvariantCulture);

                            satir.Birim = dr["TalepBirim"]?.ToString();
                            satir.MalzemeTanimi = dr["TalepMalzeme"]?.ToString();
                            satir.Gerekce = dr["TalepGerekce"]?.ToString();
                            satir.TavsiyeTedarikci = dr["TavsiyeTedarikci"]?.ToString();

                            if (dr["TalepFiyat"] != DBNull.Value)
                                satir.Fiyat = Convert.ToDecimal(dr["TalepFiyat"], CultureInfo.InvariantCulture);

                            satir.ParaBirimi = dr["FiyatParaBirimi"]?.ToString();
                            satir.TedarikciUrunKodu = dr["TedarikciUrunKodu"]?.ToString();
                            satir.TalepTipi = dr["TalepTipi"]?.ToString();

                            if (dr["TalepTermin"] != DBNull.Value)
                                satir.IhtiyacTermini = Convert.ToDateTime(dr["TalepTermin"]);

                            satir.TalepEden = dr["TalepEdenKisi"]?.ToString();

                            dto.Satirlar.Add(satir);
                        }
                    }
                }
            }

            return dto;
        }

        // =========================================================
        // TEK TALEP İÇİN PDF ÖNİZLEME (HEADER DOLDURMA)
        // =========================================================
        [HttpGet]
        [SessionAuthorize]  // ← YENİ
        public IActionResult TalepFormPdf(int id)
        {
            //if (HttpContext.Session.GetString("User") == null)
            //{
            //    TempData["LoginError"] = "Raporu görüntülemek için önce giriş yapmalısınız.";
            //    return RedirectToAction("Index", "Home");
            //}

            // 1) DTO'dan veriyi çek
            var dto = RaporVerisiniGetir(id);
            if (dto == null)
                return NotFound("Talep bulunamadı veya silinmiş.");

            // 2) DevExpress rapor nesnesini oluştur
            var report = new SatinAlmaTalepReport();

            // 3) DTO'dan HEADER label'larını doldur
            report.LoadFromDto(dto);

            // 4) Dokümanı oluştur
            report.CreateDocument();

            // 5) PDF'e export et ve sadece ÖNİZLEME (inline) olarak göster
            using (var ms = new MemoryStream())
            {
                report.ExportToPdf(ms);
                ms.Position = 0;

                // Inline preview için Content-Disposition'ı 'inline' yapıyoruz
                Response.Headers["Content-Disposition"] =
                    $"inline; filename={dto.TalepNo}_SatinAlmaTalepFormu.pdf";

                return File(ms.ToArray(), "application/pdf");
            }
        }


    }
}
