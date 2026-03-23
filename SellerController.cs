using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

using ciceksepetim.Models;

using Microsoft.EntityFrameworkCore;



namespace ciceksepetim.Controllers

{

    [Authorize(Roles = "Seller")]

    [Route("Seller/[action]/{id?}")]

    public class SellerController : Controller

    {

        private readonly ApplicationDbContext _db;

        private string MeEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";



        public SellerController(ApplicationDbContext db)

        {

            _db = db;

        }



        private IQueryable<Product> GetMyProductsQuery() => _db.Products.Where(p => p.SellerEmail == MeEmail);



        // ===================================DASHBOARD
        // 1. DASHBOARD - Düzenlenmiş ve Optimize Edilmiş Versiyon
        // ===================================
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.Now;
            var mineProducts = await GetMyProductsQuery().ToListAsync();
            var sellerProductIds = mineProducts.Select(p => p.Id).ToList();

            // Sipariş kalemlerini ve bağlı sipariş bilgilerini çekiyoruz
            var myOrderItems = await _db.OrderItems
                .AsNoTracking()
                .Where(i => i.SellerEmail == MeEmail)
                .Include(i => i.Order)
                .Select(i => new OrderItem
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    Name = i.Name ?? "Bilinmeyen Ürün",
                    SellerEmail = i.SellerEmail ?? "",
                    UnitPrice = i.UnitPrice ?? 0m,
                    Qty = i.Qty ?? 0,
                    Order = i.Order // Order.Shipping ve Order.Status için gerekli
                })
                .ToListAsync();

            // Hediye Knot Bakiyesi (Kullanıcı tablosundan direkt çekim)
            var giftKnotBalance = await _db.AppUsers
                .Where(u => u.Email == MeEmail)
                .Select(u => (decimal?)u.GiftKnotBalance)
                .FirstOrDefaultAsync() ?? 0m;

            // Kargo Geliri Hesaplama 
            // Satıcının dahil olduğu her bir farklı siparişten gelen kargo ücretini toplar
            var totalCargoRevenue = await _db.Orders
          .Where(o => o.Items.Any(i => i.SellerEmail == MeEmail))
          .SumAsync(o => o.Shipping) ?? 0m;
            // Okunmamış Mesaj Sayısı
            ViewBag.UnreadSellerMessages = await _db.Message
                .Where(m => m.RecipientEmail == MeEmail && !m.IsReadByRecipient)
                .CountAsync();

            // Yorum İstatistikleri
            var allSellerReviewsQuery = _db.Reviews.Where(r => sellerProductIds.Contains(r.ProductId));
            var totalReviews = await allSellerReviewsQuery.CountAsync();
            var averageRating = totalReviews > 0
                ? await allSellerReviewsQuery.Select(r => (double?)r.Rating).AverageAsync() ?? 0.0
                : 0.0;

            var latestReviews = await allSellerReviewsQuery
                .OrderByDescending(r => r.ReviewDate)
                .Take(5)
                .ToListAsync();

            // ViewModel Oluşturma
            var viewModel = new SellerDashboardViewModel
            {
                TotalProducts = mineProducts.Count,
                PendingOrders = await _db.Orders
            .CountAsync(o => o.Items.Any(i => i.SellerEmail == MeEmail) &&
                       (o.Status == OrderStatus.Alindi || o.Status == OrderStatus.Hazirlaniyor)),

                // Kazanç: Ürün Satışları + Kargo Geliri
                TotalSalesRevenue = myOrderItems.Sum(i => (i.UnitPrice ?? 0m) * (i.Qty ?? 0)) + totalCargoRevenue,

                TotalCargoRevenue = totalCargoRevenue,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = totalReviews,
                LatestReviews = latestReviews
            };

            // 5. GRAFİK VERİLERİ (Son 4 Hafta Döngüsü)
            for (int i = 3; i >= 0; i--)
            {
                var startDate = now.Date.AddDays(-(i + 1) * 7);
                var endDate = now.Date.AddDays(-i * 7);

                viewModel.WeeklyLabels.Add(i == 0 ? "Bu Hafta" : $"{i} Hafta Önce");

                var weekItems = myOrderItems
                    .Where(item => item.Order != null && item.Order.CreatedAt >= startDate && item.Order.CreatedAt < endDate)
                    .ToList();

                viewModel.WeeklyRevenueChart.Add(weekItems.Sum(x => (x.UnitPrice ?? 0m) * (x.Qty ?? 0)));
                viewModel.WeeklyOrderCountChart.Add(weekItems.Select(x => x.OrderId).Distinct().Count());
            }

            // 6. EN ÇOK KAZANDIRAN ÜRÜNLER (Tablo için)
            viewModel.TopSellingProducts = myOrderItems
                .GroupBy(i => new { i.ProductId, i.Name })
                .Select(g => new SellerDashboardViewModel.ProductSalesViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name ?? "Bilinmeyen Ürün",
                    TotalQuantitySold = g.Sum(i => i.Qty ?? 0),
                    TotalRevenue = g.Sum(i => (i.UnitPrice ?? 0m) * (i.Qty ?? 0))
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(5)
                .ToList();

            return View(viewModel);
        }

        // ===================================

            // 2. PRODUCT MANAGEMENT

            // ===================================

        public async Task<IActionResult> Products() => View(await GetMyProductsQuery().OrderByDescending(p => p.Id).ToListAsync());



        [HttpGet]

        public IActionResult CreateProduct()

        {

            ViewBag.Categories = _db.Categories.ToList();

            return View();

        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                // ÖNEMLİ: Bu isimde bir ürün bu satıcıda zaten var mı kontrol et
                var existingProduct = await _db.Products
                    .FirstOrDefaultAsync(p => p.Name == product.Name && p.SellerEmail == MeEmail);

                if (existingProduct != null)
                {
                    // ÜRÜN VARSA: Yeni ekleme yapma, mevcut olanın stoğunu ve fiyatını güncelle
                    existingProduct.Stock = product.Stock;
                    existingProduct.Price = product.Price;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.CategorySlug = product.CategorySlug;

                    _db.Products.Update(existingProduct);
                    await _db.SaveChangesAsync();

                    TempData["ok"] = "Bu ürün zaten mevcuttu, bilgileri ve stoğu güncellendi.";
                    return RedirectToAction(nameof(Products));
                }
                else
                {
                    // ÜRÜN YOKSA: Normal şekilde yeni ürün olarak ekle
                    product.SellerEmail = MeEmail;
                    _db.Products.Add(product);
                    await _db.SaveChangesAsync();

                    TempData["ok"] = "Yeni ürün başarıyla eklendi.";
                    return RedirectToAction(nameof(Products));
                }
            }

            ViewBag.Categories = _db.Categories.ToList();
            return View(product);
        }

        [HttpGet]

        public async Task<IActionResult> EditProduct(int id)

        {

            // Güvenlik: Sadece bu satıcıya ait ürünü getir

            var product = await GetMyProductsQuery().FirstOrDefaultAsync(p => p.Id == id);



            if (product == null)

            {

                TempData["error"] = "Ürün bulunamadı veya bu işleme yetkiniz yok.";

                return RedirectToAction(nameof(Products));

            }



            ViewBag.Categories = _db.Categories.ToList();

            return View(product);

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> EditProduct(int id, Product product)

        {

            if (id != product.Id) return NotFound();



            // Güvenlik Kontrolü: Ürün gerçekten bu satıcının mı?

            var existingProduct = await _db.Products.AsNoTracking()

        .FirstOrDefaultAsync(p => p.Id == id && p.SellerEmail == MeEmail);



            if (existingProduct == null) return Unauthorized();



            if (ModelState.IsValid)

            {

                try

                {

                    product.SellerEmail = MeEmail; // Email'in değişmediğinden emin olalım

                    _db.Update(product);

                    await _db.SaveChangesAsync();

                    TempData["ok"] = "Ürün başarıyla güncellendi.";

                    return RedirectToAction(nameof(Products));

                }

                catch (DbUpdateConcurrencyException)

                {

                    if (!_db.Products.Any(e => e.Id == product.Id)) return NotFound();

                    else throw;

                }

            }



            ViewBag.Categories = _db.Categories.ToList();

            return View(product);

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteProduct(int id)

        {

            var product = await GetMyProductsQuery().FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)

            {

                _db.Products.Remove(product);

                await _db.SaveChangesAsync();

                TempData["ok"] = "Ürün silindi.";

            }

            return RedirectToAction(nameof(Products));

        }



        // ===================================

        // 3. ORDER & CARGO MANAGEMENT

        // ===================================

        public async Task<IActionResult> Orders()

        {

            var myOrderIds = await _db.OrderItems.Where(i => i.SellerEmail == MeEmail).Select(i => i.OrderId).Distinct().ToListAsync();

            var orders = await _db.Orders.Include(o => o.Items).Where(o => myOrderIds.Contains(o.Id)).OrderByDescending(o => o.CreatedAt).ToListAsync();

            return View(orders);

        }


        public async Task<IActionResult> OrderDetail(int id)
        {
            // Siparişi, sadece bu satıcıya ait ürünlerle birlikte getiriyoruz.
            var order = await _db.Orders
                .Include(o => o.Items.Where(i => i.SellerEmail == MeEmail))
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || !order.Items.Any())
            {
                return RedirectToAction(nameof(Orders));
            }

            // Kargo ücreti güvenliği
            order.Shipping ??= 0m;

            // View'da Model.CreatedAt kullanmayı unutmayın (OrderDate yerine)
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Route kısmını en başa ' / ' koyarak kökten tanımlıyoruz (Global kuralı iptal eder)
        [Route("/Seller/UpdateCargoAction")]
        public async Task<IActionResult> UpdateCargoAction(int orderId, string cargoCompany, string trackingNumber)
        {
            // Debug için: Buraya bir breakpoint koyabilirsin.
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // Satıcı doğrulaması
            if (!order.Items.Any(i => i.SellerEmail == MeEmail)) return Unauthorized();

            order.CargoCompany = cargoCompany;
            order.TrackingNumber = trackingNumber;
            order.Status = OrderStatus.Kargolandı;
            order.DeliveryStatus = "Kargolandı";
            order.UpdatedAt = DateTime.Now;

            _db.Orders.Update(order);
            await _db.SaveChangesAsync();

            TempData["ok"] = "Kargo başarıyla güncellendi.";
            return RedirectToAction("OrderDetail", new { id = orderId });
        }


        // ===================================

        // 4. MESSAGES

        // ===================================

        public async Task<IActionResult> Messages()

        {

            var conversations = await _db.Message

              .Where(m => m.ConversationId == null && (m.SenderEmail == MeEmail || m.RecipientEmail == MeEmail))

              .OrderByDescending(m => m.SentDate).ToListAsync();

            return View(conversations);

        }



        public async Task<IActionResult> MessageDetail(int id)

        {

            var messages = await _db.Message

              .Where(m => (m.Id == id || m.ConversationId == id) && (m.SenderEmail == MeEmail || m.RecipientEmail == MeEmail))

              .OrderBy(m => m.SentDate).ToListAsync();



            if (!messages.Any()) return RedirectToAction(nameof(Messages));



            // Okunmadı işaretle

            var unread = messages.Where(m => m.RecipientEmail == MeEmail && !m.IsReadByRecipient);

            foreach (var m in unread) m.IsReadByRecipient = true;

            await _db.SaveChangesAsync();



            return View(messages);

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ReplyMessage(int conversationId, string content)

        {

            var starter = await _db.Message.FindAsync(conversationId);

            if (starter == null || string.IsNullOrWhiteSpace(content)) return RedirectToAction(nameof(Messages));



            string recipient = starter.SenderEmail == MeEmail ? starter.RecipientEmail : starter.SenderEmail;



            var reply = new Message

            {

                Subject = "Yanıt: " + starter.Subject,

                Content = content,

                SenderEmail = MeEmail,

                RecipientEmail = recipient,

                SentDate = DateTime.Now,

                ConversationId = conversationId,

                IsReadByRecipient = false

            };



            _db.Message.Add(reply);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MessageDetail), new { id = conversationId });

        }



        // ===================================

        // 5. REVIEWS

        // ===================================

        public async Task<IActionResult> Reviews()

        {

            var myProductIds = await GetMyProductsQuery().Select(p => p.Id).ToListAsync();

            var reviews = await _db.Reviews.Include(r => r.Product)

                           .Where(r => myProductIds.Contains(r.ProductId))

                           .OrderByDescending(r => r.ReviewDate).ToListAsync();

            return View(reviews);

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteReview(int id)

        {

            var review = await _db.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id);

            if (review != null && review.Product.SellerEmail == MeEmail)

            {

                _db.Reviews.Remove(review);

                await _db.SaveChangesAsync();

                TempData["ok"] = "Yorum silindi.";

            }

            return RedirectToAction(nameof(Reviews));

        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus newStatus, string? cargoCompany, string? trackingNumber)

        {

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();



            // Enum durumunu güncelle

            order.Status = newStatus;



            // Kargo bilgilerini güncelle (boş gelse bile kaydeder)

            order.CargoCompany = cargoCompany;

            order.TrackingNumber = trackingNumber;



            // Modelindeki string 'DeliveryStatus' alanını da eşitle

            order.DeliveryStatus = newStatus switch

            {

                OrderStatus.Alindi => "Alındı",

                OrderStatus.Hazirlaniyor => "Hazırlanıyor",

                OrderStatus.Kargolandı => "Kargolandı",

                OrderStatus.TeslimEdildi => "Teslim Edildi",

                OrderStatus.IptalEdildi => "İptal Edildi",

                _ => "Hazırlanıyor"

            };



            await _db.SaveChangesAsync();

            return RedirectToAction("Orders");

        }

        // ===================================

        // GIFT CARD MESSAGES (Hediye Kartı Mesajları)

        // ===================================

        public async Task<IActionResult> GiftCardMessages()
        {
            // 1. Şartı esnetiyoruz: Sadece satıcı eşleşsin, mesaj kontrolünü Select içinde yapacağız
            var giftMessages = await _db.OrderItems
                .Include(i => i.Order)
                .Where(i => i.SellerEmail == MeEmail)
                .Select(i => new SellerGiftCardViewModel
                {
                    OrderId = i.OrderId,
                    Name = i.Name,
                    Qty = i.Qty ?? 0,
                    // 2. ÇÖZÜM: OrderItem'daki mesaj boşsa, Orders tablosundaki (GeneralGiftMessage) mesajı al
                    GiftCardMessage = !string.IsNullOrWhiteSpace(i.GiftCardMessage)
                                      ? i.GiftCardMessage
                                      : i.Order.GeneralGiftMessage,

                    GeneralGiftMessage = i.Order.GeneralGiftMessage,
                    OrderDate = i.Order != null ? i.Order.CreatedAt : DateTime.Now,
                    OrderStatus = i.Order != null ? i.Order.Status : OrderStatus.Alindi
                })
                // 3. Filtreyi burada uygula: Her iki yerden de mesaj gelmiyorsa listeleme
                .Where(x => !string.IsNullOrWhiteSpace(x.GiftCardMessage) || !string.IsNullOrWhiteSpace(x.GeneralGiftMessage))
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return View(giftMessages);
        }
    }



}