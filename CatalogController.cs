using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ürünlerin listelenmesi, kategorize edilmesi ve detaylarının görüntülenmesini sağlayan kontrolcü.
/// </summary>
public class CatalogController : Controller
{
    private readonly ApplicationDbContext _db;

    public CatalogController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // KATEGORİ LİSTELEME
    // ============================================================
    public async Task<IActionResult> Category(string id)
    {
        // Gelen slug değerine (URL'deki kategori adı) göre ürünleri filtreler.
        var items = await _db.Products
                             .Where(p => p.CategorySlug == id)
                             .ToListAsync();

        // Kategori ismini View'da başlık olarak göstermek için kategori tablosundan çeker.
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Slug == id);
        ViewBag.Category = category?.Name ?? "Ürünler";

        return View(items);
    }

    // ============================================================
    // ÜRÜN DETAY SAYFASI
    // ============================================================
    [HttpGet]
    [Route("Catalog/Detail/{id}")]
    public async Task<IActionResult> Detail(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        ciceksepetim.Models.Product p = null;

        // ESNEK ARAMA MANTIĞI:
        // 1. Durum: Eğer gelen 'id' bir sayı ise doğrudan ID ile ara (Hızlı erişim).
        if (int.TryParse(id, out int numericId))
        {
            p = await _db.Products.FirstOrDefaultAsync(x => x.Id == numericId);
        }

        // 2. Durum: Eğer sayı ile bulunamadıysa Slug (SEO URL) ile ara.
        if (p == null)
        {
            p = await _db.Products.FirstOrDefaultAsync(x => x.Slug == id);
        }

        if (p == null) return NotFound();

        // Ürüne ait yorumları getirir.
        var reviews = await _db.Reviews.Where(r => r.ProductId == p.Id).ToListAsync();

        // İLGİLİ ÜRÜNLER: Aynı kategorideki diğer ürünlerden rastgele 3 tanesini seçer.
        var related = await _db.Products
                               .Where(x => x.CategorySlug == p.CategorySlug && x.Id != p.Id)
                               .Take(3)
                               .ToListAsync();

        // Ürün Meta verilerini (teknik özellikler vb.) statik sınıftan veya sözlükten çeker.
        string finalDesc = "Açıklama yok.";
        List<string> finalSpecs = new List<string>();

        if (ProductMeta.Descriptions.ContainsKey(p.Id))
        {
            var meta = ProductMeta.Descriptions[p.Id];
            finalDesc = meta.desc;
            finalSpecs = meta.specs ?? new List<string>();
        }

        // ViewModel kullanarak View'a birden fazla farklı veri seti (Ürün, Yorumlar, Benzerler) gönderilir.
        var vm = new ProductDetailViewModel
        {
            Product = p,
            Description = finalDesc,
            Specs = finalSpecs,
            Reviews = reviews,
            Related = related
        };
        return View(vm);
    }

    // ============================================================
    // YORUM EKLEME (KULLANICI ETKİLEŞİMİ)
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int productId, int rating, string text)
    {
        // Kullanıcının oturum açıp açmadığını ClaimTypes.Email üzerinden kontrol eder.
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userEmail))
        {
            TempData["hata"] = "Yorum yapabilmek için lütfen giriş yapın.";
            return RedirectToAction("Detail", new { id = productId });
        }

        var p = await _db.Products.FindAsync(productId);
        if (p == null) return NotFound();

        // Yeni yorum nesnesini oluşturur ve puanlamayı 1-5 arasında kısıtlar.
        var newReview = new Review
        {
            ProductId = productId,
            UserEmail = userEmail,
            Rating = Math.Clamp(rating, 1, 5),
            Comment = (text ?? "").Trim(),
            ReviewDate = DateTime.Now
        };

        _db.Reviews.Add(newReview);
        await _db.SaveChangesAsync(); // Veri tabanına kalıcı olarak kaydeder.

        TempData["ok"] = "Yorum eklendi. Teşekkürler!";
        return RedirectToAction("Detail", new { id = p.Id });
    }
}