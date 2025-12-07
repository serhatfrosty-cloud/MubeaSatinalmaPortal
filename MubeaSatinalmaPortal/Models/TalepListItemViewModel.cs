using System;

namespace MubeaSatinalmaPortal.Models
{
    public class TalepListItemViewModel
    {
        public int TalepID { get; set; }
        public string TalepNo { get; set; }
        public string Departman { get; set; }
        public string CostCenter { get; set; }
        public string HizmetTipi { get; set; }
        public string TalepStatusKisa { get; set; }
        public string TalepStatusAciklama { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
