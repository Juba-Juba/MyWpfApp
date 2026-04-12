// ✅ Fix CS0234: استخدم نفس الـ namespace الرئيسي عشان ما تحتاجش using إضافي
namespace MyWpfApp
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PriceFormatted => Price.ToString("N2") + " ج.م";
        public string StockStatus => Stock > 10 ? "✅ متوفر" : Stock > 0 ? "⚠️ ينفد" : "❌ نفد";
    }

    public class AppVersion
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public DateTime ReleasedAt { get; set; }
    }
}
