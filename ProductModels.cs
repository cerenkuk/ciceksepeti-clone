using System.Text.Json.Serialization;

namespace ciceksepetim.Models
{
    // Category (AYNI KALIYOR, kurucusu Program.cs'de kullanılıyor)
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public Category() { }
        public Category(string name, string slug)
        {
            Name = name;
            Slug = slug;
        }
    }

    // KRİTİK REVİZYON: Product sınıfı (Parametreli kurucu SİLİNDİ)
    public class Product
    {
        public int Id { get; set; } // Identity
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public string CategorySlug { get; set; } = "";
        public string SellerEmail { get; set; } = "";
        public int? Stock { get; set; }
        public decimal? AverageRating { get; set; }
        // SADECE BOŞ KURUCU KALMALI (CS1729 hatalarını çözmek için)
          public virtual Category? Category { get; set; }
        public Product() { }

        // Parametreli kurucu kalıcı olarak kaldırıldı.
        // public Product(string name, string slug, string imageUrl, decimal price, string categorySlug) { ... } 
    }

    // CartItem (AYNI KALIYOR)
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; }
        public string GiftCardMessage { get; set; } = ""; // Hediye mesajı

        // Hesaplanan Property
        public decimal LineTotal => UnitPrice * Qty;


        // 🔥 1. Adım: Session'dan okuma (deserileştirme) için boş constructor'ı işaretleyin.
        // Bu, System.Text.Json'ın parametresiz constructor'ı kullanmasını ve property'lere değer atamasını sağlar.
        // Bu, aldığınız InvalidOperationException hatasını çözer.
        [JsonConstructor]
        public CartItem() { }


        // 🔥 2. Adım: Yeni ürün ekleme (CartController.Add) sırasında kullanılan constructor.
        // Bu constructor artık JSON serileştirme tarafından otomatik kullanılmayacağı için
        // parametre adlarının Property adlarıyla birebir eşleşmeme sorunu kalkar.
        public CartItem(int id, string name, string imageUrl, decimal price, int qty)
        {
            ProductId = id;
            Name = name;
            ImageUrl = imageUrl;
            UnitPrice = price;
            Qty = qty;
        }
    }
    // Basit seed
    public static class CatalogSeed
    {
        // Kategoriler (AYNI KALIYOR)
        public static readonly List<Category> Categories = new()
        {
            new("Çiçek","cicek"),
            new("Yenilebilir Çiçek","yenilebilir"),
            new("Orkide / Saksı","orkide"),
            new("Kek & Çikolata","kek"),
        };

        // KRİTİK REVİZYON: Product listesi (Object Initializer kullanılıyor ve Id kaldırıldı)
        public static readonly List<Product> Products = new()
        {
            new Product { Name = "Kırmızı Gül Buketi", Slug = "kirmizi-gul", ImageUrl = "https://www.google.com/url?sa=i&url=https%3A%2F%2Fwww.ciceksepeti.com%2Faskin-adi-21-adet-kirmizi-gul-aranjmani-at3100&psig=AOvVaw0w5i5j_5p4LAIt4Arlrubl&ust=1762599148542000&source=images&cd=vfe&opi=89978449&ved=0CBIQjRxqFwoTCJC_qtPv35ADFQAAAAAdAAAAABAE", Price = 799.90m, CategorySlug = "cicek" },
            new Product { Name = "Beyaz Orkide", Slug = "beyaz-orkide", ImageUrl = "https://www.google.com/imgres?q=beyaz%20ork%C4%B1de&imgurl=https%3A%2F%2Fcdn03.ciceksepeti.com%2Fcicek%2Fat773-1%2FL%2F2-dal-beyaz-orkide-cicegi-at773-380d9eaf-3ef8-41e4-b256-41833e4352ad.jpg&imgrefurl=https%3A%2F%2Fwww.ciceksepeti.com%2Fseramik-vazoda-phalaenopsis-orkide&docid=T-9d3qhBOsSBtM&tbnid=QXzePeXKlAzvhM&vet=12ahUKEwiHlbbl79-QAxWVVfEDHf4lH6cQM3oECBoQAA..i&w=582&h=640&hcb=2&ved=2ahUKEwiHlbbl79-QAxWVVfEDHf4lH6cQM3oECBoQAA", Price = 1299.00m, CategorySlug = "orkide" },
            new Product { Name = "Papatya Aranjman", Slug = "papatya", ImageUrl = "https://images.unsplash.com/photo-1526045612212-70caf35c14df?q=80&w=1200&auto=format&fit=crop", Price = 449.50m, CategorySlug = "cicek" },
            new Product { Name = "Sukulent Set", Slug = "sukulent", ImageUrl = "https://www.google.com/url?sa=i&url=https%3A%2F%2Fwww.kaktussukulent.com%2Fkucuk-sukulentler&psig=AOvVaw0Xa34xJ3K2nmnwya4T_lUr&ust=1762599269397000&source=images&cd=vfe&opi=89978449&ved=0CBUQjRxqFwoTCPi2q4zw35ADFQAAAAAdAAAAABAE", Price = 349.90m, CategorySlug = "orkide" },
            new Product { Name = "Çikolata Kutusu", Slug = "cikolata", ImageUrl = "https://www.google.com/imgres?q=%C3%A7%C3%A7ek%20kutusu&imgurl=https%3A%2F%2Fwww.ankaracicekcisi.com%2Fresimler%2Fb_13_2.jpg&imgrefurl=https%3A%2F%2Fwww.ankaracicekcisi.com%2Fcicek-ask-cicek-kutusu&docid=Vj0HHlPaBbYGTM&tbnid=jzh9rtIDJvIHZM&vet=12ahUKEwjE2ZSc8N-QAxUKRfEDHYPeI_EQM3oECBcQAA..i&w=600&h=600&hcb=2&ved=2ahUKEwjE2ZSc8N-QAxUKRfEDHYPeI_EQM3oECBcQAA", Price = 589.90m, CategorySlug = "kek" },
        };
    }

public static class ProductMeta
    {
        public static readonly Dictionary<int, (string desc, List<string> specs)> Descriptions = new()
         {
             { 1, ("Aşkı anlatmanın klasiği: taptaze kırmızı güller.",
                 new(){ "11 adet kırmızı gül","Yaklaşık 35–40 cm","Not kartı hediyeli"}) },
             { 2, ("Zarafetiyle öne çıkan beyaz orkide.",
                 new(){ "Tek dal Phalaenopsis","Seramik saksı","Bakım kartı dahil"}) }
         };
    }
}