using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// A barra como o servidor a mandou, com a recarga contada no relógio local a partir do
    /// que faltava no instante da mensagem. Não decide nada: o servidor recusa o uso em recarga.
    /// </summary>
    internal sealed class EstadoDaBarra
    {
        private readonly double[] _prontaEm = new double[ConfigSkills.SkillsPorArma];
        private SkillDaBarra[] _skills = new SkillDaBarra[ConfigSkills.SkillsPorArma];

        public long ArmaId { get; private set; }

        /// <summary>Se a barra já chegou do servidor ao menos uma vez.</summary>
        public bool Recebida { get; private set; }

        /// <summary>Lista de leitura de todas as skills da barra atual.</summary>
        public IReadOnlyList<SkillDaBarra> Skills => _skills;

        /// <summary>Se há ao menos uma skill (com arma equipada).</summary>
        public bool TemSkills
        {
            get
            {
                foreach (var skill in _skills)
                {
                    if (skill != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>A skill do espaço, ou <c>null</c> se vazio.</summary>
        public SkillDaBarra Skill(int espaco) =>
            espaco >= 0 && espaco < _skills.Length ? _skills[espaco] : null;

        /// <summary>Identifica a qual tipo de arma as skills ativas pertencem, ou nulo se sem arma.</summary>
        public TipoEquipamento? TipoDaArmaAtiva()
        {
            foreach (var skill in _skills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.Id))
                {
                    var tipo = CatalogoDeSkills.TipoDaSkill(skill.Id);
                    if (tipo.HasValue)
                    {
                        return tipo.Value;
                    }
                }
            }

            return null;
        }

        /// <summary>Se a skill com esse ID está atualmente na barra ativa.</summary>
        public bool ContemSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return false;
            }

            foreach (var skill in _skills)
            {
                if (skill != null && skill.Id == skillId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Espaço de atalho (0, 1 ou 2) da skill na barra, ou -1 se não estiver na barra.</summary>
        public int EspacoDaSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return -1;
            }

            foreach (var skill in _skills)
            {
                if (skill != null && skill.Id == skillId)
                {
                    return skill.Espaco;
                }
            }

            return -1;
        }

        public void Aplicar(BarraDeSkillsMudou barra, double agora)
        {
            Recebida = true;
            ArmaId = barra?.ArmaId ?? 0;
            _skills = new SkillDaBarra[ConfigSkills.SkillsPorArma];
            if (barra?.Skills == null)
            {
                return;
            }

            foreach (var skill in barra.Skills)
            {
                if (skill == null || skill.Espaco < 0 || skill.Espaco >= _skills.Length)
                {
                    continue;
                }

                _skills[skill.Espaco] = skill;
                _prontaEm[skill.Espaco] = agora + skill.RestanteMs / 1000.0;
            }
        }

        /// <summary>O servidor confirmou o uso: a recarga do espaço começa agora.</summary>
        public void Usada(int espaco, int recargaMs, double agora)
        {
            if (espaco >= 0 && espaco < _prontaEm.Length)
            {
                _prontaEm[espaco] = agora + recargaMs / 1000.0;
            }
        }

        /// <summary>Quanto falta da recarga do espaço, em ms (0 se pronta ou vazio).</summary>
        public int RestanteMs(int espaco, double agora)
        {
            if (Skill(espaco) == null)
            {
                return 0;
            }

            var falta = (_prontaEm[espaco] - agora) * 1000.0;
            return falta <= 0 ? 0 : (int)falta;
        }
    }
}
