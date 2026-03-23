namespace ciceksepetim.Models
{
    /// <summary>
    /// Sisteme kayıtlı tüm kullanıcıların (Müşteri, Satıcı, Admin) ortak temel sınıfı.
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }

        // Kullanıcının adı soyadı bilgisi.
        public string FullName { get; set; } = "";

        // Giriş anahtarı ve benzersiz tanımlayıcı.
        public string Email { get; set; } = "";

        // Güvenlik gereği şifre asla açık metin (plain text) olarak tutulmaz, 
        // burada şifrelenmiş (hashed) hali saklanır.
        public string PasswordHash { get; set; } = "";

        // Yetkilendirme için rol bilgisi: "Customer", "Seller" veya "Admin".
        public string Role { get; set; } = "Customer";

        // Özellikle satıcıların (Seller) sisteme giriş yapabilmesi için 
        // Admin tarafından onaylanıp onaylanmadığını belirtir.
        public bool IsApproved { get; set; } = false;

        // Sadakat programı: Kullanıcının alışverişlerde kullanabileceği "Hediye Knot" bakiyesi.
        public decimal GiftKnotBalance { get; set; } = 0m;

        // Profil güncellemelerinin takibi için zaman damgası.
        public DateTime? UpdatedAt { get; set; }
    }
}