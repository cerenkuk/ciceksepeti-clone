using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Kullanıcılar arası iç mesajlaşma ve destek taleplerini temsil eden veri modeli.
    /// SQL tablosuyla (Messages) birebir uyumlu özellikler içerir.
    /// </summary>
    public class Message
    {
        [Key]
        public int Id { get; set; }

        // Mesajın başlığı (Örn: "Sipariş Gecikmesi", "Ürün Sorusu")
        public string Subject { get; set; } = "";

        // Mesajın ana gövdesi. 
        // Not: SQL'deki 'Content' sütunu ile eşleşir.
        public string Content { get; set; } = "";

        // Mesajın kategorisi: "Destek", "Sipariş", "ÜrünSorusu" vb.
        public string MessageType { get; set; } = "Destek";

        // GÖNDEREN BİLGİSİ
        // AppUser tablosundaki Email alanı ile mantıksal bir ilişki kurar.
        public string SenderEmail { get; set; } = "";

        // ALICI BİLGİSİ
        // Mesajın kime ulaştırılacağını belirten e-posta adresi.
        public string RecipientEmail { get; set; } = "";

        // Okundu Bilgileri: Mesajın iki taraflı durum takibini sağlar.
        public bool IsReadBySender { get; set; } = true;
        public bool IsReadByRecipient { get; set; } = false;

        // Mesajın gönderildiği zaman damgası.
        public DateTime SentDate { get; set; } = DateTime.Now;

        // Eğer mesaj bir siparişle ilgiliyse, o siparişin Id'si burada tutulur.
        public int? RelatedOrderId { get; set; }

        /// <summary>
        /// Self-Referencing (Kendi Kendine Referans): 
        /// Bir mesaj dizisini (conversation thread) takip etmek için kullanılır.
        /// Eğer bu bir yanıtsa, diziyi başlatan ilk mesajın Id'sini tutar.
        /// </summary>
        public int? ConversationId { get; set; }

        // EF Core Navigation Property: ConversationId üzerinden ana mesaja erişim sağlar.
        [ForeignKey(nameof(ConversationId))]
        public virtual Message? ConversationStarter { get; set; }
    }
}