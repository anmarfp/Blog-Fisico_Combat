using FisicoCombat.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FisicoCombat.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<Usuario>(options)
{
    public DbSet<Modalidade> Modalidades => Set<Modalidade>();
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<Postagem> Postagens => Set<Postagem>();
    public DbSet<Comentario> Comentarios => Set<Comentario>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Modalidade>(e =>
        {
            e.HasIndex(m => m.Slug).IsUnique();
            e.HasIndex(m => m.Nome).IsUnique();
        });

        builder.Entity<Professor>()
            .HasMany(p => p.Modalidades)
            .WithMany(m => m.Professores)
            .UsingEntity(j => j.ToTable("ProfessorModalidade"));

        builder.Entity<Postagem>(e =>
        {
            e.HasIndex(p => p.PublicadaEm);
            // Excluir uma modalidade não apaga as postagens; elas ficam sem modalidade.
            e.HasOne(p => p.Modalidade).WithMany(m => m.Postagens)
                .HasForeignKey(p => p.ModalidadeId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Autor).WithMany()
                .HasForeignKey(p => p.AutorId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Comentario>(e =>
        {
            e.HasIndex(c => new { c.PostagemId, c.Status });
            e.HasOne(c => c.Postagem).WithMany(p => p.Comentarios)
                .HasForeignKey(c => c.PostagemId).OnDelete(DeleteBehavior.Cascade);
            // LGPD: ao excluir a conta, os comentários do usuário também são excluídos.
            e.HasOne(c => c.Usuario).WithMany(u => u.Comentarios)
                .HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
