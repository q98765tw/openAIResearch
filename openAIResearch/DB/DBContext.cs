using Microsoft.EntityFrameworkCore;
using openAIResearch.DB.Model;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 定义你的表，例如：
    public virtual DbSet<user> users { get; set; }
    public virtual DbSet<Document> documents { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 指定表名
        modelBuilder.Entity<Document>()
            .ToTable("documents");

        // 配置向量欄位
        modelBuilder.Entity<Document>()
            .Property(d => d.Embedding)
            .HasColumnType("vector(1536)");

        // 配置索引
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Embedding)
            .HasDatabaseName("idx_embedding_vector")
            .HasMethod("ivfflat");
    }
}

