# 0021. Hospedagem gratuita: PC do grupo agora, Oracle Cloud em São Paulo depois

**Data:** 2026-10-11
**Status:** aceito

## Contexto

O servidor só roda em `localhost` e o grupo quer jogar junto sem custo. O servidor precisa de **UDP** (tempo real, [ADR 0015](0015-litenetlib-para-o-tempo-real.md)), além do HTTP da conta ([ADR 0009](0009-api-http-para-conta-e-personagens.md)), e de latência boa no Brasil.

Opções avaliadas em out/2026:

| Opção | Resultado |
|---|---|
| **Oracle Cloud Always Free** | Grátis sem prazo, região em São Paulo, VM ARM até 2 OCPU / 12 GB (limite reduzido em jun/2026), UDP liberado, cerca de 10 TB/mês de saída. Pede cartão; recursos grátis só na região escolhida no cadastro; capacidade às vezes esgotada; VM ociosa por 7 dias pode ser recolhida em conta só grátis. |
| Google Cloud e2-micro | Grátis sem prazo, mas só em regiões dos EUA (≈150 ms) e 1 GB/mês de saída para fora da América do Norte. |
| AWS | Contas novas (desde jul/2025) não têm mais VM grátis, só crédito inicial. |
| Render, Railway, Fly.io | Sem UDP ou sem plano grátis. |
| PC do grupo + Tailscale ou playit.gg | Grátis, aceita UDP, mas só fica no ar com o PC ligado. ngrok não serve (sem UDP). |

## Decisão

- **Agora:** o servidor roda no PC de alguém do grupo, exposto por **Tailscale** (rede privada) ou **playit.gg** (túnel público TCP + UDP).
- **Sempre ligado:** uma VM **Oracle Cloud Always Free** na região **São Paulo**, com o servidor como serviço `systemd` e **PostgreSQL** na mesma VM (cumpre o [ADR 0010](0010-banco-de-dados.md)).
- **HTTPS** com subdomínio grátis (DuckDNS) e Caddy na frente do HTTP; o UDP 5081 fica direto na VM. Build que não é de desenvolvimento não aceita HTTP.
- **Deploy** automático pelo GitHub Actions a cada merge na `main`, com volta à versão anterior se `/saude` falhar.
- **Backup** diário do banco no Object Storage grátis da Oracle.
- **Client** gerado no CI e distribuído numa página restrita do itch.io.
- **Reserva:** Google Cloud e2-micro, se não houver capacidade na Oracle em São Paulo.
- As tarefas estão no plano da issue #197.

### Prontidão para produção (implementada em out/2026)

- **Portas e Proxy:** HTTP (porta 5080) atrás de proxy reverso Caddy com repasse de cabeçalhos `X-Forwarded-For` e `X-Forwarded-Proto` via `ForwardedHeadersOptions`; UDP 5081 direto no host.
- **Segredos:** Segredos de desenvolvimento (ex.: chave JWT padrão) são terminantemente recusados fora de `Development`; variáveis de ambiente (`Jwt__Chave`, `ConnectionStrings__Jogo`) são obrigatórias.
- **Saúde e Diagnóstico:** Endpoint público `GET /saude` reporta status 200 OK com `Banco: conectado` ou 503 Service Unavailable com `Banco: indisponivel`.
- **Logs:** Suporte a logs estruturados no console em formato JSON compacto (`Observabilidade:ConsoleEstruturado` ou padrão em `Production`) para captura direta por `systemd` / `journald`.
- **Publicação:** Alvo de publicação self-contained documentado para `linux-arm64` e `linux-x64` via `npm run server:publicar`.

## Consequências

- Custo zero, mas sem garantia de disponibilidade: um processo, uma VM, sem redundância.
- O client e o servidor deixam de assumir `localhost`: o endereço passa a ser configurável.
- Os limites grátis mudam sem aviso (a Oracle cortou pela metade em 2026): reavaliar este ADR se mudarem de novo ou se o jogo passar do que uma VM aguenta.
- Dividir o mundo entre servidores continua em aberto ([arquitetura.md](../arquitetura.md#decisões-em-aberto)); este ADR cobre um servidor só.
- Uma pessoa do grupo fica dona da conta Oracle (cartão e acesso), registrada em [responsaveis.md](../responsaveis.md).
