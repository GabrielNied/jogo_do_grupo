# Montarias

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 16000–16999

## Resumo

Sistema de montarias: a pessoa chama uma montaria, anda com velocidade aumentada autoritativa e desmonta quando desejar ou quando entrar em combate.

## Definido

- **Coleção de montarias:** menu de montarias (tecla **B**, ordem 50 nas `JanelasDoJogo`) e acesso rápido no HUD, permitindo visualizar a coleção e equipar o companheiro desejado.
- **Modelos disponíveis:**
  - *Corcel Castanho* (Tipo 1): montaria clássica em tons acolhedores e arreios tradicionais de couro com detalhes dourados.
  - *Corcel Noturno* (Tipo 2): montaria mística com pelagem ébano, tons de índigo, sela prateada, detalhes luminescentes e chifre estrelado.
- **Tempo de espera para montar:** subir na montaria exige 1,5 segundo de invocação parado (`RegrasMontaria.TempoInvocacaoSegundos`). Deslocar-se, sofrer dano ou cancelar interrompe a barra de progresso. Desmontar é instantâneo.
- **Velocidade:** a velocidade montada é autoritativa no servidor (`VelocidadesDeMovimento`), com corrida padrão de 9,0 m/s (`RegrasMontaria.VelocidadeMontariaPadrao`), contra os 5,5 m/s da corrida padrão a pé.
- **Validação de movimento:** o `ValidadorDeMovimento` aceita o teto da montaria enquanto a pessoa estiver montada.
- **Multiplayer:** quem está na cidade ou no mundo aberto vê os outros jogadores montados e desmontados em tempo real (`EstadoMontaria`), refletindo o tipo e o modelo equipado pelo jogador.
- **Combate:** não é permitido chamar montaria durante o combate. Se o personagem montado entrar em combate ou sofrer queda (`PersonagemCaiu`), é desmontado automaticamente e a velocidade retorna aos valores a pé.
- **Desconexão:** desconectar desmonta o personagem e limpa a sessão.

- **Todas as montarias são iguais e só muda o visual** (decidido por Gabriel em 2026-10-16): sem bônus, sem habilidade utilitária e sem voo.

## Em aberto

- [x] Estábulo e coleção de montarias com aparências diferentes (implementado com Corcel Castanho e Corcel Noturno)
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

## Depende de

- `Mundo`: `IVelocidadesAutoritativasDoMundo` para atualização de velocidades autoritativas no servidor e validação de deslocamento.
- `Combate`: `IEstadoDeCombate` para consulta de combate ativo e evento `PersonagemCaiu` para desmontar na queda.

## Expõe

- `IMontariasConsulta`: interface no servidor para outras funcionalidades consultarem se um jogador está montado (`EstaMontado(long personagemId)`).
