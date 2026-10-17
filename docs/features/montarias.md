# Montarias

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 16000–16999

## Resumo

Sistema de montarias: a pessoa chama uma montaria, anda com velocidade aumentada autoritativa e desmonta quando desejar ou quando entrar em combate.

## Definido

- **Modelo alvo (implementado #928):** montarias entram como **item raro negociável** largado por **chefes** (campo, chefe raro, dungeon/raid — tabela de bosses, 5% de chance com `ConfigLoot.ChanceDeMontariaDoChefe`). **Usar o item registra a montaria na conta** (`MontariaDaConta` / tabela `MontariasDaConta`); a conta **coleciona várias**; só **uma montada** por vez. O *Corcel Noturno* é obtido via item consumível negociável (*Rédeas do Corcel Noturno*, ID 10); o *Corcel Castanho* (Tipo 1) é desbloqueado por padrão em todas as contas.
- **Coleção e menu:** janela de [Montarias] (tecla **B** / atalho): **catálogo completo**, bloqueados em cinza com texto de origem (ex.: "Drop raro do Senhor das Sombras (5%)"); obtidas: equipar visual e atalhos de montar/desmontar.
- **Modelos disponíveis:**
  - *Corcel Castanho* (Tipo 1): montaria clássica em tons acolhedores e arreios tradicionais de couro com detalhes dourados (desbloqueada por padrão).
  - *Corcel Noturno* (Tipo 2): montaria mística com pelagem ébano, tons de índigo, sela prateada, detalhes luminescentes e chifre estrelado (drop raro de chefe).
- **Tempo de espera para montar:** subir na montaria exige 1,5 segundo de invocação parado (`RegrasMontaria.TempoInvocacaoSegundos`). Deslocar-se, sofrer dano ou cancelar interrompe a barra de progresso. Desmontar é instantâneo.
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

## Em aberto

- [x] Estábulo e coleção de montarias com aparências diferentes (implementado com Corcel Castanho e Corcel Noturno)
- [x] Drop raro de chefe, item consumível negociável e registro na conta (#928)
- [x] Decidido em 2026-10-16 (quiz de definições 2): não. Montarias com bônus ou habilidades utilitárias
- [x] Decidido em 2026-10-16 (quiz de definições 2): não. Voo e montarias voadoras

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
