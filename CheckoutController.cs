using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ciceksepetim.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Ödeme ve sipariş tamamlama süreçlerini yöneten kontrolcü.
    /// Sepetteki geçici verileri (Session), kalıcı sipariş verilerine (SQL) dönüştürür.
    /// </summary>
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CART_KEY = "cart";

        public CheckoutController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // YARDIMCI METOTLAR
        // ============================================================

        private List<CartItem> LoadCart() => HttpContext.Session.GetObj<List<CartItem>>(CART_KEY) ?? new();

        private void SaveCart(List<CartItem> items) => HttpContext.Session.SetObj(CART_KEY, items);

        /// <summary>
        /// Sepetin boş olup olmadığını kontrol eder. Boşsa kullanıcıyı sepete geri gönderir.
        /// </summary>
        private bool EnsureCartNotEmpty(out IActionResult? redirect)
        {
            var cart = LoadCart();
            if (cart.Count == 0)
            {
                TempData["ok"] = "Sepetiniz boş. Sipariş veremezsiniz.";
                redirect = RedirectToAction("Index", "Cart");
                return false;
            }
            redirect = null;
            return true;
        }

        // ============================================================
        // ÖDEME ADIMLARI
        // ============================================================

        /// <summary>
        /// Ödeme sayfasını (Özet ve Form) görüntüler.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            if (!EnsureCartNotEmpty(out var red)) return red!;

            // GÜVENLİK: Kullanıcı giriş yapmamışsa ödeme sayfasına giremez.
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ok"] = "Sipariş vermek için lütfen giriş yapın.";
                // returnUrl: Giriş yaptıktan sonra kaldığı bu sayfaya otomatik döner.
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Checkout") });
            }

            var cart = LoadCart();

            // Hesaplamalar: 500 TL üzeri kargo bedava mantığı.
            ViewBag.Subtotal = cart.Sum(x => x.LineTotal);
            ViewBag.Shipping = ((decimal)ViewBag.Subtotal >= 500m) ? 0m : 49.90m;
            ViewBag.Total = (decimal)ViewBag.Subtotal + (decimal)ViewBag.Shipping;

            return View(cart);
        }

        /// <summary>
        /// Formdan gelen verilerle siparişi SQL tablosuna kaydeder.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Place(
            DateTime? DeliveryScheduledTime,
            string CardHolderName,
            string CardNumber,
            string CardExpiration,
            string CardCvv,
            string GeneralGiftMessage)
        {
            if (!EnsureCartNotEmpty(out var red)) return red!;

            // Kullanıcı e-posta kontrolü
            var customerEmail = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
            {
                TempData["Error"] = "Sipariş vermek için lütfen önce giriş yapın.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index), "Checkout") });
            }

            var cart = LoadCart();
            var subtotal = cart.Sum(x => x.LineTotal);
            var shipping = (subtotal >= 500m) ? 0m : 49.90m;
            var total = subtotal + shipping;

            // Kart Numarası Validasyonu (Basit Kontrol)
            if (string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 16)
            {
                TempData["Error"] = "Lütfen geçerli bir 16 haneli kart numarası girin.";
                return RedirectToAction(nameof(Index));
            }

            // PERFORMANS: Ürünlerin satıcılarını tek bir sorgu ile sözlük (Dictionary) yapısına alır.
            var productIds = cart.Select(x => x.ProductId).ToList();
            var productsData = await _db.Products
                                         .Where(p => productIds.Contains(p.Id))
                                         .Select(p => new { p.Id, p.SellerEmail })
                                         .ToDictionaryAsync(p => p.Id, p => p.SellerEmail);

            // 1. ADIM: Sipariş Master Kaydı (Ana sipariş bilgileri)
            var order = new Order
            {
                CustomerName = User?.Identity?.Name ?? "Müşteri",
                CustomerEmail = customerEmail,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Alindi,

                // Form Verileri ve Demo Bilgiler
                FullName = "DEMO Adı", // Burası formdan gelen modelle değiştirilebilir
                AddressLine1 = "DEMO Adres",
                City = "İstanbul",
                Phone = "5551234567",
                District = "DEMO İlçe",
                PaymentMethod = "Kredi Kartı",
                ItemCount = cart.Sum(x => x.Qty),

                Total = total,
                Subtotal = subtotal,
                Shipping = shipping,

                // Özel İstekler ve Ödeme Bilgileri
                DeliveryScheduledTime = DeliveryScheduledTime,
                GeneralGiftMessage = GeneralGiftMessage,
                CardHolderName = CardHolderName,
                CardExpiration = CardExpiration,

                // GÜVENLİK: Kartın tamamı değil, sadece son 4 hanesi maskelenerek kaydedilir. (PCI-DSS uyumu için)
                CardNumberMasked = "**** **** **** " + CardNumber.Substring(CardNumber.Length - 4),
                DeliveryStatus = "Hazırlanıyor"
            };

            // 2. ADIM: Sipariş Detayları (Ürün bazlı satırlar)
            order.Items = cart.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                Name = x.Name,
                ImageUrl = x.ImageUrl,
                UnitPrice = x.UnitPrice,
                Qty = x.Qty,
                SellerEmail = productsData.GetValueOrDefault(x.ProductId) ?? "",
                GiftCardMessage = x.GiftCardMessage // Her ürünün kendi hediye notu
            }).ToList();

            // 3. ADIM: Veri Tabanı İşlemi
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // 4. ADIM: Temizlik (Sipariş bittiği için sepet boşaltılır)
            SaveCart(new List<CartItem>());

            TempData["ok"] = "Siparişiniz başarıyla alındı.";
            return RedirectToAction(nameof(Success), new { id = order.Id });
        }

        /// <summary>
        /// Sipariş onay sayfasını görüntüler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var o = await _db.Orders
                             .Include(o => o.Items)
                             .FirstOrDefaultAsync(x => x.Id == id);

            if (o == null) return NotFound();
            return View(o);
        }
    }
}