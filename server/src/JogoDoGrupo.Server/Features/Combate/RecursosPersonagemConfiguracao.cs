using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonagemEntidade = JogoDoGrupo.Server.Features.Personagem.Personagem;

namespace JogoDoGrupo.Server.Features.Combate;

internal sealed class RecursosPersonagemConfiguracao : IEntityTypeConfiguration<RecursosPersonagem>
{
    public void Configure(EntityTypeBuilder<RecursosPersonagem> recursos)
    {
        recursos.ToTable("RecursosPersonagem");
        recursos.HasKey(r => r.PersonagemId);
        recursos.Property(r => r.PersonagemId).ValueGeneratedNever();
        recursos
            .HasOne<PersonagemEntidade>()
            .WithMany()
            .HasForeignKey(r => r.PersonagemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
