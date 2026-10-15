# Robôs de carga do tempo real

Console .NET que imita N jogadores contra um servidor de verdade, sem Unity (issue #150). Cada robô registra (ou reaproveita) a conta `robo_0001`..., cria um personagem pela API HTTP, conecta por LiteNetLib ([ADR 0015](../../docs/decisoes/0015-litenetlib-para-o-tempo-real.md)), entra na cidade e anda em círculo perto do nascimento (10 intenções por segundo, como o client), com um ataque leve a cada 2 s. Todos ficam à vista uns dos outros: é o pior caso de replicação numa área.

A meta de jogadores por área e os números medidos estão em [arquitetura.md](../../docs/arquitetura.md#carga-do-tempo-real).

## Rodar

Com o servidor de pé (`npm run server:rodar`), na raiz:

```
$env:JOGO_CHAVE_JWT = "<Jwt:Chave>"     # PowerShell; no bash: export JOGO_CHAVE_JWT=...
dotnet run -c Release --project tools/JogoDoGrupo.Robos -- --robos 80 --duracao 30
```

- `JOGO_CHAVE_JWT`: a chave do servidor (no desenvolvimento, `Jwt:Chave` do `appsettings.Development.json`). Com ela o robô assina um token curto com `permissao=administrar` e lê o tick em `/admin/metricas`. Sem ela, o resto sai igual e o tick fica de fora. `--chave-jwt <chave>` faz o mesmo, mas fica no histórico do shell: prefira a variável.
- `--json arquivo.json` grava o relatório em JSON; `--json -` escreve só o JSON no console.
- `--ajuda` lista as outras opções (servidor, porta UDP, ritmo de entrada, de movimento e de ataque, prefixo das contas).
- Sai com código 1 se houve erro, para usar em script.

## O que sai

```
Robôs: 80/80 no mundo, 15 s medidos
Mensagens: 838.4/s enviadas, 66905.1/s recebidas (7708.9 KB/s), réplicas 63662.7/s
Latência do ataque (ida e volta): p50 45.7 ms, p95 64.5 ms, p99 138.3 ms, máx 169.7 ms (588 amostras, 0 recusados)
Tick do servidor: 63 passos/s, duração p50 0.785 ms, p95 4.84 ms, máx 8.552 ms, último passo há 3.1 ms; 80 online, 0 erros no log no último minuto
Erros: 0
```

- **Mensagens:** pacotes por segundo somando todos os robôs; **réplicas** são as `PosicaoJogador` de outros robôs.
- **Latência do ataque:** de mandar `IntencaoDeAtaque` a receber o próprio `AtaqueIniciado` (ou `AtaqueRecusado`). Passa pelo laço, pelo Combate e pelo banco.
- **Tick:** passos do laço por segundo e duração de cada passo, nos últimos ~512 passos (lido no fim, com a carga ligada).
- **Erros:** conta que não entrou (`Api`, `Conexao`, `EntradaNaCidade`), robô derrubado (`Queda`), ataque sem resposta, `PosicaoForcada` (o servidor recusou um movimento honesto) e pacote ilegível.

## Cuidados

- Medição só vale com o servidor em Release e, de preferência, os robôs em outra máquina: na mesma, os dois disputam a CPU.
- Todos os robôs saem do mesmo IP. O servidor aceita no máximo `TempoReal:MaximoConexoesPendentesPorIp` (16) conexões ainda sem autenticar por IP: com o laço lento, a entrada rápida dá `Conexao: conexão recusada`. Baixe `--entradas-por-s` ou suba o servidor com `TempoReal__MaximoConexoesPendentesPorIp=1000` só para a carga.
- As contas ficam no banco do servidor e são reaproveitadas na próxima carga. Duas cargas ao mesmo tempo precisam de `--prefixo` diferentes.
- Cada robô abre uma `NetManager` (uma thread); algumas centenas é o limite prático de uma máquina.
- O teste curto (5 robôs por 5 s contra o `ServidorDeTeste`) roda no `dotnet test`: `tests/JogoDoGrupo.Server.Tests/Core/TempoReal/RobosDeCargaTestes.cs`.
