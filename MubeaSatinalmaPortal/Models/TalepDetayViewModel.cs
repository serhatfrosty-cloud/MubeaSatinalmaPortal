using System;
using System.Collections.Generic;

namespace MubeaSatinalmaPortal.Models
{
    public class TalepDetaySatirViewModel
    {
        public int TalepDetailsRows { get; set; }
        public decimal? TalepAdet { get; set; }
        public string TalepBirim { get; set; }
        public string TalepMalzeme { get; set; }
        public string TalepGerekce { get; set; }
        public string TavsiyeTedarikci { get; set; }
        public decimal? TalepFiyat { get; set; }
        public string FiyatParaBirimi { get; set; }
        public string TedarikciUrunKodu { get; set; }
        public string TalepTipi { get; set; }
        public DateTime? TalepTermin { get; set; }
        public string TalepEdenKisi { get; set; }
    }

    public class TalepOnayLogViewModel
    {
        public int TalepOnayLogID { get; set; }
        public int EskiStatusId { get; set; }
        public int YeniStatusId { get; set; }
        public string IslemTipi { get; set; }          // ONAY / RED
        public string IslemYapanUser { get; set; }
        public int IslemYapanLevel { get; set; }
        public string OnayNotu { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string EskiStatusText { get; set; }     // Örn: Talep Edildi
        public string YeniStatusText { get; set; }     // Örn: Satın Alma Onayında
    }

    public class TalepDetayViewModel
    {
        public int TalepID { get; set; }
        public string TalepNo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedByUser { get; set; }
        public int TalepStatusId { get; set; }
        public string TalepStatusKisa { get; set; }
        public string TalepStatusAciklama { get; set; }

        public string DepartmanKodu { get; set; }
        public string DepartmanAciklama { get; set; }
        public string CostCenterCode { get; set; }
        public string CostCenterDescription { get; set; }
        public string HizmetKodu { get; set; }
        public string HizmetAciklamasi { get; set; }

        public bool KullaniciOnayVerebilirMi { get; set; }

        public List<TalepDetaySatirViewModel> Satirlar { get; set; } = new List<TalepDetaySatirViewModel>();

        // 🔹 ONAY / RED GEÇMIŞİ
        public List<TalepOnayLogViewModel> OnayLoglari { get; set; } = new List<TalepOnayLogViewModel>();
    }
}
