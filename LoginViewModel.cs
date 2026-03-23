using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    /// <summary>
    /// Kullanıcı giriş formu için gerekli olan veri modelini temsil eder.
    /// View ve Controller arasındaki veri transferini (DTO) yönetir.
    /// </summary>
    public class LoginViewModel
    {
        // [Required]: Bu alanın boş bırakılamayacağını belirtir.
        // [EmailAddress]: Girişin geçerli bir e-posta formatında olmasını zorunlu kılar.
        // [Display]: Form etiketlerinde (label) görünecek dostane ismi tanımlar.
        [Required(ErrorMessage = "E-posta adresi girmek zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta Adresi")]
        public string Email { get; set; } = "";

        // [DataType(DataType.Password)]: HTML tarafında input tipinin 'password' olmasını sağlar (karakterleri gizler).
        [Required(ErrorMessage = "Şifre alanı boş bırakılamaz.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = "";

        // Kullanıcı oturumunun tarayıcı kapatıldıktan sonra da devam edip etmeyeceğini belirler.
        // Genellikle CookieAuthentication içindeki 'IsPersistent' özelliğine bağlanır.
        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}