using Microsoft.AspNetCore.Mvc;
using ciceksepetim;
using ciceksepetim.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System;

namespace ciceksepetim.Controllers
{
    /// <summary>
    /// Ürün arama ve filtreleme işlemlerini yöneten kontrolcü.
    /// Büyük veri setlerinde performansı korumak için SQL seviyesinde sayfalama (Skip/Take) yapar.
    /// </summary>
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SearchController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Arama sonuçlarını getirir.
        /// </summary>
        /// <param name="q">Arama terimi</param>
        /// <param name="cat">Kategori slug bilgisi (opsiyonel)</param>
        /// <param name="page">Aktif sayfa numarası</param>
        /// <param name="pageSize">Sayfada gösterilecek ürün sayısı</param>
        [HttpGet]
        public async Task<IActionResult> Index(string q, string? cat, int page = 1, int pageSize = 12)
        {
            q ??= "";
            var term = q.Trim();

            // 1. ERTELENMİŞ SORGULAMA (IQueryable): 
            // Bu aşamada veri tabanına henüz gidilmez. Sadece SQL komutunun iskeleti oluşturulur.
            IQueryable<Product> query = _db.Products.AsQueryable();

            // 2. KATEGORİ FİLTRESİ:
            // Eğer bir kategori seçilmişse, sorguya "WHERE CategorySlug = ..." şartı eklenir.
            if (!string.IsNullOrWhiteSpace(cat))
            {
                query = query.Where(p => p.CategorySlug == cat);
            }

            // 3. ARAMA TERİMİ FİLTRESİ:
            if (!string.IsNullOrWhiteSpace(term))
            {
                // SQL LIKE '%term%' yapısına dönüşür.
                query = query.Where(p =>
                    p.Name.Contains(term) ||
                    p.Slug.Contains(term));
            }
            else
            {
                // Boş arama yapıldığında tüm ürünleri getirmek yerine boş liste dönmesi sağlanır.
                query = query.Where(p => false);
            }

            // 4. SIRALAMA:
            // Önce aranan kelimeyle başlayanları (en alakalılar), sonra alfabetik sırayı getirir.
            query = query.OrderByDescending(p => p.Name.StartsWith(term))
                         .ThenBy(p => p.Name);

            // 5. VIEWMODEL HAZIRLIĞI:
            var vm = new SearchVM
            {
                Q = term,
                Cat = cat,
                // Filtreleme menüsü için kategoriler veri tabanından asenkron çekilir.
                Categories = await _db.Categories.ToListAsync(),
                Page = Math.Max(1, page),
                PageSize = Math.Clamp(pageSize, 6, 48)
            };

            // 6. SAYFALAMA VE EXECUTION:
            // CountAsync() -> SQL'de "SELECT COUNT(*)" çalıştırarak toplam sonucu bulur.
            vm.Total = await query.CountAsync();

            var skip = (vm.Page - 1) * vm.PageSize;

            // Skip/Take -> SQL'de "OFFSET ... FETCH NEXT ..." yapısını kullanarak 
            // sadece o sayfada görünecek veriyi RAM'e yükler.
            vm.Results = await query
                .Skip(skip)
                .Take(vm.PageSize)
                .ToListAsync();

            return View(vm);
        }
    }
}