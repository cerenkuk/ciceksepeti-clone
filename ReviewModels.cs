using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Ürünlere yapılan kullanıcı yorumlarını ve puanlamaları temsil eder.
    /// </summary>
    public class Review
    {
        public int Id { get; set; }

        // Hangi ürüne yorum yapıldığını belirtir.
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = default!;

        // Yorumu yapan kullanıcının kimliği.
        public string UserEmail { get; set; } = "";

        // 1 ile 5 yıldız arasında puanlama.
        public int Rating { get; set; }

        // Kullanıcının metin içeriği.
        public string Comment { get; set; } = "";

        // Yorumun yapıldığı tarih (Sıralama için kritiktir).
        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bellek içi (In-memory) yorum deposu. 
    /// Ürün bazlı hızlı erişim için Dictionary yapısı kullanılmıştır.
    /// </summary>
    public static class ReviewStore
    {
        // Key: ProductId, Value: O ürüne ait yorum listesi.
        private static readonly Dictionary<int, List<Review>> _byProduct = new();
        private static int _seq = 1;
        private static readonly object _lock = new();

        /// <summary>
        /// Yeni bir yorum ekler ve otomatik ID atar.
        /// </summary>
        public static void Add(Review r)
        {
            lock (_lock)
            {
                r.Id = _seq++;
                if (!_byProduct.ContainsKey(r.ProductId))
                    _byProduct[r.ProductId] = new();
                _byProduct[r.ProductId].Add(r);
            }
        }

        /// <summary>
        /// Ürüne ait yorumları en yeni tarihten en eskiye doğru getirir.
        /// </summary>
        public static List<Review> GetByProduct(int productId)
        {
            lock (_lock)
            {
                return _byProduct.TryGetValue(productId, out var list)
                    ? list.OrderByDescending(x => x.ReviewDate).ToList() // DÜZELTME: ReviewDate kullanıldı.
                    : new List<Review>();
            }
        }

        // ... Diğer metodlar (Remove, GetAll) thread-safe kilit yapısını koruyor.
    }

    /// <summary>
    /// Ürün detay sayfasında (Product/Detail) gösterilecek tüm verileri birleştirir.
    /// </summary>
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = default!;
        public string Description { get; set; } = "";
        public List<string> Specs { get; set; } = new(); // Ürün teknik özellikleri
        public List<Review> Reviews { get; set; } = new();

        // Ortalama puanı hesaplar (Yuvarlanmış şekilde).
        public double AvgRating => Reviews.Count == 0 ? 0 : Math.Round(Reviews.Average(x => x.Rating), 1);
        public int ReviewCount => Reviews.Count;

        // Benzer ürünler veya "Bunları da beğenebilirsiniz" listesi.
        public List<Product> Related { get; set; } = new();
    }
}