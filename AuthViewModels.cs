using System.ComponentModel.DataAnnotations;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Yeni kullanıcı kayıt formu için kullanılan veri transfer modeli (DTO).
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad alanı boş bırakılamaz.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "E-posta adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta formatı giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)] // View tarafında input type="password" olmasını sağlar.
        [Display(Name = "Şifre")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        // Password alanı ile birebir aynı değerin girilmesini zorunlu kılar (Client-side validation).
        [Compare(nameof(Password), ErrorMessage = "Şifreler birbiriyle eşleşmiyor.")]
        [Display(Name = "Şifre (Tekrar)")]
        public string ConfirmPassword { get; set; } = "";
    }

    /// <summary>
    /// Sisteme giriş (Login) formunda kullanıcıdan beklenen verileri temsil eder.
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta girmek zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Şifre girmek zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = "";

        // Cookie bazlı kimlik doğrulamada "Persistent Cookie" oluşturulup oluşturulmayacağını belirler.
        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }
}