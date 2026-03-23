namespace ciceksepetim.Models
{
    /// <summary>
    /// Arama sonuçları, filtreleme ve sayfalama verilerini taşıyan görünüm modeli.
    /// Hem arama çubuğundan gelen sorguları hem de kategori sayfalarını destekler.
    /// </summary>
    public class SearchVM
    {
        // 'Q' (Query): Kullanıcının arama kutusuna yazdığı anahtar kelime.
        public string Q { get; set; } = "";

        // 'Cat' (Category): Filtreleme için kullanılan kategori slug değeri (Örn: "orkide").
        public string? Cat { get; set; }

        // Arama veya filtreleme kriterlerine uyan ürünlerin listesi.
        public List<Product> Results { get; set; } = new();

        // Kenar çubuğunda (Sidebar) listelenecek tüm kategoriler.
        public List<Category> Categories { get; set; } = new();

        // ===================================
        // SAYFALAMA (PAGINATION) MANTIĞI
        // ===================================

        // Aktif olarak görüntülenen sayfa numarası.
        public int Page { get; set; } = 1;

        // Bir sayfada gösterilecek maksimum ürün sayısı.
        public int PageSize { get; set; } = 12;

        // Kriterlere uyan toplam ürün sayısı (DB'den dönen filtrelenmiş sayı).
        public int Total { get; set; }

        /// <summary>
        /// Toplam sayfa sayısını hesaplayan yardımcı özellik.
        /// Matematiksel olarak (Toplam / SayfaBoyutu) değerini yukarı yuvarlar.
        /// </summary>
        public int Pages => (int)Math.Ceiling((double)Math.Max(0, Total) / Math.Max(1, PageSize));
    }
}