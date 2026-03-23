namespace ciceksepetim.Models
{
    /// <summary>
    /// Bir siparişin yaşam döngüsü boyunca geçebileceği aşamaları temsil eder.
    /// Veritabanında tamsayı (int) olarak saklanır, kod içerisinde isimlendirilmiş sabitler olarak kullanılır.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary> Ödeme onaylandı, sipariş satıcı ekranına düştü. </summary>
        Alindi = 0,

        /// <summary> Satıcı siparişi onayladı ve ürün/çiçek hazırlanma aşamasında. </summary>
        Hazirlaniyor = 1,

        /// <summary> Ürün kuryeye veya kargo firmasına teslim edildi, takip numarası girildi. </summary>
        Kargolandı = 2,

        /// <summary> Ürün alıcıya ulaştı ve süreç başarıyla tamamlandı. </summary>
        TeslimEdildi = 3,

        /// <summary> Müşteri veya satıcı tarafından herhangi bir sebeple durdurulan süreç. </summary>
        IptalEdildi = 4
    }
}