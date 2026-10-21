namespace JogoDoGrupo.Shared.Features.Personagem
{
    /// <summary>
    /// Os volumes de água e as vias de escalada que valem no jogo: o servidor confere contra esta
    /// lista e o client usa a mesma. A água de rios e lagos do terreno não precisa estar aqui
    /// (vem de <c>MapaDoMundo</c>); esta lista é para água fora do terreno (poça de dungeon) e
    /// para as vias de escalada. Quem marca volumes na cena roda o menu do Unity
    /// <c>Jogo do Grupo/Personagem/Exportar volumes marcados</c>, que reescreve o trecho entre as
    /// marcas abaixo.
    /// </summary>
    public static class VolumesMarcados
    {
        public static readonly VolumeMarcado[] Todos = new VolumeMarcado[]
        {
            // INICIO-DOS-VOLUMES (gerado pelo menu do Unity; não edite à mão)
            // FIM-DOS-VOLUMES
        };
    }
}
