using Microsoft.AspNetCore.Mvc;
using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using System;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Siparişlerin listelenmesi, detaylandırılması ve yetki bazlı filtrelenmesini sağlar.
    /// [Authorize] özniteliği ile sadece giriş yapmış kullanıcılar erişebilir.
    /// </summary>
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Aktif kullanıcının e-posta adresini Claim yapısından çeker.
        private string MeEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // SİPARİŞ LİSTESİ (YETKİ BAZLI FİLTRELEME)
        // ============================================================
        public async Task<IActionResult> Index()
        {
            // Temel sorgu: En yeni sipariş en üstte olacak şekilde hazırlanır.
            IQueryable<Order> query = _db.Orders
                                         .OrderByDescending(o => o.CreatedAt)
                                         .AsQueryable();

            if (!User.Identity.IsAuthenticated || string.IsNullOrEmpty(MeEmail))
            {
                return View(new List<Order>());
            }

            // ROLE BAZLI SORGU YÖNETİMİ:
            if (User.IsInRole("Admin"))
            {
                // Admin: Tüm siparişleri görebilir, filtre uygulanmaz.
            }
            else if (User.IsInRole("Seller"))
            {
                // Satıcı: Sadece kendi ürünlerinin bulunduğu sipariş ID'lerini tespit eder.
                var sellerOrderIds = await _db.OrderItems
                                              .Where(oi => oi.SellerEmail == MeEmail)
                                              .Select(oi => oi.OrderId)
                                              .Distinct()
                                              .ToListAsync();

                query = query.Where(o => sellerOrderIds.Contains(o.Id));
            }
            else
            {
                // Müşteri: Sadece kendi e-posta adresiyle eşleşen siparişleri görür.
                query = query.Where(o => o.CustomerEmail == MeEmail);
            }

            var result = await query.ToListAsync();
            return View(result);
        }

        // ============================================================
        // SİPARİŞ DETAYI (GÜVENLİK KONTROLLÜ)
        // ============================================================
        public async Task<IActionResult> Detail(int id)
        {
            var o = await _db.Orders
                             .Include(o => o.Items)
                             .FirstOrDefaultAsync(o => o.Id == id);

            if (o == null) return NotFound();
            if (string.IsNullOrEmpty(MeEmail)) return Unauthorized();

            // GÜVENLİK BARİYERİ: Başkasının siparişine ID üzerinden erişim engellenir.
            if (User.IsInRole("Admin")) { /* Tam Yetki */ }
            else if (User.IsInRole("Seller"))
            {
                // Satıcı bu siparişin içinde en az bir ürününe sahip mi?
                bool hasSellerItem = o.Items.Any(item => item.SellerEmail == MeEmail);
                if (!hasSellerItem) return Forbid();
            }
            else if (o.CustomerEmail != MeEmail)
            {
                // Müşteri kendine ait olmayan siparişi göremez.
                return Forbid();
            }

            return View(o);
        }

        // ============================================================
        // SİPARİŞ OLUŞTURMA VE STOK YÖNETİMİ
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place([FromForm] PlaceOrderViewModel vm)
        {
            if (string.IsNullOrEmpty(MeEmail)) return Unauthorized();

            // 1. Session'dan sepet verilerini kontrol et.
            var cartItemsSession = HttpContext.Session.GetObj<List<CartItem>>("cart");
            if (cartItemsSession == null || !cartItemsSession.Any())
            {
                TempData["hata"] = "Sepetiniz boş.";
                return RedirectToAction("Index", "Cart");
            }

            // 2. Fiyat ve Kargo hesaplamalarını backend tarafında doğrula (Güvenlik için).
            decimal subtotal = cartItemsSession.Sum(i => i.UnitPrice * i.Qty);
            decimal shippingFee = (subtotal > 0 && subtotal < 500m) ? 49.90m : 0m;
            decimal finalTotal = subtotal + shippingFee;

            var cartItems = cartItemsSession.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                Name = x.Name,
                UnitPrice = x.UnitPrice,
                Qty = x.Qty,
                SellerEmail = _db.Products.Where(p => p.Id == x.ProductId).Select(p => p.SellerEmail).FirstOrDefault() ?? "satici@test.com"
            }).ToList();

            // 3. Sipariş nesnesini hazırla.
            var order = new Order
            {
                CustomerEmail = MeEmail,
                FullName = vm.FullName,
                Phone = vm.Phone,
                City = vm.City,
                District = vm.District,
                AddressLine1 = vm.AddressLine1,
                GeneralGiftMessage = vm.GeneralGiftMessage,
                CardHolderName = vm.CardHolderName,
                CardExpiration = vm.CardExpiration,
                CreatedAt = DateTime.Now,
                DeliveryScheduledTime = vm.DeliveryScheduledTime,
                Status = OrderStatus.Alindi,
                CargoCompany = "MNG Kargo",
                CardNumberMasked = vm.CardNumber,
                PaymentMethod = "Kredi Kartı",
                Subtotal = subtotal,
                Shipping = shippingFee,
                Total = finalTotal,
                Items = cartItems
            };

            // 4. TRANSACTIONAL STOK KONTROLÜ: Sipariş kaydedilmeden önce stoklar düşülür.
            foreach (var item in cartItems)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    if (product.Stock >= item.Qty)
                    {
                        product.Stock -= (int)item.Qty;
                    }
                    else
                    {
                        TempData["hata"] = $"{product.Name} ürününde yeterli stok kalmadı!";
                        return RedirectToAction("Index", "Cart");
                    }
                }
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 5. İşlem başarılı ise sepeti temizle.
            HttpContext.Session.Remove("cart");

            return RedirectToAction("Detail", new { id = order.Id });
        }
    }
}