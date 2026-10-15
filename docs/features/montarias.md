# Montarias

**Responsável:**
**Status:** em desenvolvimento
**Faixa de IDs de mensagem:** 16000–16999

## Resumo

Sistema de montarias: a pessoa chama uma montaria, anda com velocidade aumentada autoritativa e desmonta quando desejar ou quando entrar em combate.

## Definido

- **Primeira montaria:** uma montaria disponível para todos os personagens para testes e locomoção rápida no mundo aberto e na cidade.
- **Velocidade:** a velocidade montada é autoritativa no servidor (`VelocidadesDeMovimento`), com corrida padrão de 9,0 m/s (`RegrasMontaria.VelocidadeMontariaPadrao`), contra os 5,5 m/s da corrida padrão a pé.
- **Validação de movimento:** o `ValidadorDeMovimento` aceita o teto da montaria enquanto a pessoa estiver montada.
- **Multiplayer:** quem está na cidade ou no mundo aberto vê os outros jogadores montados e desmontados em tempo real (`EstadoMontaria`).
- **Combate:** não é permitido chamar montaria durante o combate. Se o personagem montado entrar em combate ou sofrer queda (`PersonagemCaiu`), é desmontado automaticamente e a velocidade retorna aos valores a pé.
- **Desconexão:** desconectar desmonta o personagem.

## Em aberto

- [ ] Estábulo e coleção de montarias com aparências diferentes
- [ ] Montarias com bônus ou habilidades utilitárias
- [ ] Voo e montarias voadoras

## Regras

### Velocidade autoritativa

| Estado | Andar (m/s) | Correr (m/s) |
|---|---|---|
| A pé | 2,0 (`RegrasVelocidade.AndarPadrao`) | 5,5 (`RegrasVelocidade.CorrerPadrao`) |
| Montado | 2,0 | 9,0 (`RegrasMontaria.VelocidadeMontariaPadrao`) |

Ao montar, o servidor envia `VelocidadesDeMovimento` com `Correr = 9.0f` e notifica `EstadoMontaria` (`Montado = true`). O client aplica e responde `ConfirmarVelocidade`.

### Mensagens de rede

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 16000 | `ChamarMontaria` | client → servidor | _(vazia)_ |
| 16001 | `Desmontar` | client → servidor | _(vazia)_ |
| 16002 | `EstadoMontaria` | servidor → client | `PersonagemId` (long), `Montado` (bool), `TipoMontaria` (int) |

## Depende de

- `Mundo`: `IVelocidadesAutoritativasDoMundo` para atualização de velocidades autoritativas no servidor e validação de deslocamento.
- `Combate`: `IEstadoDeCombate` para consulta de combate ativo e evento `PersonagemCaiu` para desmontar na queda.

## Expõe

- `IMontariasConsulta`: interface no servidor para outras funcionalidades consultarem se um jogador está montado (`EstaMontado(long personagemId)`).
