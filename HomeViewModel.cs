namespace ciceksepetim.Models
{
    /// <summary>
    /// Ana sayfanın (Home/Index) tüm dinamik içeriğini birleştiren ViewModel.
    /// Navigation, reklam alanları ve ürün listeleme verilerini barındırır.
    /// </summary>
    public class HomeViewModel
    {
        // Ana sayfada üst veya yan menüde listelenecek kategoriler
        public List<CategoryLink> Categories { get; set; } = new();

        // Sayfa ortasındaki veya başındaki kampanyalı banner (reklam) kartları
        public List<BannerCard> Banners { get; set; } = new();

        /// <summary>
        /// KRİTİK DÜZELTME: Index.cshtml tarafında ürünlerin döngüye girmesini sağlar.
        /// HomeController'da veritabanından çekilen vitrin ürünlerini taşır.
        /// </summary>
        public List<Product> Products { get; set; } = new();
    }

    /// <summary>
    /// Kategori navigasyonu için kullanılan hafif veri yapısı.
    /// URL yerine SEO dostu "Slug" değerini tutar.
    /// </summary>
    /// <param name="Text">Kategorinin görünen adı (Örn: "Doğum Günü")</param>
    /// <param name="Slug">URL'de görünecek link parçası (Örn: "dogum-gunu")</param>
    public record CategoryLink(string Text, string Slug);

    /// <summary>
    /// Ana sayfadaki promosyon kartlarını temsil eden record tipi.
    /// Modern C# özelliklerini kullanarak değişmez (immutable) veri taşıma sağlar.
    /// </summary>
    /// <param name="Title">Banner başlığı</param>
    /// <param name="Subtitle">Alt başlık veya kampanya metni</param>
    /// <param name="ImageUrl">Görselin dosya yolu</param>
    /// <param name="size">CSS tarafında kartın genişliğini belirler (sm, md, lg)</param>
    /// <param name="theme">Kartın renk temasını belirler (blue, green, pink vs.)</param>
    /// <param name="LinkSlug">Banner tıklandığında yönlendirilecek hedef kategori/ürün</param>
    public record BannerCard(
        string Title, string Subtitle, string ImageUrl,
        string size = "md", string theme = "blue",
        string? LinkSlug = null
    );
}