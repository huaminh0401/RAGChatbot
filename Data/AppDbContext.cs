using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<EmbeddingResearch> EmbeddingResearch => Set<EmbeddingResearch>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subject>().ToTable("Subjects");
        modelBuilder.Entity<Document>().ToTable("Documents");
        modelBuilder.Entity<DocumentChunk>().ToTable("DocumentChunks");
        modelBuilder.Entity<EmbeddingResearch>().ToTable("EmbeddingResearch");
        modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUsers");
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.TokenHash)
            .IsUnique();

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Subject)
            .WithMany(s => s.Documents)
            .HasForeignKey(d => d.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentChunk>()
            .HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmbeddingResearch>()
            .HasOne(e => e.Chunk)
            .WithMany(c => c.Embeddings)
            .HasForeignKey(e => e.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.ApplicationUser)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
