using Microsoft.AspNetCore.Mvc;
//using ciceksepetim.Data; // ApplicationDbContext için gerekli (varsayalım)
using ciceksepetim.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ciceksepetim.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ciceksepetim.Controllers/HomeController.cs dosyasındaki Index metodu
        public async Task<IActionResult> Index()
        {
            // 1. Kategori Sorgusu: Tüm Kategorileri veritabanından çeker
            var sqlCategories = await _db.Categories
                                         .OrderBy(c => c.Name)
                                         .ToListAsync();

            // 2. KRİTİK DÜZELTME: Ürün Sorgusu: Popüler ürünleri (veya ilk 5 ürünü) veritabanından çeker
            // Tablonuz Product olarak adlandırıldıysa Product'ı kullanın
            var sqlProducts = await _db.Products
                                       .Take(5) // Ana sayfada sadece ilk 5 ürünü gösterelim
                                       .ToListAsync();

            var vm = new HomeViewModel
            {
                // Kategoriler: Veritabanından gelen Category modelini CategoryLink record'una dönüştürür.
                Categories = sqlCategories.Select(c => new CategoryLink(c.Name, c.Slug)).ToList(),

                // KRİTİK DÜZELTME: Ürünler listesini HomeViewModel'e ekler.
                Products = sqlProducts,

                // Banners statik kaldığı için aynen korundu
                Banners = new()
                {
                    // ... Mevcut banner tanımlamaları (Aynı kalır) ...
                    new("Yeni Ürünler","Aynı gün teslimat",
                        "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?q=80&w=1200&auto=format&fit=crop",
                        size:"xl", theme:"magenta"),
                    new("Gurme Lezzetler","Aynı gün teslimat",
                        "https://images.unsplash.com/photo-1570197788417-0e82375c9371?q=80&w=1200&auto=format&fit=crop",
                        size:"xl", theme:"blue"),
                    new("Premium Çiçekler","Aynı gün teslim",
                        "https://images.unsplash.com/photo-1526178613298-8694fd12d059?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"green"),
                    new("Yenilebilir Çiçekler","Lotus Biscoff’lu",
                        "https://images.unsplash.com/photo-1504754524776-8f4f37790ca0?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"orange"),
                    new("Yeni İş Çiçekleri","Aynı gün teslim",
                        "https://images.unsplash.com/photo-1498837167922-ddd27525d352?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"red"),
                    new("Geçmiş Olsun","Yanındayız",
                        "https://images.unsplash.com/photo-1496782850948-75ba482b4a2f?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"teal"),
                    new("Premium Kekler","Aynı gün teslim",
                        "https://images.unsplash.com/photo-1499636136210-6f4ee915583e?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"violet"),
                    new("Çok Satanlar","Yenilebilir",
                        "https://images.unsplash.com/photo-1519681393784-d120267933ba?q=80&w=1200&auto=format&fit=crop",
                        size:"md", theme:"purple"),
                }
            };
            return View(vm);
        }
    }
}