# Jogar com o grupo a partir de um PC

Como o grupo joga junto **agora**, sem nuvem e sem custo: o servidor roda no PC de uma pessoa (o **anfitrião**) e os outros conectam nele. A decisão e o plano para depois (Oracle Cloud) estão no [ADR 0021](decisoes/0021-hospedagem-gratuita.md).

> Este guia ainda **não foi testado de ponta a ponta** por uma pessoa do grupo. Quem seguir pela primeira vez: corrija aqui o que não bater.

O servidor usa duas portas:

| Porta | Protocolo | Para quê |
|---|---|---|
| **5080** | TCP (HTTP) | conta, login e personagens |
| **5081** | UDP | tempo real (movimento, combate, chat) |

Só fica no ar enquanto o PC do anfitrião estiver ligado com o servidor rodando.

## 1. Anfitrião: subir o servidor escutando na rede

Com o repositório pronto ([primeiros passos](../README.md#primeiros-passos)), rode na raiz:

```
dotnet run --project server/src/JogoDoGrupo.Server --urls http://0.0.0.0:5080
```

- O `--urls http://0.0.0.0:5080` é o que faz o HTTP aceitar conexões de outros PCs. Sem ele (por exemplo com `npm run server:rodar`) o servidor só escuta em `localhost`. A variável `ASPNETCORE_URLS=http://0.0.0.0:5080` faz o mesmo quando o servidor não é aberto pelo `dotnet run`.
- O tempo real (UDP 5081) já escuta em todos os endereços; não precisa mudar nada.

**Firewall do Windows:** na primeira vez o Windows pergunta se o `dotnet` pode usar a rede; aceite para **redes privadas**. Se a pergunta não aparecer e ninguém conseguir conectar, abra um PowerShell **como administrador** e libere as duas portas:

```
New-NetFirewallRule -DisplayName "Jogo do Grupo HTTP" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow
New-NetFirewallRule -DisplayName "Jogo do Grupo tempo real" -Direction Inbound -Protocol UDP -LocalPort 5081 -Action Allow
```

## 2. Ligar os PCs: escolha um dos dois caminhos

### Tailscale (recomendado para o grupo)

Rede privada entre os PCs do grupo. Não abre porta no roteador e ninguém de fora enxerga o servidor.

1. Todos instalam o [Tailscale](https://tailscale.com/download) e entram na **mesma rede** (a mesma conta, ou convidados para a rede do anfitrião).
2. O anfitrião copia o próprio endereço `100.x.y.z` (aparece no ícone do Tailscale).
3. Todos usam esse endereço no passo 3.

### playit.gg (para quem não quer instalar nada além do jogo)

Túnel público: só o anfitrião instala o programa do [playit.gg](https://playit.gg) e cria **dois túneis**, um **TCP** para a porta local 5080 e um **UDP** para a porta local 5081. Cada túnel ganha um endereço e uma porta públicos, que os outros usam no passo 3.

- O endereço é público: qualquer pessoa que o descobrir chega ao servidor.
- Confira no site os limites do plano grátis (quantidade de túneis e região) antes de combinar com o grupo; eles mudam e não estão anotados aqui.
- ngrok **não serve**: não tem UDP.

## 3. Jogadores: abrir o client apontando para o anfitrião

O client procura o endereço nesta ordem: argumentos de linha de comando, depois o arquivo `servidor.json` ao lado do executável, depois `localhost`.

Por argumento (atalho do Windows ou terminal):

```
jogo_do_grupo.exe -servidor http://100.x.y.z:5080 -tempo-real 100.x.y.z:5081
```

Ou pelo arquivo `servidor.json` ao lado do executável (no Editor, na pasta `client/`):

```json
{ "servidor": "http://100.x.y.z:5080", "tempoReal": "100.x.y.z:5081" }
```

- Com o playit.gg, troque pelos endereços e portas que cada túnel mostrou (o do TCP em `servidor`, o do UDP em `tempoReal`).
- O anfitrião joga no próprio PC sem configurar nada (`localhost`).
- Endereço inválido não trava o jogo: fica um aviso no log e vale a fonte seguinte.

## Lembretes

- **Só o Development Build aceita HTTP.** Um build final recusa `http://` e precisa de HTTPS (issue #203). Até lá, distribua para o grupo um Development Build.
- **Uma conta por jogador.** O servidor aceita uma sessão por conta: entrar com a mesma conta em outro PC derruba a primeira.
- Os dados (contas e personagens) ficam no banco do PC do anfitrião. Se outra pessoa hospedar, começa do zero.

## Se não conectar

| Sintoma | O que conferir |
|---|---|
| O login falha de cara | O servidor subiu com `--urls http://0.0.0.0:5080`? O firewall liberou a porta 5080 (TCP)? O endereço em `-servidor` tem `http://` e a porta? |
| O login passa, mas não entra no mundo | A porta **5081 UDP** está liberada no firewall? No playit.gg, o segundo túnel é UDP? O `-tempo-real` está certo? |
| Erro de conexão insegura | O client é um build final; use um Development Build. |
