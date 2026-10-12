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
- A vida cai em `Dano` (sem defesa por enquanto; a conta fica em `ServicoDeDanoRecebido.DanoFinal`, onde a defesa entra depois), **nunca abaixo de 0**, e o dono recebe `RecursosMudaram` (só ele). Vida 0 leva ao estado caído ([abaixo](#estado-caído-no-servidor-125)); quem já está caído não recebe dano (o golpe é ignorado).
- O assinante não bloqueia (o evento sai do laço de IA de Mobs): o banco roda numa tarefa, na mesma transação dos outros recursos, então golpes simultâneos no mesmo personagem somam. Personagem sem recursos criados começa da vida cheia; um que não existe só registra erro no log.

### Esquiva no servidor (#233)

- O servidor recebe `IntencaoDeEsquiva`, confere e cobra **15 de vigor uma vez** (mesma transação do ataque) e só então pede ao Mundo a janela de deslocamento (`IDeslocamentosDoMundo`: 5 m em 0,6 s). Quem valida cada posição continua sendo o Mundo; fora da janela vale o limite de corrida.
- Quem recebe: `EsquivaIniciada` vai a todos que estão na área (o dono também); `EsquivaRecusada` e `RecursosMudaram` só ao dono.
- Recusas: `VigorInsuficiente` (nada é gasto), `VoceEstaCaido` e `AcaoEmAndamento`.
- **Ataque e esquiva não valem ao mesmo tempo:** a esquiva é recusada durante um ataque (do começo ao fim da recuperação) e durante outra esquiva; o ataque pedido durante a esquiva é recusado com `AcaoEmAndamento`, sem entrar na fila. De dois pedidos no mesmo instante, um vale. A esquiva **não cancela** o preparo de um ataque (proposta; a pergunta segue aberta na #62).
- Direção inválida ou esquiva de quem ainda não entrou no mundo: o pedido é descartado sem resposta, como no ataque.
- Parede: o servidor ainda não conhece a geometria; valida distância, velocidade e os limites do mundo. Colisão com parede fica no client até o servidor ter o mapa.

### Estado caído no servidor (#125, subtarefa #423)

Combate é o dono do estado caído (decidido em 2026-10-11). As regras e os casos de borda estão em [personagem.md](personagem.md#morte-e-caído-issue-97) e em [fatia-jogavel.md](fatia-jogavel.md) (seção 7); aqui só o contrato e o que o servidor faz.

| ID | Nome | Direção | Campos |
|---|---|---|---|
| 6020 | `ComecarALevantar` | client → servidor | `AlvoId` |
| 6021 | `CancelarLevantar` | client → servidor | (vazio) |
| 6022 | `VoltarParaACidade` | client → servidor | (vazio) |
| 6023 | `EstadoDeQueda` | servidor → caído, ajudante e quem está na área | `PersonagemId`, `Estado` (`FaseDeQueda`: `Vivo`=0, `Caido`=1, `Levantando`=2, `Retorno`=3), `SegundosRestantes`, `AjudanteId`, `ProgressoMs` |
| 6024 | `AcaoRecusada` | servidor → quem pediu | `Codigo`: `VoceEstaCaido`, `AlvoNaoEstaCaido`, `LongeDemais`, `JaEstaSendoLevantado`, `NaoEstaCaido`, `AindaNaoPodeVoltar` |

- **Números** em `RegrasQueda` (shared): 45 s caído, 3 s para levantar, 20% da vida, 2 m (2,0 vale), 5 s até poder voltar. O servidor lê os tempos de `ConfigQueda` (padrão = `RegrasQueda`; os testes de integração usam tempos curtos).
- **Quando cai:** vida 0 depois de um golpe de inimigo. `EstadoDeQueda` com `Caido` vai ao dono, ao grupo e à área; `PersonagemCaiu` é publicado em `Contratos/Combate/`.
- **Levantar:** `Levantando` com `AjudanteId` e `ProgressoMs`; ao completar, `Vivo` com 20% da vida (vigor e energia como estavam), `RecursosMudaram` ao dono e o evento `PersonagemLevantou`. O servidor confere o alcance a cada 100 ms: sair dos 2 m, soltar, levar dano, cair, atacar, esquivar ou desconectar interrompem e zeram o progresso (volta `Caido`).
- **Voltar à cidade:** fim dos 45 s ou `VoltarParaACidade` (depois de 5 s). Vida, vigor e energia vão ao máximo, `Retorno` é enviado e o evento `PersonagemVoltouParaACidade` é publicado: **quem leva o personagem ao ponto de nascimento é o Mundo** (ainda não assina o evento; veja abaixo). Nada de item, XP, moeda ou nível muda.
- **Uma saída por queda:** levantar, fim do tempo e voltar disputam a mesma trava (`ControleDeQueda`); a primeira que vence ganha e as outras viram `NaoEstaCaido` ou `AlvoNaoEstaCaido`. Se a interação termina no mesmo ms do fim do tempo, levantar vence.
- **Contagem não para** enquanto alguém levanta, e **não há** os 2 s sem dano depois de levantar (os dois seguem em aberto em [personagem.md](personagem.md#em-aberto)).
- **Para outras funcionalidades (ADR 0016):** `IEstadoDeQueda.EstaCaido(id)` (Mobs deve ignorar quem está caído: não atacar nem contar como alvo) e os eventos `PersonagemCaiu`, `PersonagemLevantou` e `PersonagemVoltouParaACidade`, em `server/.../Contratos/Combate/`.
- **Pendências de outras funcionalidades:** Social ainda não expõe o grupo em `Contratos/` (hoje qualquer jogador vivo a até 2 m levanta e o aviso vai a toda a área; TODO em `ServicoDeQueda`); Mundo ainda não assina `PersonagemVoltouParaACidade` para mover ao nascimento; Mobs ainda não consulta `IEstadoDeQueda`.

### Sensação do acerto no client (#453)

O client só reage ao que o servidor confirmou (`AtaqueAcertou`, vida menor em `RecursosMudaram`, `EsquivaIniciada`, `InimigoPreparaAtaque`); nada aparece por previsão de dano. Código em `Features/Combate/Scripts/` (`EscolhaDeEfeito`, `ControleImpacto`).

- **Golpe leve:** flash curto e recuo pequeno no inimigo, som baixo. **Pesado:** flash e recuo maiores, hit-stop de 0,07 s e tremor de câmera leve.
- **Golpe de outro jogador** no mesmo inimigo: só flash curto e som baixo (sem hit-stop, recuo nem tremor), para não poluir a tela.
- **Golpe recebido** (vida que caiu): moldura vermelha, flash no personagem, tremor leve e som; se tirou 20% ou mais da vida máxima, também hit-stop e tremor maior.
- **Limites:** no máximo 8 flashes e 4 sons por janela de 0,12 s; hit-stop e tremor não se somam (vale o maior, com teto).
- **Esquiva:** som ao esquivar. "Esquivou!" aparece quando o jogador estava no círculo de um golpe avisado, esquivou antes do golpe, estava fora quando o preparo acabou e o servidor não tirou vida. Hoje é inferido; uma mensagem do servidor para "esquivou" deixaria exato. Estar dentro de um círculo avisado mostra "Saia da área!" com um som de alerta.
- **Sons:** gerados por código (`SintetizadorDeSons`), sem arquivos de terceiros. Entram no mixer de Core quando o #123 existir. Tremor, hit-stop e som têm chaves em `ConfiguracaoDeImpacto`, a ligar na tela de opções do Core.
