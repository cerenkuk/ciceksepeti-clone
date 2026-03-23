using System.Text.Json;

namespace ciceksepetim.Models
{
    /// <summary>
    /// ISession arayüzüne nesne tabanlı (Object-based) veri saklama yeteneği kazandıran genişletme sınıfı.
    /// Nesneleri JSON formatına dönüştürerek (Serialization) bellekte tutar.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Herhangi bir nesneyi JSON stringine dönüştürerek Session'a kaydeder.
        /// </summary>
        /// <typeparam name="T">Kaydedilecek nesne tipi.</typeparam>
        /// <param name="s">Genişletilen ISession nesnesi.</param>
        /// <param name="key">Veriye erişim için kullanılacak anahtar isim.</param>
        /// <param name="value">Saklanacak olan veri/nesne.</param>
        public static void SetObj<T>(this ISession s, string key, T value) =>
            s.SetString(key, JsonSerializer.Serialize(value));

        /// <summary>
        /// Session'da JSON formatında tutulan veriyi okur ve tekrar nesneye (Object) dönüştürür.
        /// </summary>
        /// <typeparam name="T">Dönüştürülecek hedef nesne tipi.</typeparam>
        /// <param name="s">Genişletilen ISession nesnesi.</param>
        /// <param name="key">Verinin saklandığı anahtar isim.</param>
        /// <returns>Veri varsa nesne olarak döner, yoksa T tipinde yeni bir boş örnek (new T) döner.</returns>
        public static T GetObj<T>(this ISession s, string key) where T : new()
        {
            var json = s.GetString(key);

            // Veri boşsa null döndürmek yerine 'new T()' dönmesi, 
            // Controller tarafında sürekli null kontrolü yapma zahmetini ortadan kaldırır.
            return string.IsNullOrEmpty(json) ? new T() : JsonSerializer.Deserialize<T>(json)!;
        }
    }
}