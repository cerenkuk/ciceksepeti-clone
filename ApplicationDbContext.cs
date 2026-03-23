using Microsoft.EntityFrameworkCore;
using ciceksepetim.Models;

namespace ciceksepetim
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Müşteri ve Hesap Bilgileri
        public DbSet<AppUser> AppUsers { get; set; } = default!;
        public DbSet<SavedAddress> SavedAddresses { get; set; } = default!;
        public DbSet<SavedCard> SavedCards { get; set; } = default!;
        public DbSet<Message> Message { get; set; } = default!;

        // Ürün ve İçerik
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;
        public DbSet<Review> Reviews { get; set; } = default!;

        // Sipariş Bilgileri
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderItem> OrderItems { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>()
        .ToTable(tb => tb.HasTrigger("trg_ProductStockHistory"))
        .ToTable(tb => tb.HasTrigger("trg_LowStockWarning_Message"));

            // Orders tablosunda trigger olduğunu EF Core'a bildir
            modelBuilder.Entity<Order>()
                .ToTable(tb => tb.HasTrigger("trg_UpdateOrderTimestamp"));

            // Messages tablosunda trigger olduğunu EF Core'a bildir
            modelBuilder.Entity<Message>()
                .ToTable(tb => tb.HasTrigger("trg_SecureMessageArchive"));
            // OrderItem tablosu için trigger olduğunu belirtiyoruz
            modelBuilder.Entity<OrderItem>()
                .ToTable(tb => tb.HasTrigger("trg_DecreaseStockOnOrder"));
            modelBuilder.Entity<Order>()
        .ToTable(tb => tb.HasTrigger("trg_UpdateOrderTimestamp"));
            // **********************************
            // 🔥 KRİTİK REVİZYON: SQL TABLO EŞLEŞTİRMELERİ 
            // **********************************
            modelBuilder.Entity<AppUser>().ToTable("AppUsers");
            modelBuilder.Entity<SavedAddress>().ToTable("SavedAddresses");
            modelBuilder.Entity<SavedCard>().ToTable("SavedCards");
            modelBuilder.Entity<Message>().ToTable("Message");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderItem>().ToTable("OrderItems");

            // **********************************
            // MODEL KURALLARI VE İLİŞKİLER
            // **********************************

            // 🔥 KRİTİK ÇÖZÜM: CustomerEmail Dış Anahtar İlişkisini Koparma
            // Orders tablosunda, AppUsers'a bağlanmasını engelliyoruz.
            // Bu, EF Core'un AppUsers'a Dış Anahtar kısıtlaması eklemesini engeller.
            modelBuilder.Entity<Order>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(o => o.CustomerEmail)
                .HasPrincipalKey(u => u.Email)
                .IsRequired(false) // Bu ilişki zorunlu değil
                .OnDelete(DeleteBehavior.NoAction); // Silme işlemi yapılmasın

            // UpdatedAt Kuralı (Bu, SQL'de UPDATE'de otomatik zaman ataması sağlar)
            modelBuilder.Entity<Order>()
                .Property(o => o.UpdatedAt)
                .HasDefaultValueSql("GETDATE()") // SQL'de ilk eklendiğinde değer atar
                .ValueGeneratedOnAddOrUpdate(); // Her güncellemede değer üretilmesini sağlar

            // Order - OrderItem İlişkisi
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order: CustomerEmail Index'i (Hızlı arama için)
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CustomerEmail);

            // AppUser: Email'i benzersiz ve hızlı sorgulanabilir yapalım
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Product: Slug'ı benzersiz ve hızlı sorgulanabilir yapalım
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            // Category: Slug'ı benzersiz yapalım
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();
            // Category - Product İlişkisi (Yeni Bağlantı)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category) // Her ürünün bir kategorisi vardır
                .WithMany(c => c.Products) // Bir kategorinin birçok ürünü olabilir
                .HasForeignKey(p => p.CategorySlug) // Ürün tablosundaki bağlayıcı alan
                .HasPrincipalKey(c => c.Slug) // Kategori tablosundaki hedef alan (Slug üzerinden bağladığın için)
                .OnDelete(DeleteBehavior.SetNull); // Kategori silinirse ürünler silinmesin, kategorisiz kalsın
        }
    }
}