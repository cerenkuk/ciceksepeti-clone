using System;
using System.ComponentModel.DataAnnotations;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Ödeme ve sipariş tamamlama (Checkout) sayfasından gelen verileri taşıyan model.
    /// Bu modeldeki veriler, doğrulama sonrası 'Order' nesnesine dönüştürülür.
    /// </summary>
    public class PlaceOrderViewModel
    {
        // --- Teslimat Bilgileri ---
        [Required(ErrorMessage = "Alıcı adı zorunludur.")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Şehir seçimi zorunludur.")]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "İlçe seçimi zorunludur.")]
        public string District { get; set; } = "";

        [Required(ErrorMessage = "Açık adres belirtilmelidir.")]
        public string AddressLine1 { get; set; } = "";

        // --- Kişiselleştirme ---
        // Çiçek siparişlerinde opsiyonel olan genel kart mesajı.
        public string GeneralGiftMessage { get; set; } = "";

        // Alıcının çiçeği teslim almasını istediği özel zaman dilimi.
        public DateTime? DeliveryScheduledTime { get; set; }

        // --- Ödeme Bilgileri (Kredi Kartı) ---
        // NOT: Güvenlik nedeniyle bu veriler veritabanına kaydedilmez, 
        // sadece ödeme anında banka servisine (gateway) gönderilir.

        [Required(ErrorMessage = "Kart üzerindeki isim zorunludur.")]
        public string CardHolderName { get; set; } = "";

        [Required(ErrorMessage = "Kart numarası gereklidir.")]
        [CreditCard(ErrorMessage = "Geçerli bir kart numarası giriniz.")]
        public string CardNumber { get; set; } = "";

        [Required(ErrorMessage = "Son kullanma tarihi (AA/YY) zorunludur.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Format AA/YY şeklinde olmalıdır.")]
        public string CardExpiration { get; set; } = "";
    }
}