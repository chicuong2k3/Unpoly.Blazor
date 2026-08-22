namespace Jubin.Data;

public record Product(
    int Id,
    string Slug,
    string Name,
    decimal Price,
    decimal? SalePrice,
    string Category,
    string Collection)
{
    public decimal Effective => SalePrice ?? Price;
    public bool OnSale => SalePrice is not null;
}

public record Category(string Slug, string Name);

/// <summary>
/// In-memory fake data. Deliberately no database: this sample teaches Unpoly,
/// not EF Core.
/// </summary>
public static class Catalog
{
    public static readonly Category[] Categories =
    [
        new("dam",    "Đầm"),
        new("ao",     "Áo & Corset"),
        new("quan",   "Quần & Chân váy"),
        new("set",    "Set đồ"),
    ];

    public static readonly (string Slug, string Name)[] Collections =
    [
        ("spring-summer-26", "Spring Summer '26"),
        ("the-angel-diary",  "The Angel Diary"),
    ];

    public static readonly string[] Sizes = ["XS", "S", "M", "L"];

    public static readonly IReadOnlyList<Product> Products = Build();

    /// <summary>
    /// Version of the catalog. A real app would derive this from the data — a row version,
    /// a MAX(updated_at), a hash. Bumped by Touch() so the 304 path can be exercised.
    /// </summary>
    public static DateTimeOffset LastModified { get; private set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static string ETag => $"\"catalog-{LastModified.ToUnixTimeSeconds()}\"";

    public static void Touch() => LastModified = DateTimeOffset.UtcNow;

    static Product[] Build()
    {
        string[] damNames  = ["Đầm Lụa Ánh Trăng", "Đầm Xoè Tiểu Thư", "Đầm Ôm Cổ Vuông", "Đầm Hai Dây Bèo", "Đầm Sơ Mi Dáng A", "Đầm Voan Hoa Nhí", "Đầm Cúp Ngực Nơ", "Đầm Suông Tay Phồng"];
        string[] aoNames   = ["Corset Ren Trắng", "Áo Croptop Cổ Tim", "Áo Sơ Mi Tay Bồng", "Corset Satin Đen", "Áo Thun Basic Petite", "Áo Kiểu Cổ Yếm", "Corset Nhung Đỏ"];
        string[] quanNames = ["Quần Ống Suông Lưng Cao", "Chân Váy Xếp Ly", "Quần Short Vintage", "Chân Váy Bút Chì", "Quần Jean Baggy Petite", "Chân Váy Xoè Ren", "Quần Culottes Linen"];
        string[] setNames  = ["Set Áo Quần Kẻ Sọc", "Set Corset & Chân Váy", "Set Blazer Croptop", "Set Đồ Ngủ Lụa", "Set Áo Váy Denim", "Set Cardigan & Váy", "Set Thể Thao Nữ", "Set Công Sở Petite"];

        var groups = new (string Cat, string[] Names)[]
        {
            ("dam", damNames), ("ao", aoNames), ("quan", quanNames), ("set", setNames),
        };

        var list = new List<Product>();
        var id = 1;
        foreach (var (cat, names) in groups)
        {
            foreach (var name in names)
            {
                var price = 250_000m + (id * 37_000m % 700_000m);
                var onSale = id % 4 == 0;
                list.Add(new Product(
                    Id: id,
                    Slug: $"{cat}-{id}",
                    Name: name,
                    Price: Math.Round(price / 1000) * 1000,
                    SalePrice: onSale ? Math.Round(price * 0.7m / 1000) * 1000 : null,
                    Category: cat,
                    Collection: Collections[id % 2].Slug));
                id++;
            }
        }
        return [.. list];
    }

    public static IEnumerable<Product> ByCategory(string? categorySlug) =>
        string.IsNullOrEmpty(categorySlug)
            ? Products
            : Products.Where(p => p.Category == categorySlug);

    public static Product? BySlug(string slug) => Products.FirstOrDefault(p => p.Slug == slug);

    public static string CategoryName(string slug) =>
        Categories.FirstOrDefault(c => c.Slug == slug)?.Name ?? slug;
}
