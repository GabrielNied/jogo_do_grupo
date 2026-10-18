# Montarias

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 16000–16999

## Resumo

Sistema de montarias: a pessoa chama uma montaria, anda com velocidade aumentada autoritativa e desmonta quando desejar ou quando entrar em combate.

## Definido

- **Estábulo na Brasa Mansa** (decidido por Gabriel em 2026-10-18): o cavalariço vende as montarias básicas por coroas, a **Mula de Carga** (que sai da loja geral) e um **cavalo comum**. As montarias de chefe continuam só no drop ([npcs](npcs.md#elenco-da-brasa-mansa), [#1067](https://github.com/GabrielNied/jogo_do_grupo/issues/1067)).
- **Modelo alvo (implementado #928):** montarias entram como **item raro negociável** largado por **chefes** (campo, chefe raro, dungeon/raid — tabela de bosses, 5% de chance com `ConfigLoot.ChanceDeMontariaDoChefe`). **Usar o item registra a montaria na conta** (`MontariaDaConta` / tabela `MontariasDaConta`); a conta **coleciona várias**; só **uma montada** por vez. O *Corcel Noturno* é obtido via item consumível negociável (*Rédeas do Corcel Noturno*, ID 10); o *Corcel Castanho* (Tipo 1) é desbloqueado por padrão em todas as contas.
- **Coleção e menu:** janela [Companheiros](interface.md#companheiros-u) (tecla **U**), aba **Montarias**: **catálogo completo**, bloqueados em cinza com texto de origem (ex.: "Drop raro do Guardião do Reflexo (5%)"); obtidas: equipar visual e atalhos de montar/desmontar.
- **Modelos disponíveis:**
  - *Corcel Castanho* (Tipo 1): montaria clássica em tons acolhedores e arreios tradicionais de couro com detalhes dourados (desbloqueada por padrão).
  - *Corcel Noturno* (Tipo 2): montaria mística com pelagem ébano, tons de índigo, sela prateada, detalhes luminescentes e chifre estrelado (drop raro de chefe).
- **Tempo de espera para montar:** subir na montaria exige 1,5 segundo de invocação parado (`RegrasMontaria.TempoInvocacaoSegundos`). Deslocar-se, sofrer dano ou cancelar interrompe a barra de progresso. Desmontar é instantâneo.
- **Atalho de montaria (T):** alterna montar e desmontar. O atalho é ignorado quando o menu do Esc estiver aberto ou quando houver digitação de texto ativa (chat, busca, janela de reporte F9).
- **Velocidade:** a velocidade montada é autoritativa no servidor (`VelocidadesDeMovimento`), com corrida padrão de 9,0 m/s (`RegrasMontaria.VelocidadeMontariaPadrao`), contra os 5,5 m/s da corrida padrão a pé.
- **Validação de movimento:** o `ValidadorDeMovimento` aceita o teto da montaria enquanto a pessoa estiver montada.
- **Multiplayer:** quem está na cidade ou no mundo aberto vê os outros jogadores montados e desmontados em tempo real (`EstadoMontaria`), refletindo o tipo e o modelo equipado pelo jogador.
- **Combate:** não é permitido chamar montaria durante o combate nem usar itens consumíveis de montaria em combate. Se o personagem montado entrar em combate ou sofrer queda (`PersonagemCaiu`), é desmontado automaticamente e a velocidade retorna aos valores a pé.
- **Desconexão:** desconectar desmonta o personagem e limpa a sessão.
- **Uso do item e registro na conta:**
  - Usar o item (`UsarItemMontaria`, 16005) fora de combate consome o exemplar da bolsa e salva permanentemente na conta (`MontariaDaConta`).
  - Se o jogador já possuir a montaria registrada, a ação é recusada com código `JaPossuiMontaria` (16008) e o item **não é consumido** (mantendo-o negociável para outros jogadores).
  - Se estiver em combate, a ação é recusada com `EmCombate` (16008) sem consumir o item.
  - Ao registrar, notifica `MontariaDesbloqueada` (16007), envia a coleção atualizada `ColecaoMontarias` (16006) e equipa a montaria automaticamente.

- **Todas as montarias são iguais e só muda o visual** (decidido por Gabriel em 2026-10-16): sem bônus, sem habilidade utilitária e sem voo.
- **Atalho da coleção:** a janela de montarias abre com **P**. A tecla **M** permanece reservada ao mapa e **T** à invocação da montaria ativa.

## Em aberto

- [x] Estábulo e coleção de montarias com aparências diferentes (implementado com Corcel Castanho e Corcel Noturno)
- [x] Drop raro de chefe, item consumível negociável e registro na conta (#928)
- [x] Decidido em 2026-10-16 (quiz de definições 2): não. Montarias com bônus ou habilidades utilitárias
- [x] Decidido em 2026-10-16 (quiz de definições 2): não. Voo e montarias voadoras
- [x] Demais tipos e rédeas do [catálogo proposto](#catálogo-proposta) no `shared` e loot **por chefe** (Presa-Velha e Guardião; endgame e mula na loja).

## Catálogo (proposta)

**Proposta** alinhada a [mobs](mobs.md#chefe-e-chefe-raro) e `ConfigLoot.ChanceDeMontariaDoChefe`. Implementado em `RegrasMontaria`, `CatalogoItens` e `RegrasLootCompanheiros` (Presa-Velha → Lobo de Presa; Guardião → Corcel de Cristal; tipos 5–7 no catálogo U aguardam chefes; Mula na loja).

### Drop e item

| Regra | Valor |
|---|---|
| Quem dropa | **Chefe** ou **chefe raro** |
| Quem **não** dropa | Comum, elite |
| Chance | **5%** por participante por morte (`ConfigLoot.ChanceDeMontariaDoChefe`) |
| Item | Rédeas consumíveis **negociáveis**; registrar fora de combate |
| Stats | **Todas iguais** (corrida 9 m/s); só muda o visual |
| Sorteio futuro | **Uma montaria rara por chefe** (ou tabela fixa por `CriaturaId` de chefe), em vez de um único item para todos os chefes |

Convenção: **Rédeas do/da …**. Cada linha reserva um `TipoMontaria` (1–99) e id em `CatalogoItens`. Tipo **1** = inicial (desbloqueado por padrão).

### Lista por progressão

| Tipo | Nome | Item (proposto) | Origem (texto na UI) | Chefe / fonte |
|---|---|---|---|---|
| 1 | Corcel Castanho | — *(implementado)* | Montaria inicial de Asterra | Conta nova |
| 2 | Corcel Noturno | Rédeas do Corcel Noturno *(id 10, implementado)* | Drop raro de chefes (5%) | Hoje: **qualquer** chefe/chefe raro; alvo: ver linhas 3–4 |
| 3 | Lobo de Presa | Rédeas do Lobo de Presa | Drop raro da Presa-Velha (5%) | **Presa-Velha** (id 200) |
| 4 | Corcel de Cristal | Rédeas do Corcel de Cristal | Drop raro do Guardião do Reflexo (5%) | **Guardião do Reflexo** (id 100, dungeon) |
| 5 | Corcel da Geada | Rédeas do Corcel da Geada | Drop raro de chefe de neve (5%) | Chefe de raid/neve *(ficha FUTURO)* |
| 6 | Basalto Vivente | Rédeas do Basalto Vivente | Drop raro do Colosso Basáltico (5%) | **Colosso Basáltico** *(FUTURO, [mobs](mobs.md#livro-de-asterra-bestiário-proposta-do-board-2026-10-11))* |
| 7 | Tempestário | Rédeas do Tempestário | Drop raro do Ancião Trovejante (5%) | **Ancião Trovejante** *(FUTURO)* |
| 8 | Mula de Carga | Rédeas da Mula de Carga | Comerciante ou missão *(sem drop de chefe)* | Opcional: cosmético acessível, negociável |

Sem voo, sem bônus de velocidade ou utilidade. Pets: [pets.md — Catálogo (proposta)](pets.md#catálogo-proposta).

### Prioridade sugerida

1. Separar **Presa-Velha** (Lobo de Presa) e **Guardião do Reflexo** (Corcel de Cristal ou renomear Noturno só para um deles).
2. Endgame quando existirem chefes de mundo do livro.
3. Mula de Carga só se economia/missões pedirem montaria barata fora do loop de boss.

## Regras

### Velocidade autoritativa

| Estado | Andar (m/s) | Correr (m/s) |
|---|---|---|
| A pé | 2,0 (`RegrasVelocidade.AndarPadrao`) | 5,5 (`RegrasVelocidade.CorrerPadrao`) |
| Montado | 2,0 | 9,0 (`RegrasMontaria.VelocidadeMontariaPadrao`) |

Ao montar, o servidor envia `VelocidadesDeMovimento` com `Correr = 9.0f` e notifica `EstadoMontaria` (`Montado = true`, `TipoMontaria`). O client aplica e responde `ConfirmarVelocidade`.

### Mensagens de rede

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 16000 | `ChamarMontaria` | client → servidor | _(vazia)_ |
| 16001 | `Desmontar` | client → servidor | _(vazia)_ |
| 16002 | `EstadoMontaria` | servidor → client | `PersonagemId` (long), `Montado` (bool), `TipoMontaria` (int) |
| 16003 | `EquiparMontaria` | client → servidor | `TipoMontaria` (int) |
| 16004 | `MontariaEquipada` | servidor → client | `TipoMontaria` (int) |
| 16005 | `UsarItemMontaria` | client → servidor | `InstanciaId` (long) |
| 16006 | `ColecaoMontarias` | servidor → client | `MontariasDesbloqueadas` (int[]) |
| 16007 | `MontariaDesbloqueada` | servidor → client | `TipoMontaria` (int), `Nome` (string) |
| 16008 | `MontariaRecusada` | servidor → client | `Codigo` (string), `Motivo` (string) |

## Depende de

- `Mundo`: `IVelocidadesAutoritativasDoMundo` para atualização de velocidades autoritativas no servidor e validação de deslocamento.
- `Combate`: `IEstadoDeCombate` para consulta de combate ativo e evento `PersonagemCaiu` para desmontar na queda.
- `Itens`: `CatalogoItens` para rédeas consumíveis negociáveis e consumo de exemplar na bolsa.

## Expõe

- `IMontariasConsulta`: interface no servidor para outras funcionalidades consultarem se um jogador está montado (`EstaMontado(long personagemId)`), se uma conta possui uma montaria (`PossuiMontaria(long contaId, int tipoMontaria)`) ou obter toda a coleção desbloqueada da conta (`ObterColecao(long contaId)`).
