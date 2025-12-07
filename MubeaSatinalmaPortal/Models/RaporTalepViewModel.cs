using System;
using System.Collections.Generic;

namespace MubeaSatinalmaPortal.Models
{
    public class RaporTalepSatirViewModel
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

    public class RaporTalepViewModel
    {
        public int TalepID { get; set; }
        public string TalepNo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedByUser { get; set; }

        public string DepartmanKodu { get; set; }
        public string DepartmanAciklama { get; set; }
        public string CostCenterCode { get; set; }
        public string CostCenterDescription { get; set; }
        public string HizmetKodu { get; set; }
        public string HizmetAciklamasi { get; set; }

        public int TalepStatusId { get; set; }
        public string TalepStatusKisa { get; set; }
        public string TalepStatusAciklama { get; set; }

        public List<RaporTalepSatirViewModel> Satirlar { get; set; } = new List<RaporTalepSatirViewModel>();
    }
}
