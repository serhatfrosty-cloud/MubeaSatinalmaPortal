using System;

namespace MubeaSatinalmaPortal.Models
{
    /// <summary>
    /// Satın alma talebinin her bir satırı için DTO.
    /// Excel formundaki her satıra karşılık gelir (Pozisyon 1, 2, 3 ...).
    /// </summary>
    public class SatinAlmaTalepSatirDto
    {
        /// <summary>
        /// Pozisyon / Satır numarası (1,2,3...)
        /// </summary>
        public int SatirNo { get; set; }

        /// <summary>
        /// Sipariş adedi (TalepAdet)
        /// </summary>
        public decimal? TalepAdet { get; set; }

        /// <summary>
        /// Birim (ADET, SET, PAKET vb.)
        /// </summary>
        public string Birim { get; set; }

        /// <summary>
        /// Malzeme Tanımı
        /// </summary>
        public string MalzemeTanimi { get; set; }

        /// <summary>
        /// Yorum / Gerekçe
        /// </summary>
        public string Gerekce { get; set; }

        /// <summary>
        /// Tavsiye edilen tedarikçi
        /// </summary>
        public string TavsiyeTedarikci { get; set; }

        /// <summary>
        /// Fiyat (sadece sayı kısmı) - TalepFiyat
        /// </summary>
        public decimal? Fiyat { get; set; }

        /// <summary>
        /// Para birimi (EUR, USD, TL...) - FiyatParaBirimi
        /// </summary>
        public string ParaBirimi { get; set; }

        /// <summary>
        /// Tedarikçi ürün kodu
        /// </summary>
        public string TedarikciUrunKodu { get; set; }

        /// <summary>
        /// Talep tipi (YENİ / YEDEK)
        /// </summary>
        public string TalepTipi { get; set; }

        /// <summary>
        /// İhtiyaç termini
        /// </summary>
        public DateTime? IhtiyacTermini { get; set; }

        /// <summary>
        /// Talep eden kişi (griddeki alan)
        /// </summary>
        public string TalepEden { get; set; }
    }
}
