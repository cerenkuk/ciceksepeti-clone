namespace ciceksepetim.Models
{
    /// <summary>
    /// Ürünler ve Satıcılar arasındaki sahiplik ilişkisini yöneten bellek içi (In-memory) depo.
    /// Satıcı bazlı filtreleme ve yetki kontrolleri için merkezi bir nokta sağlar.
    /// </summary>
    public static class ProductOwnerStore
    {
        // Key: ProductId, Value: SellerEmail
        // Hangi ürünün hangi satıcı hesabı tarafından oluşturulduğunu eşleştirir.
        private static readonly Dictionary<int, string> _owner = new();

        // Çoklu kanal (Thread) güvenliği için kilit nesnesi.
        private static readonly object _lock = new();

        /// <summary>
        /// Bir ürünü belirli bir satıcıya atar.
        /// </summary>
        public static void SetOwner(int productId, string email)
        {
            lock (_lock) _owner[productId] = email;
        }

        /// <summary>
        /// Ürünün sahibini (satıcı e-postasını) döndürür.
        /// </summary>
        public static string? GetOwner(int productId)
        {
            lock (_lock) return _owner.TryGetValue(productId, out var e) ? e : null;
        }

        /// <summary>
        /// Verilen e-postanın, belirtilen ürünün sahibi olup olmadığını doğrular.
        /// Satıcı paneli işlemlerinde (Ürün güncelleme, sipariş görme) güvenlik kontrolü için kullanılır.
        /// </summary>
        public static bool IsOwner(string email, int productId)
            => string.Equals(GetOwner(productId), email, StringComparison.OrdinalIgnoreCase);
    }
}