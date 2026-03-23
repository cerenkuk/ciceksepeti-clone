using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    /// <summary>
    /// Kullanıcı kayıt formu için veri taşıma ve doğrulama modelidir.
    /// View tarafındaki form ile Controller arasındaki köprüyü kurar.
    /// </summary>
    public class RegisterViewModel
    {
        // [Required]: Bu alanın doldurulması zorunludur.
        // [Display]: HTML formunda etiket (label) olarak görünecek ismi belirler.
        [Required(ErrorMessage = "Ad Soyad alanı boş geçilemez.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = "";

        // [EmailAddress]: Girişin e-posta formatında (örn: isim@domain.com) olup olmadığını denetler.
        [Required(ErrorMessage = "E-posta adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = "";

        // [MinLength(6)]: Şifrenin en az 6 karakter olması gerektiğini belirtir (Güvenlik gereksinimi).
        // [DataType(DataType.Password)]: Tarayıcıda karakterlerin yıldız veya nokta şeklinde maskelenmesini sağlar.
        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifreniz en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = "";

        // [Compare]: 'Password' özelliği ile bu özelliğin değerinin aynı olduğunu kontrol eder. 
        // Kullanıcının şifresini yanlış yazmasını önlemek için kullanılır.
        [Required(ErrorMessage = "Lütfen şifrenizi tekrar giriniz.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Şifreler birbiriyle eşleşmiyor.")]
        [Display(Name = "Şifre (Tekrar)")]
        public string ConfirmPassword { get; set; } = "";
    }
}