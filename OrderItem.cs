using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Sipariş edilen her bir ürünün detaylarını (fiyat, miktar, satıcı) saklayan model.
    /// Sipariş anındaki verileri korumak için ürün bilgilerini "snapshot" olarak tutar.
    /// </summary>
    public class OrderItem
    {
        public int Id { get; set; }

        // Bağlı olduğu ana siparişin ID'si (Foreign Key)
        public int OrderId { get; set; }

        // Referans alınan orijinal ürünün ID'si
        public int ProductId { get; set; }

        // Sipariş anındaki ürün adı (Ürün adı değişse bile geçmiş kayıt değişmemeli)
        public string Name { get; set; } = "";

        // Ürünün sipariş anındaki görsel yolu
        public string ImageUrl { get; set; } = "";

        /// <summary>
        /// Ürünün birim satış fiyatı. 
        /// NULL gelme ihtimaline karşı nullable yapılmıştır (Defensive Programming).
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Sipariş edilen adet miktarı.
        /// </summary>
        public int? Qty { get; set; }

        // Ürünün hangi satıcıya ait olduğu bilgisi (Satıcı paneli filtrelemesi için kritik)
        public string SellerEmail { get; set; } = "";

        // Bu ürüne özel yazılmış hediye notu (Örn: Çiçek kartı mesajı)
        public string GiftCardMessage { get; set; } = "";

        /// <summary>
        /// Hesaplanan Özellik: Satır toplam tutarını verir.
        /// [NotMapped]: Veritabanında bir sütun olarak oluşturulmaz, çalışma anında hesaplanır.
        /// </summary>
        [NotMapped]
        public decimal LineTotal => (UnitPrice ?? 0m) * (Qty ?? 0);

        // Navigation Property: EF Core için Sipariş tablosuyla olan ilişkiyi tanımlar
        public virtual Order? Order { get; set; }
    }
}