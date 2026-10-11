# Combate

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 6000–6999

## Resumo

Cálculo de dano, seleção de alvo e efeitos de status. Usado por PvE e PvP.

## Definido

- Regras iniciais da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- **Combate de ação** em 3ª pessoa, no estilo de New World (D4 da [validação de 2026-10-10](../brainstorm/validacao.md)): mira, esquiva, posicionamento
- Base: [New World](../referencias.md)
- **Ritmo do New World: ação, mas não frenética e sem spam de skills** (preferência do grupo, 2026-10-10). O dano vem principalmente de ataque leve e pesado; poucas skills, com recarga que pesa; stamina para esquiva e bloqueio
- **Papéis no grupo: tank, healer e DPS**, no estilo de World of Warcraft (decidido em 2026-10-10). Sem classes, o papel vem da combinação de itens equipados (D2)
- **Buffs de grupo**: cada papel traz buffs que complementam o time
- **Ataques telegrafados** (decidido em 2026-10-10): bosses e inimigos fortes avisam antes de bater (área marcada no chão, brilho ou animação de preparo) e o jogador tem tempo de esquivar ou sair da área. Base: Final Fantasy XIV e WoW

## Em aberto

- [x] ~~Combate com alvo fixo (tab target) ou mira livre (action)?~~ Decidido: action (D4)
- [ ] Ataque leve e pesado, bloqueio, stamina: quais entram na fatia jogável?
- [ ] **Troca de arma** (duas armas, como New World e Throne and Liberty)? Veja S10 na [validação](../brainstorm/validacao.md)
- [x] ~~Bosses com ataques telegrafados?~~ Decidido em 2026-10-10: sim (veja "Definido")
- [ ] Como o servidor valida acertos com a latência (lag compensation)?
- [ ] Quais efeitos de status existem?
- [ ] Como funciona o agro (ameaça) para o tank segurar os inimigos?
- [ ] Quais buffs cada papel traz? Eles acumulam?
- [ ] Um jogador pode trocar de papel trocando de itens no meio da dungeon?

## Regras

## Depende de

## Expõe

Código em `shared/Package/Runtime/Features/Combate/`. Regras e números: [fatia-jogavel.md](fatia-jogavel.md), seção 1 (não copiados aqui).

- `AtributosCombate`: os cinco atributos finais (Força, Destreza, Intelecto, Vitalidade, Espírito).
- `RegrasRecursos`: fórmulas puras de vida, vigor e energia máximos, `Limitar` (clamp) e regeneração (`RegenerarVigor`, `RegenerarEnergia`).
- `RecursosResponse`: atuais, máximos e atributos finais.
- `RotasCombate.Recursos`: `GET /api/personagens/{personagemId}/recursos` responde 200 `RecursosResponse`. Erros: `CodigosErroCombate.PersonagemNaoEncontrado` (404) e `NaoAutenticado` (401, Core).

Ataque da primeira arma (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 2):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6000 | `IntencaoDeAtaque` | client → servidor | `Tipo` (`TipoDeAtaque`: Leve 0, Pesado 1), `DirecaoY` (graus, mesma convenção de `IntencaoDeMovimento.RotacaoY`), `Sequencia` |
| 6001 | `AtaqueIniciado` | servidor → quem está na área | `AtacanteId`, `Tipo`, `DirecaoY`, `PreparoMs` |
| 6002 | `AtaqueAcertou` | servidor → quem está na área | `AtacanteId`, `Acertos[]` (`AcertoDeAtaque`: `AlvoId`, `Dano`, `VidaRestante`, `VidaMaxima`) |
| 6003 | `AtaqueRecusado` | servidor → atacante | `Sequencia`, `Codigo` (`CodigosErroCombate.VigorInsuficiente`, `AcaoEmAndamento` ou `VoceEstaCaido`) |
| 6004 | `RecursosMudaram` | servidor → dono | vida, vigor e energia atuais e máximos |

- `RegrasAtaque`: constantes de balanceamento do ataque e fórmulas puras (`PreparoSegundos`, `RecuperacaoSegundos`, `DuracaoSegundos`, `CustoDeVigor`, `TemVigor`, `Dano`, `EntraNaFila`, `EstaNoVolume`). O servidor decide; o client usa só para tocar a animação no tempo certo.
- Os IDs 6010 a 6012 ficam reservados para a esquiva (#62).
