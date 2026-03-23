using System.ComponentModel.DataAnnotations.Schema;

namespace ciceksepetim.Models
{
    // ============================================
    // 1. PROFILE VIEW MODEL (Tekrarlama Hatalarını çözer)
    // ============================================
    public class ProfileViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }

    // ============================================
    // 2. KAYITLI KARTLAR (SavedCard) - Controller Hatalarını çözer
    // ============================================
    // Bu model, AccountController'ın Cards ve AddCard metotlarında kullanılır.
    public class SavedCard
    {
        public int Id { get; set; }

        // Controller'da filtreleme ve çekim için zorunlu (Invalid column name 'Email' hatasını çözer)
        public string Email { get; set; } = "";

        public string HolderName { get; set; } = "";
        public string Brand { get; set; } = "";

        // Controller'da atama için zorunlu (Invalid column name 'Masked' ve CS0200 hatalarını çözer)
        public string Masked { get; set; } = "";

        // Controller'da atama için zorunlu (Invalid column name 'Exp' ve CS0200 hatalarını çözer)
        public string Exp { get; set; } = "";
    }

    // ============================================
    // 3. KAYITLI ADRESLER (SavedAddress) - CSHTML Hatalarını çözer
    // ============================================
    // Bu model, Addresses.cshtml'nin gerektirdiği tüm alanlara sahiptir.
    public class SavedAddress
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Title { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string City { get; set; } = "";
        public string District { get; set; } = "";

        // 🔥 CSHTML'deki @a.AddressLine ile eşleşmeli (Invalid column name 'AddressLine' hatasını çözer)
        public string AddressLine { get; set; } = "";

        // 🔥 CSHTML'deki @a.IsDefault ile eşleşmeli (CS1061 hatasını çözer)
        public bool IsDefault { get; set; }
    }
}