namespace ciceksepetim.Models
{
    /// <summary>
    /// Ürün listeleme sayfalarında (Grid/Kart görünümü) kullanılan minimalist veri yapısı.
    /// Record kullanımı sayesinde 'Immutable' (değiştirilemez) ve hafiftir.
    /// </summary>
    /// <param name="Id">Ürünün benzersiz kimliği.</param>
    /// <param name="Name">Ürün adı (Örn: "Kırmızı Gül Buketi").</param>
    /// <param name="Price">Satış fiyatı.</param>
    /// <param name="ImageUrl">Ürünün vitrin görselinin dosya yolu.</param>
    public record ProductCard(int Id, string Name, decimal Price, string ImageUrl);
}