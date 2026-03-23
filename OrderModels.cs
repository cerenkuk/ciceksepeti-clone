using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Siparişin ana gövdesini temsil eden model. 
    /// Müşteri bilgileri, teslimat detayları, ödeme özeti ve kargo takibini yönetir.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }

        // ===================================
        // 1. MÜŞTERİ VE TESLİMAT BİLGİLERİ
        // ===================================
        public string CustomerEmail { get; set; } = ""; // Siparişi veren hesap
        public string CustomerName { get; set; } = "";  // Siparişi veren kişi
        public string FullName { get; set; } = "";      // Alıcı adı (Teslim edilecek kişi)
        public string Phone { get; set; } = "";         // Alıcı iletişim numarası

        // Adres Hiyerarşisi
        public string City { get; set; } = "";
        public string District { get; set; } = "";

        /// <summary>
        /// CSHTML görünümleriyle uyumluluk için AddressLine1 olarak revize edildi.
        /// </summary>
        public string AddressLine1 { get; set; } = "";

        // ===================================
        // 2. FİNANSAL VERİLER VE ÖDEME
        // ===================================
        public string PaymentMethod { get; set; } = ""; // Örn: Kredi Kartı, Havale

        // Nullable decimal alanlar: İndirim veya kargo kampanyaları hesaplaması için esneklik sağlar.
        public decimal? Subtotal { get; set; } // Ürünlerin toplam bedeli
        public decimal? Shipping { get; set; } // Kargo ücreti
        public decimal? Total { get; set; }    // Genel Toplam (Subtotal + Shipping)
        public int ItemCount { get; set; }     // Siparişteki toplam parça sayısı

        // Hassas verilerin güvenliği için kart numarasının sadece son 4 hanesini maskeli tutarız.
        public string CardHolderName { get; set; } = "";
        public string CardExpiration { get; set; } = "";
        public string CardNumberMasked { get; set; } = "";

        // ===================================
        // 3. SÜREÇ YÖNETİMİ VE LOJİSTİK
        // ===================================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Siparişin yaşam döngüsü: Alındı -> Hazırlanıyor -> Kargolandı -> Tamamlandı
        public OrderStatus Status { get; set; } = OrderStatus.Alindi;
        public string DeliveryStatus { get; set; } = "Hazırlanıyor"; // Kullanıcıya gösterilecek sözel durum

        // Kargo Takibi (Satıcı tarafından güncellenir)
        [MaxLength(50)]
        public string? TrackingNumber { get; set; } // Kargo takip kodu
        [MaxLength(50)]
        public string? CargoCompany { get; set; }   // Taşıyıcı firma ismi

        // Hediye gönderimleri için özel tarihli teslimat planlama
        public DateTime? DeliveryScheduledTime { get; set; }
        public string GeneralGiftMessage { get; set; } = ""; // Tüm sipariş için ortak not

        // ===================================
        // 4. İLİŞKİSEL VERİLER
        // ===================================
        /// <summary>
        /// Bire-Çok (One-to-Many) İlişki: Bir siparişin birden fazla kalemi olabilir.
        /// </summary>
        public List<OrderItem> Items { get; set; } = new();
    }
}