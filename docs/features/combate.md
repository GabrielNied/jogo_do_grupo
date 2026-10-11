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
- **Decisões do Gabriel em 2026-10-11** (estavam só em comentário de issue):
  - **Ataque (#61):** cone de **2,5 m e 90°** à frente do atacante; **sem compensação de lag na fatia**; boneco de treino na cidade. Os números moram em `RegrasAtaque`.
  - **Esquiva (#62):** o dono do código é **Combate** (o vigor é dele); a tecla é o Alt esquerdo ([interface.md](interface.md)).
  - **Caído (#125):** o dono do estado é **Combate**, que já é dono da vida; [Personagem](personagem.md#morte-e-caído-issue-97) só mostra a tela.

## Em aberto

- [x] ~~Combate com alvo fixo (tab target) ou mira livre (action)?~~ Decidido: action (D4)
- [ ] Ataque leve e pesado, bloqueio, stamina: quais entram na fatia jogável?
- [ ] **Troca de arma** (duas armas, como New World e Throne and Liberty)? Veja S10 na [validação](../brainstorm/validacao.md)
- [x] ~~Bosses com ataques telegrafados?~~ Decidido em 2026-10-10: sim (veja "Definido")
- [ ] Como o servidor valida acertos com a latência (lag compensation)? Decidido pelo Gabriel em 2026-10-11 **para a fatia**: sem compensação de lag (o cone é medido no momento do acerto, com a posição que o servidor tem). Depois da fatia a pergunta segue aberta (issue #172).
- [ ] Quais efeitos de status existem?
- [ ] Como funciona o agro (ameaça) para o tank segurar os inimigos?
- [ ] Quais buffs cada papel traz? Eles acumulam?
- [ ] Um jogador pode trocar de papel trocando de itens no meio da dungeon?

### Propostas do livro de Asterra (2026-10-11, não validadas)

Vêm do [Livro de ideias](../brainstorm/figjam.md) (seção 10). São **proposta do board (2026-10-11), não validada pelo grupo**; nada aqui muda o que já está em "Definido" ou implementado.

- [ ] **Bloqueio por defesa** (sem parry nem janela de timing): com defesa de pelo menos 1,5x a potência de bloqueio do golpe, anula o dano direto; abaixo disso reduz proporcionalmente, até 80%. Responde em parte à pergunta de bloqueio acima. Depende da defesa (equipamento, [itens.md](itens.md)); o dano recebido hoje não tem defesa (`ServicoDeDanoRecebido.DanoFinal`).
- [ ] **Sem defesa de timing:** o livro rejeita parry e bloqueio perfeito. **Conflito leve** com a sugestão "defesa que abre oportunidade" (bloquear no timing certo desequilibra o inimigo), que está só em "Sugestões para discussão" do board.
- [ ] **Esquiva sem invulnerabilidade:** coincide com o que já vale (seção 3 da [fatia](fatia-jogavel.md)); o livro só reforça.
- [ ] **Ataques básicos por arma** (espada corta, martelo varre, arco com disparo ou tiro carregado, cajado com pulso ou projétil ampliado): hoje só a espada existe. Outras armas entram com [itens.md](itens.md).
- [ ] **Mira e volumes de acerto:** mouse direciona golpes, projéteis e áreas, sem seleção automática obrigatória; a área visível da skill deve ser a área real de acerto; volumes generosos (cone, faixa, círculo). Coerente com o cone de 90° já implementado; vale como critério para as skills.
- [ ] **Movimentos além de correr e esquivar:** pulo, natação e escalada (proposta de escalada em superfícies e bordas do mapa; rios cruzados a nado, com gelo como alternativa). Dono: [personagem.md](personagem.md) e [mundo.md](mundo.md).
- [ ] **Recursos:** pools-base de 100 vigor e 100 energia e regeneração de 10 vigor/s e 5 energia/s após 2 s **já coincidem** com a [fatia](fatia-jogavel.md#1-atributos-e-recursos). Novidade: poções, refeições e descanso em locais seguros como fontes de recuperação, e equipamentos e comida que modificam a regeneração.
- [ ] **Estado caído:** o livro propõe 45 s, levantar por 3 s, 20% de vida e retorno à cidade ou teleporte descoberto; coincide com a [fatia](fatia-jogavel.md#7-dungeon-de-até-5-com-um-boss) e com [personagem.md](personagem.md#morte-e-caído-issue-97). Novidade: o **botão de retorno antes dos 45 s** e o retorno a **teleporte descoberto**; sem perda de XP, moeda, equipamento, materiais nem durabilidade por morte na primeira versão.
- [ ] **Combos e cast com compromisso** (sugestões do topo do board): uma skill prepara e outra finaliza; skills fortes com preparo visível e cancelável (perde parte do recurso). Ficam em [skills.md](skills.md).

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
- ~~Os IDs 6010 a 6012 ficam reservados para a esquiva (#62).~~ Substituído em 2026-10-11: o contrato da esquiva está abaixo.

Esquiva (tempo real; regras e números em [fatia-jogavel.md](fatia-jogavel.md), seção 3):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 6010 | `IntencaoDeEsquiva` | client → servidor | `DirecaoY` (graus, mesma convenção do ataque), `Sequencia` |
| 6011 | `EsquivaRecusada` | servidor → dono | `Sequencia`, `Codigo` (os mesmos códigos do ataque: `VigorInsuficiente`, `AcaoEmAndamento`, `VoceEstaCaido`) |
| 6012 | `EsquivaIniciada` | servidor → quem está na área | `PersonagemId`, `DirecaoY`, `DuracaoMs` |

- `RegrasEsquiva`: constantes de balanceamento (5 m, 0,6 s, 15 de vigor) e fórmulas puras (`TemVigor`, `EmAndamento`, `DistanciaPermitidaAte`, `DeslocamentoCabe`, `Destino`). A posição continua chegando pelas mensagens do Mundo.

### Ataque no servidor (#215)

- Quem recebe: `AtaqueIniciado` e `AtaqueAcertou` vão a todos que o Mundo diz estarem na área do atacante (`IPosicoesDoMundo`); `AtaqueRecusado` só ao atacante; `RecursosMudaram` só ao dono, e só quando o ataque gasta vigor (o pesado; o leve não gasta nada).
- O servidor mede o cone no **momento do acerto** (preparo depois do pedido) com a posição que o Mundo tem então; não há compensação de lag. A direção vem do pedido.
- Fila: um pedido feito nos últimos 0,3 s da ação espera e começa no fim dela (vigor e queda são conferidos nessa hora, e a recusa sai com a `Sequencia` dele). Pedido durante a ação fora da janela, ou com a fila ocupada, é recusado com `AcaoEmAndamento`.
- Caído (vida 0) é recusado com `VoceEstaCaido`; o pesado gasta 20 de vigor uma vez só (transação no banco) e é recusado sem vigor.
- `Tipo` fora do enum, direção inválida, ou ataque de quem ainda não entrou no mundo: o pedido é **descartado sem resposta** (o contrato não tem código para isso; um client correto nunca manda).
- O dano é `RegrasAtaque.Dano(tipo, Força final)`; cada alvo leva o mesmo golpe uma vez, e o `Dano` de `AcertoDeAtaque` é o que o alvo realmente perdeu (nunca mais que a vida que ele tinha).
- **Alvos:** `IAlvosDeCombate` (`server/.../Contratos/Combate/`, ADR 0016) é implementado por quem é dono de alvos; Combate junta todas as implementações. Hoje só há o **boneco de treino** (id de alvo `-1`), em `ConfigBonecoDeTreino`: 180 de vida, posição fixa na cidade (x 0, z -22, 2 m à frente do nascimento), volta à vida cheia 5 s depois do último golpe que levou. Com 0 de vida ele sai de combate (não é mais alvo) até voltar. O servidor **não avisa** a volta: o client deduz pelos 5 s sem golpes.
- **Onde o boneco está (provisório):** o contrato não tem mensagem para isso, então o client coloca o modelo na mesma posição fixa (x 0, z -22) até o shared ganhar uma mensagem ou constante para os alvos fixos (pendente com o responsável pelo contrato).

### Golpe de inimigo tira vida (#278)

- Combate, dono da vida, assina `GolpeDeInimigoAcertou` (`Contratos/Mobs/`, ADR 0016). Mobs decide quem estava no círculo; um evento por personagem atingido.
- A vida cai em `Dano` (sem defesa por enquanto; a conta fica em `ServicoDeDanoRecebido.DanoFinal`, onde a defesa entra depois), **nunca abaixo de 0**, e o dono recebe `RecursosMudaram` (só ele). A morte e o renascimento são a #125: em 0 a vida só para.
- O assinante não bloqueia (o evento sai do laço de IA de Mobs): o banco roda numa tarefa, na mesma transação dos outros recursos, então golpes simultâneos no mesmo personagem somam. Personagem sem recursos criados começa da vida cheia; um que não existe só registra erro no log.

### Esquiva no servidor (#233)

- O servidor recebe `IntencaoDeEsquiva`, confere e cobra **15 de vigor uma vez** (mesma transação do ataque) e só então pede ao Mundo a janela de deslocamento (`IDeslocamentosDoMundo`: 5 m em 0,6 s). Quem valida cada posição continua sendo o Mundo; fora da janela vale o limite de corrida.
- Quem recebe: `EsquivaIniciada` vai a todos que estão na área (o dono também); `EsquivaRecusada` e `RecursosMudaram` só ao dono.
- Recusas: `VigorInsuficiente` (nada é gasto), `VoceEstaCaido` e `AcaoEmAndamento`.
- **Ataque e esquiva não valem ao mesmo tempo:** a esquiva é recusada durante um ataque (do começo ao fim da recuperação) e durante outra esquiva; o ataque pedido durante a esquiva é recusado com `AcaoEmAndamento`, sem entrar na fila. De dois pedidos no mesmo instante, um vale. A esquiva **não cancela** o preparo de um ataque (proposta; a pergunta segue aberta na #62).
- Direção inválida ou esquiva de quem ainda não entrou no mundo: o pedido é descartado sem resposta, como no ataque.
- Parede: o servidor ainda não conhece a geometria; valida distância, velocidade e os limites do mundo. Colisão com parede fica no client até o servidor ter o mapa.
