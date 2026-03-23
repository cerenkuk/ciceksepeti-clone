using System.Collections.Generic;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Satıcı Panelinin ana sayfasında (Dashboard) gösterilecek istatistiksel verileri taşır.
    /// Satış trendleri, finansal durum ve müşteri geri bildirimlerini konsolide eder.
    /// </summary>
    public class SellerDashboardViewModel
    {
        // --- Temel Metrikler (KPIs) ---
        public int TotalProducts { get; set; }      // Satıcının aktif ürün sayısı
        public int PendingOrders { get; set; }      // Henüz onaylanmamış/hazırlanmamış siparişler
        public decimal TotalSalesRevenue { get; set; } // Tüm zamanların toplam kazancı
        public decimal MonthlySalesRevenue { get; set; } // İçinde bulunulan ayın kazancı

        // --- Mağaza Puanı ve Geri Bildirimler ---
        public double AverageRating { get; set; }   // Mağaza genel puan ortalaması
        public int TotalReviews { get; set; }       // Toplam yorum sayısı
        public List<Review> LatestReviews { get; set; } = new List<Review>(); // Son gelen yorumlar

        // --- Finansal ve Bonus Bilgileri ---
        // Hediye Knot Bakiyesi: Platform içi teşvik veya kupon sistemi bakiyesi.
        public decimal GiftKnotBalance { get; set; }
        public decimal TotalCargoRevenue { get; set; } // Kargo kesintileri veya kazançları

        // --- Grafik Verileri (Chart.js vb. kütüphaneler için) ---
        // Haftalık kazanç ve sipariş trendlerini görselleştirmek için kullanılır.
        public List<decimal> WeeklyRevenueChart { get; set; } = new List<decimal>();
        public List<int> WeeklyOrderCountChart { get; set; } = new List<int>();
        public List<string> WeeklyLabels { get; set; } = new List<string>(); // Örn: "Pzt", "Sal"...

        // --- Performans Analizi ---
        // Satıcının en çok kazandıran/satan ürünlerini listeler.
        public List<ProductSalesViewModel> TopSellingProducts { get; set; } = new List<ProductSalesViewModel>();

        /// <summary>
        /// Ürün bazlı satış performansını tutan yardımcı alt model.
        /// </summary>
        public class ProductSalesViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public int TotalQuantitySold { get; set; } // Satış adedi
            public decimal TotalRevenue { get; set; }    // Bu üründen elde edilen ciro
        }
    }
}