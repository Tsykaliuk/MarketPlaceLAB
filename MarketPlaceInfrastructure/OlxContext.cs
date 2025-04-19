using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MarketPlaceDomain.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarketPlaceInfrastructure;

public class OlxContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public OlxContext(DbContextOptions<OlxContext> options)
        : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("categories");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("(NEWID())");

            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProductId });
            entity.ToTable("favorites");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("nvarchar(450)");
            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .HasColumnType("nvarchar(450)");

            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("GETDATE()")
                .HasColumnType("datetime")
                .HasColumnName("added_at");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Favorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("images");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.ProductId)
                .HasColumnName("product_id");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("orders");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ProductId)
                .HasColumnName("product_id")
                .HasColumnType("nvarchar(450)");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("nvarchar(450)");

            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasConversion(new EnumToStringConverter<OrderStatus>())
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_price");

            entity.HasOne(d => d.Product)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("products");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("NEWID()");
            entity.Property(e => e.CategoryId)
                .HasColumnName("category_id")
                .HasColumnType("nvarchar(450)");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("nvarchar(450)");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(max)")
                .HasColumnName("description");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasConversion(new EnumToStringConverter<ProductStatus>())
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.Category)
                .WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.User)
                .WithMany(p => p.Products)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("user_roles");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<ApplicationRole>().HasData(
            new ApplicationRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
            new ApplicationRole { Id = "2", Name = "User", NormalizedName = "USER" }
        );
    }
}
