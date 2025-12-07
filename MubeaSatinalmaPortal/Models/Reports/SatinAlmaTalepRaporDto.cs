using System;
using System.Collections.Generic;

namespace MubeaSatinalmaPortal.Models
{
    /// <summary>
    /// Tek bir satın alma talebinin rapor için tüm bilgilerini tutar.
    /// Header + satırlar (ve istersen ileride onay bilgileri).
    /// DevExpress XtraReport bu DTO'yu DataSource olarak kullanacak.
    /// </summary>
    public class SatinAlmaTalepRaporDto
    {
        // --------- HEADER ALANLARI ---------

        /// <summary>
        /// Veri tabanındaki TalepID
        /// </summary>
        public int TalepID { get; set; }

        /// <summary>
        /// Talep numarası (MP00000001 gibi)
        /// </summary>
        public string TalepNo { get; set; }

        /// <summary>
        /// Talep oluşturma tarihi (formdaki TARIH alanı)
        /// </summary>
        public DateTime Tarih { get; set; }

        /// <summary>
        /// Departman kodu + açıklaması (örn: IT - Bilgi İşlem)
        /// İstersen direkt "Departman: ..." olarak tek string de tutabiliriz.
        /// </summary>
        public string Departman { get; set; }

        /// <summary>
        /// Masraf merkezi kodu + açıklaması
        /// (örn: CC1001 - IT COST CENTER)
        /// </summary>
        public string MasrafMerkezi { get; set; }

        /// <summary>
        /// Talep oluşturan kullanıcı (AD\SOYAD ya da User kodu)
        /// </summary>
        public string OlusturanKullanici { get; set; }

        /// <summary>
        /// Talep status kısa (örn: TALEP EDİLDİ, SATIN ALMA ONAYINDA)
        /// </summary>
        public string DurumKisa { get; set; }

        /// <summary>
        /// Talep status açıklaması (TBL_TalepOnayAciklama'dan)
        /// </summary>
        public string DurumAciklama { get; set; }

        // --------- SATIRLAR ---------

        /// <summary>
        /// Talep satırlarının listesi.
        /// DevExpress raporda Detail band/DetailReport olarak bağlanacak.
        /// </summary>
        public List<SatinAlmaTalepSatirDto> Satirlar { get; set; } = new List<SatinAlmaTalepSatirDto>();
    }
}
