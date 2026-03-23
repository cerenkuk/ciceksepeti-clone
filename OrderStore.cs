using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ciceksepetim.Models
{
    /// <summary>
    /// Siparişlerin bellekte (RAM) geçici olarak tutulduğu depo sınıfı.
    /// Uygulama durduğunda veriler silinir; hızlı prototipleme ve test için idealdir.
    /// </summary>
    public static class OrderStore
    {
        // Thread-safe (kanallara güvenli) sözlük yapısı. 
        // Aynı anda birden fazla kişi sipariş verdiğinde veri bozulmasını önler.
        private static readonly ConcurrentDictionary<int, Order> _orders = new();

        // Benzersiz ID üretimi için atomik sayaç.
        private static int _seq = 1000;

        /// <summary>
        /// Yeni bir sipariş nesnesi oluşturur ve depoya ekler.
        /// </summary>
        /// <param name="lines">Sepetteki ürün ID'leri ve miktarları.</param>
        public static Order Create(string customerName, string customerEmail, IEnumerable<(int productId, int qty)> lines)
        {
            var order = new Order
            {
                // Interlocked: Aynı anda iki işlemin aynı ID'yi almasını kesin olarak engeller.
                Id = Interlocked.Increment(ref _seq),
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Alindi,
                Items = new List<OrderItem>()
            };

            foreach (var (pid, qty) in lines)
            {
                // Ürün bilgilerini mevcut katalogdan (snapshot) çekerek dondurur.
                var p = CatalogSeed.Products.FirstOrDefault(x => x.Id == pid);
                if (p == null) continue;

                order.Items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    UnitPrice = p.Price,
                    Qty = Math.Max(1, Math.Min(99, qty)) // 1-99 arası miktar kısıtlaması (Safety Check)
                });
            }

            _orders[order.Id] = order;
            return order;
        }

        /// <summary>
        /// Belirli bir satıcıya ait olan siparişleri getirir.
        /// </summary>
        public static List<Order> ListForSeller(string sellerEmail)
        {
            if (string.IsNullOrWhiteSpace(sellerEmail)) return new List<Order>();

            // Karmaşık Filtreleme: Siparişin içindeki ürünlerden herhangi biri 
            // sorgulanan satıcıya aitse o siparişi listeye dahil eder.
            return _orders.Values
                .Where(o => o.Items.Any(it => ProductOwnerStore.IsOwner(sellerEmail, it.ProductId)))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// ID'ye göre sipariş arar.
        /// </summary>
        public static Order? Get(int id) => _orders.TryGetValue(id, out var o) ? o : null;

        /// <summary>
        /// Müşterinin kendi sipariş geçmişini görmesini sağlar.
        /// </summary>
        public static List<Order> ListForCustomer(string email) =>
            _orders.Values.Where(o => o.CustomerEmail.Equals(email ?? "", StringComparison.OrdinalIgnoreCase))
                          .OrderByDescending(o => o.CreatedAt).ToList();
    }
}