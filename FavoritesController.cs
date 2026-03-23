using Microsoft.AspNetCore.Mvc;
using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Kullanıcıların beğendiği ürünleri (Favoriler) yöneten kontrolcü.
    /// Favori ürün ID'leri oturum bazlı (HashSet) saklanır, detaylar SQL'den çekilir.
    /// </summary>
    public class FavoritesController : Controller
    {
        // Session (Oturum) anahtarı
        private const string FAV_KEY = "favs";

        private readonly ApplicationDbContext _db;

        public FavoritesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ============================================================
        // SESSION YARDIMCILARI
        // ============================================================

        /// <summary>
        /// Favori ID'lerini Session'dan benzersiz bir küme (HashSet) olarak yükler.
        /// HashSet kullanımı, 'var mı?' kontrolünü O(1) hızında yaptığı için performanslıdır.
        /// </summary>
        private HashSet<int> Load()
            => HttpContext.Session.GetObj<HashSet<int>>(FAV_KEY) ?? new HashSet<int>();

        private void Save(HashSet<int> set)
            => HttpContext.Session.SetObj(FAV_KEY, set);

        // ============================================================
        // FAVORİ YÖNETİMİ (TOGGLE MANTIĞI)
        // ============================================================

        /// <summary>
        /// Ürünü favorilere ekler veya zaten varsa listeden çıkarır (Toggle).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            // GÜVENLİK: Eklenmek istenen ürün gerçekten veri tabanında var mı?
            if (!await _db.Products.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            var favs = Load();

            // HashSet.Add() metodu: Eleman yoksa ekler true döner, varsa eklemez false döner.
            var added = favs.Add(id);
            if (!added)
            {
                favs.Remove(id); // Ürün zaten favorilerdeymiş, o halde listeden çıkar.
            }

            Save(favs);
            TempData["ok"] = added ? "Ürün favorilere eklendi." : "Favorilerden çıkarıldı.";

            // Kullanıcıyı geldiği sayfaya geri gönderir.
            var referer = Request.Headers["Referer"].ToString();
            return string.IsNullOrWhiteSpace(referer)
                ? RedirectToAction("Index")
                : Redirect(referer);
        }

        // ============================================================
        // FAVORİ LİSTESİ GÖRÜNTÜLEME
        // ============================================================

        /// <summary>
        /// Session'daki ID'leri kullanarak veri tabanından ürün detaylarını getirir.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var favs = Load();

            // SQL Sorgusu: 'WHERE ID IN (...)' mantığıyla sadece favorilenmiş ürünleri getirir.
            var items = await _db.Products
                                 .Where(p => favs.Contains(p.Id))
                                 .ToListAsync();

            return View(items);
        }
    }
}