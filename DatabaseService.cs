using System.Data;
using Microsoft.Data.SqlClient;

// ✅ Fix CS0234 + CS0246: شيل الـ using MyWpfApp.Models
// لأن Models دلوقتي في namespace MyWpfApp نفسه
namespace MyWpfApp.Services
{
    public class DatabaseService
    {
        // ✏️ غيّر الـ Connection String دي حسب عندك
        private const string ConnectionString =
            @"Server=localhost;Database=MyWpfAppDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // =============================================
        // جلب المنتجات
        // =============================================
        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_GetActiveProducts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id        = reader.GetInt32("Id"),
                    Name      = reader.GetString("Name"),
                    Category  = reader.GetString("Category"),
                    Price     = reader.GetDecimal("Price"),
                    Stock     = reader.GetInt32("Stock"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }

            return products;
        }

        // =============================================
        // إضافة منتج
        // =============================================
        public async Task<int> AddProductAsync(Product product)
        {
            using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_AddProduct", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Name",     product.Name);
            cmd.Parameters.AddWithValue("@Category", product.Category);
            cmd.Parameters.AddWithValue("@Price",    product.Price);
            cmd.Parameters.AddWithValue("@Stock",    product.Stock);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // =============================================
        // تسجيل نشاط
        // ✅ Fix CS8625: string? بدل string عشان تقبل null
        // =============================================
        public async Task LogActivityAsync(string action, string? details = null)
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("sp_LogActivity", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@Action",  action);
                cmd.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* لا نوقف البرنامج بسبب خطأ في اللوج */ }
        }

        // =============================================
        // اختبار الاتصال
        // =============================================
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
