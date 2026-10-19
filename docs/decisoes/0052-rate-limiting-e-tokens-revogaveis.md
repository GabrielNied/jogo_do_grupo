# 0052. Rate limiting e tokens revogáveis na autenticação

**Data:** 2026-10-18
**Status:** aceita

## Contexto

As rotas públicas `/registrar` e `/login` não possuíam limite de taxa por IP, permitindo tentativas contínuas de força bruta e sobrecarga no cálculo de PBKDF2. Além disso, tokens JWT tinham validade padrão de 100 anos sem mecanismo de invalidação (revogação), impedindo o encerramento forçado de sessões antigas ou comprometidas.

## Decisão

1. **Rate Limiting por IP no ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`)**:
   - Política `ContasPorIp` aplicada nas rotas `/registrar` e `/login` com limite configurável (padrão 15 requisições por minuto por IP, configurável via `Contas:RateLimit:LimitePorMinuto`).
   - Quando excedido, responde status HTTP 429 com `ErroApi` e código `CodigosErro.MuitasTentativas`.
   - Pode ser desativado nos testes gerais via `Contas:RateLimit:Ativo = false`.

2. **Atraso progressivo e bloqueio temporário por usuário (`ControleDeFalhasLogin`)**:
   - Falhas consecutivas de login por usuário normalizado acumulam atraso progressivo (a partir da 3ª falha) usando `TimeProvider`.
   - Atingindo 5 falhas consecutivas (`FalhasParaBloqueio`), a conta é temporariamente bloqueada por 5 minutos, respondendo 429 com `CodigosErro.MuitasTentativas`.
   - Autenticação com sucesso zera as falhas acumuladas.

3. **Carimbo de segurança (`CarimboSeguranca`) e revogação**:
   - Adicionada coluna `CarimboSeguranca` na tabela `Contas` (GUID aleatório gerado na criação).
   - O JWT gerado no login inclui a claim `carimbo`.
   - Validade padrão do JWT reduzida de 100 anos para 7 dias (168 horas), configurável via `Jwt:ValidadeHoras`.
   - Validação HTTP (`JwtBearerEvents.OnTokenValidated`) e UDP (`AutenticadorTempoReal`) verificam se o carimbo do token confere com o carimbo atual da conta via `IVerificadorCarimboConta` (com cache de 30s).
   - Ao alterar o carimbo no banco e invalidar o verificador, qualquer token antigo é imediatamente recusado tanto no HTTP quanto no UDP com `CodigosErro.NaoAutenticado`.

## Consequências

- Proteção robusta contra força bruta de senhas e ataques de negação de serviço nas rotas de autenticação.
- Capacidade de deslogar outros dispositivos e revogar sessões remotamente ao trocar a senha ou por ação de segurança.
- Mantém o contrato compartilhado estável através do código `CodigosErro.MuitasTentativas` e corpo padrão `ErroApi`.
