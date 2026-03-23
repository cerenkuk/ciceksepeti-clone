using ciceksepetim.Models;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Satıcı panelinde hediye mesajlarını listelemek ve yazdırmak (print) için kullanılan görünüm modeli.
    /// Sipariş genelindeki mesaj ile ürün bazlı özel mesajları birleştirir.
    /// </summary>
    public class SellerGiftCardViewModel
    {
        // İlgili siparişin benzersiz numarası.
        public int OrderId { get; set; }

        /// <summary>
        /// Ürünün adı. 
        /// Satıcının hangi hediye kartının hangi ürünle eşleşeceğini bilmesi için gereklidir.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Sipariş edilen adet.
        /// Çoklu alımlarda kaç adet kart hazırlanacağını belirlemek için kullanılır.
        /// </summary>
        public int Qty { get; set; }

        /// <summary>
        /// Ürüne özel hediye kartı mesajı.
        /// Örn: "Doğum günün kutlu olsun canım arkadaşım."
        /// </summary>
        public string GiftCardMessage { get; set; } = "";

        /// <summary>
        /// Siparişin tamamı için geçerli olan genel not.
        /// Genellikle fatura adresi veya teslimat talimatı gibi ek bilgileri içerebilir.
        /// </summary>
        public string GeneralGiftMessage { get; set; } = "";

        // Siparişin oluşturulma tarihi. 
        // Satıcının teslimat önceliğini belirlemesine yardımcı olur.
        public DateTime OrderDate { get; set; }

        // Siparişin güncel durumu (Alındı, Hazırlanıyor vb.).
        public OrderStatus OrderStatus { get; set; }
    }
}