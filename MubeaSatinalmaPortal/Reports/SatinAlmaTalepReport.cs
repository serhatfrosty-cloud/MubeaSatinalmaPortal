using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using MubeaSatinalmaPortal.Models;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing.Printing;

namespace MubeaSatinalmaPortal.Reports
{
    public partial class SatinAlmaTalepReport : XtraReport
    {
        public SatinAlmaTalepReport()
        {
            InitializeComponent();
        }

        public void LoadFromDto(SatinAlmaTalepRaporDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Header alanları
            if (xrLblDepartman != null)
                xrLblDepartman.Text = "Departman : " + (dto.Departman ?? string.Empty);

            if (xrLbltarih != null)
                xrLbltarih.Text = "Tarih : " + dto.Tarih.ToString("dd.MM.yyyy");

            if (xrlblcost != null)
                xrlblcost.Text = "Masraf Merkezi : " + (dto.MasrafMerkezi ?? string.Empty);

            // =======================
            // DETAIL DATATABLE
            // =======================
            var dt = new DataTable("TalepDetails");

            // raporda kullandığın kolonlar
            dt.Columns.Add("TalepDetailsRows", typeof(int));
            dt.Columns.Add("TalepAdet", typeof(decimal));
            dt.Columns.Add("TalepBirim", typeof(string));
            dt.Columns.Add("TalepMalzeme", typeof(string));
            dt.Columns.Add("TalepGerekce", typeof(string));
            dt.Columns.Add("TavsiyeTedarikci", typeof(string));
            dt.Columns.Add("TalepFiyat", typeof(decimal));
            dt.Columns.Add("FiyatParaBirimi", typeof(string));
            dt.Columns.Add("TedarikciUrunKodu", typeof(string));
            dt.Columns.Add("TalepTipi", typeof(string));
            dt.Columns.Add("TalepTermin", typeof(DateTime));
            dt.Columns.Add("TalepEdenKisi", typeof(string));




            if (dto.Satirlar != null)
            {
                foreach (var s in dto.Satirlar)
                {


                    var row = dt.NewRow();


                    row["TalepDetailsRows"] = s.SatirNo;
                    row["TalepAdet"] = (object?)s.TalepAdet ?? DBNull.Value;
                    row["TalepBirim"] = s.Birim ?? string.Empty;
                    row["TalepMalzeme"] = s.MalzemeTanimi ?? string.Empty;
                    row["TalepGerekce"] = s.Gerekce ?? string.Empty;
                    row["TavsiyeTedarikci"] = s.TavsiyeTedarikci ?? string.Empty;
                    row["TalepFiyat"] = (object?)s.Fiyat ?? DBNull.Value;
                    row["FiyatParaBirimi"] = s.ParaBirimi ?? string.Empty;
                    row["TedarikciUrunKodu"] = s.TedarikciUrunKodu ?? string.Empty;
                    row["TalepTipi"] = s.TalepTipi ?? string.Empty;
                    row["TalepTermin"] = (object?)s.IhtiyacTermini ?? DBNull.Value;
                    row["TalepEdenKisi"] = s.TalepEden ?? string.Empty;

                    dt.Rows.Add(row);
                }
            }

            this.DataSource = dt;
            xrCellTalepEden.ExpressionBindings.Clear();
            xrCellTalepEden.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "'TALEP EDEN'")
            );

            xrCellTalepEden.ProcessDuplicatesTarget = ProcessDuplicatesTarget.Value;
            xrCellTalepEden.ProcessDuplicatesMode = ProcessDuplicatesMode.Merge;
        }

        // Bu event'i SADECE sol 1. sütundaki hücreye bağla: xrCellTalepEden


    }
}
