using Microsoft.AspNetCore.Mvc;
using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Alışveriş sepeti işlemlerini (Ekleme, Silme, Güncelleme) yöneten kontrolcü.
    /// Sepet verileri kullanıcı oturumu (Session) içerisinde saklanır.
    /// </summary>
    public class CartController : Controller
    {
        // Session içerisinde sepet verisine ulaşmak için kullanılan anahtar kelime.
        private const string CART_KEY = "cart";

        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // SESSION (OTURUM) YARDIMCI METOTLARI
        // ============================================================

        /// <summary>
        /// Sepeti Session'dan okur. Eğer sepet boşsa yeni bir liste döner.
        /// </summary>
        private List<CartItem> Load()
            => HttpContext.Session.GetObj<List<CartItem>>(CART_KEY) ?? new();

        /// <summary>
        /// Güncel sepet listesini Session'a JSON formatında kaydeder.
        /// </summary>
        private void Save(List<CartItem> items)
            => HttpContext.Session.SetObj(CART_KEY, items);

        /// <summary>
        /// İşlem sonrası kullanıcıyı geldiği sayfaya geri gönderir.
        /// </summary>
        private IActionResult GoBackOr(string controller = "Cart", string action = "Index")
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Url.IsLocalUrl(referer))
                return Redirect(referer);

            return RedirectToAction(action, controller);
        }

        // ============================================================
        // SEPET İŞLEMLERİ (CRUD)
        // ============================================================

        /// <summary>
        /// Veri tabanından ürün bilgilerini çekerek sepete ekleme yapar.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int qty = 1)
        {
            // 1. Ürünün güncel fiyat ve bilgilerini veri tabanından (SQL) kontrol et.
            var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id);

            // Ürün bulunamazsa hata döndür.
            if (p == null) return NotFound();

            // Miktarı 1 ile 99 arasında sınırla.
            qty = Math.Clamp(qty, 1, 99);

            var cart = Load();
            // 2. Ürün sepette zaten var mı kontrol et.
            var line = cart.FirstOrDefault(x => x.ProductId == id);

            if (line == null)
            {
                // Sepette yoksa yeni bir sepet kalemi olarak ekle.
                cart.Add(new CartItem(p.Id, p.Name, p.ImageUrl, p.Price, qty));
            }
            else
            {
                // Varsa miktarını artır (Maksimum 99 olacak şekilde).
                line.Qty = Math.Min(99, line.Qty + qty);
            }

            Save(cart);
            TempData["ok"] = $"{p.Name} sepete eklendi.";
            return GoBackOr();
        }

        /// <summary>
        /// Belirli bir ürünü sepetten tamamen kaldırır.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var cart = Load();
            cart.RemoveAll(x => x.ProductId == id);
            Save(cart);
            TempData["ok"] = "Ürün sepetten kaldırıldı.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Sepetteki ürün miktarını el ile günceller.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int id, int qty)
        {
            var cart = Load();
            var line = cart.FirstOrDefault(x => x.ProductId == id);
            if (line == null) return RedirectToAction(nameof(Index));

            // Eğer miktar 0 veya altıysa ürünü sil, değilse miktarı güncelle.
            if (qty <= 0) cart.RemoveAll(x => x.ProductId == id);
            else line.Qty = Math.Clamp(qty, 1, 99);

            Save(cart);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Sepetteki tüm ürünleri temizler.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            Save(new List<CartItem>());
            TempData["ok"] = "Sepet temizlendi.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // GÖRÜNÜM VE BİLGİ METOTLARI
        // ============================================================

        /// <summary>
        /// Sepet sayfasını görüntüler ve toplam tutarı hesaplar.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var cart = Load();
            // Toplam tutar ve toplam ürün sayısını ViewBag ile View'a taşıyoruz.
            ViewBag.Total = cart.Sum(x => x.LineTotal);
            ViewBag.Count = cart.Sum(x => x.Qty);
            return View(cart);
        }

        /// <summary>
        /// AJAX istekleri için sepet sayısını döner. (Örn: Navbar'daki sepet ikonu güncellemesi)
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Count()
        {
            var n = Load().Sum(x => x.Qty);
            return Ok(new { count = n });
        }
    }
}