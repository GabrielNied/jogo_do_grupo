using System.Globalization;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Contratos.Social;
using JogoDoGrupo.Server.Core.Contas;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core.Moderacao;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Core.Moderacao;

/// <summary>
/// Serviço central de moderação: execução de comandos de GM com auditoria e recepção de denúncias (#187).
/// </summary>
public sealed class ServicoDeModeracao(
    IServiceProvider servicos,
    ServidorTempoReal tempoReal,
    ILogger<ServicoDeModeracao> logger
)
{
    public async Task<ResultadoComandoGm> ProcessarComandoGmAsync(
        SessaoTempoReal sessao,
        PedirComandoGm pedido
    )
    {
        if (!sessao.EhGm)
        {
            logger.LogWarning(
                "[GM-AUDIT-RECUSA] Conta {ContaId} Personagem {PersonagemId} tentou comando '{Comando}' sem ser GM.",
                sessao.ContaId,
                sessao.PersonagemId,
                pedido.Comando
            );
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Você não tem permissão para usar comandos de GM.",
            };
        }

        var comando = (pedido.Comando ?? "").Trim().ToLowerInvariant();
        if (!RegrasModeracao.EhComandoGmValido(comando))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem =
                    "Comando de GM desconhecido. Use /ajuda para ver os comandos disponíveis.",
            };
        }

        var args = (pedido.Argumentos ?? "").Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        return comando switch
        {
            "ajuda" => new ResultadoComandoGm
            {
                Sucesso = true,
                Mensagem =
                    "Comandos de GM: /tp <x y z> ou /tp <jogador>, /puxar <jogador>, /silenciar <jogador> [minutos], /expulsar <jogador> [motivo], /banir <jogador> [dias] [motivo]",
            },
            "tp" => await ExecutarTeleporteAsync(sessao, args),
            "puxar" => await ExecutarPuxarAsync(sessao, args),
            "silenciar" => await ExecutarSilenciarAsync(sessao, args),
            "expulsar" => await ExecutarExpulsarAsync(sessao, args),
            "banir" => await ExecutarBanirAsync(sessao, args),
            _ => new ResultadoComandoGm { Sucesso = false, Mensagem = "Comando não implementado." },
        };
    }

    public async Task<ResultadoDenuncia> ProcessarDenunciaAsync(
        SessaoTempoReal sessao,
        DenunciarJogador pedido
    )
    {
        if (string.IsNullOrWhiteSpace(pedido.AlvoNome))
        {
            return new ResultadoDenuncia
            {
                Sucesso = false,
                Mensagem = "Nome do jogador alvo é obrigatório.",
            };
        }

        var motivo = (pedido.Motivo ?? "").Trim().ToLowerInvariant();
        if (!RegrasModeracao.EhMotivoDenunciaValido(motivo))
        {
            return new ResultadoDenuncia
            {
                Sucesso = false,
                Mensagem = "Motivo de denúncia inválido.",
            };
        }

        var diretorio = servicos.GetService<IDiretorioDePersonagens>();
        long? alvoPersonagemId = null;
        long? alvoContaId = null;

        if (diretorio != null && diretorio.TentarObterPorNome(pedido.AlvoNome.Trim(), out var pId))
        {
            alvoPersonagemId = pId;
            if (diretorio.TentarObterContaDoPersonagem(pId, out var cId))
            {
                alvoContaId = cId;
            }
        }

        var detalhes = (pedido.Detalhes ?? "").Trim();
        if (detalhes.Length > RegrasModeracao.TamanhoMaximoDetalhes)
        {
            detalhes = detalhes.Substring(0, RegrasModeracao.TamanhoMaximoDetalhes);
        }

        var denuncia = new Denuncia
        {
            AutorContaId = sessao.ContaId,
            AutorPersonagemId = sessao.PersonagemId,
            AlvoContaId = alvoContaId,
            AlvoPersonagemId = alvoPersonagemId,
            AlvoNome = pedido.AlvoNome.Trim(),
            Motivo = motivo,
            Detalhes = detalhes,
            MetadadosContexto = "",
            CriadaEm = DateTime.UtcNow,
        };

        var escopos = servicos.GetRequiredService<IServiceScopeFactory>();
        using (var escopo = escopos.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            db.Set<Denuncia>().Add(denuncia);
            await db.SaveChangesAsync();
        }

        logger.LogInformation(
            "[DENUNCIA] Conta {ContaId} denunciou '{AlvoNome}' (Motivo: {Motivo}).",
            sessao.ContaId,
            pedido.AlvoNome,
            motivo
        );

        return new ResultadoDenuncia
        {
            Sucesso = true,
            Mensagem = "Denúncia enviada com sucesso. Nossa equipe irá analisar.",
        };
    }

    private async Task<ResultadoComandoGm> ExecutarTeleporteAsync(
        SessaoTempoReal sessao,
        string[] args
    )
    {
        var teleporte = servicos.GetService<ITeleporteMundo>();
        if (teleporte == null)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Serviço de teleporte indisponível.",
            };
        }

        if (args.Length == 3)
        {
            if (
                float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                && float.TryParse(
                    args[1],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var y
                )
                && float.TryParse(
                    args[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var z
                )
            )
            {
                var resultado = teleporte.Teleportar(sessao.PersonagemId, x, y, z);
                if (resultado.Aceito)
                {
                    await RegistrarAuditoriaAsync(
                        sessao,
                        "tp",
                        FormattableString.Invariant($"{x} {y} {z}"),
                        "Teleporte por coordenadas"
                    );
                    return new ResultadoComandoGm
                    {
                        Sucesso = true,
                        Mensagem = FormattableString.Invariant(
                            $"Teleportado para ({x}, {y}, {z})."
                        ),
                    };
                }

                return new ResultadoComandoGm { Sucesso = false, Mensagem = resultado.Texto };
            }

            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Coordenadas inválidas. Use números: /tp <x> <y> <z>",
            };
        }

        if (args.Length >= 1)
        {
            var alvoNome = args[0];
            var diretorio = servicos.GetService<IDiretorioDePersonagens>();
            var posicoes = servicos.GetService<IPosicoesDoMundo>();

            if (diretorio == null || posicoes == null)
            {
                return new ResultadoComandoGm
                {
                    Sucesso = false,
                    Mensagem = "Serviços de localização indisponíveis.",
                };
            }

            if (!diretorio.TentarObterPorNome(alvoNome, out var alvoId))
            {
                return new ResultadoComandoGm
                {
                    Sucesso = false,
                    Mensagem = $"Jogador '{alvoNome}' não encontrado.",
                };
            }

            if (!posicoes.TentarObter(alvoId, out var pos))
            {
                return new ResultadoComandoGm
                {
                    Sucesso = false,
                    Mensagem = $"Jogador '{alvoNome}' não está localizado no mundo.",
                };
            }

            var resultado = teleporte.Teleportar(sessao.PersonagemId, pos.X, pos.Y, pos.Z);
            if (resultado.Aceito)
            {
                await RegistrarAuditoriaAsync(
                    sessao,
                    "tp",
                    alvoNome,
                    FormattableString.Invariant(
                        $"Teleporte para jogador em ({pos.X}, {pos.Y}, {pos.Z})"
                    )
                );
                return new ResultadoComandoGm
                {
                    Sucesso = true,
                    Mensagem = $"Teleportado para junto de '{alvoNome}'.",
                };
            }

            return new ResultadoComandoGm { Sucesso = false, Mensagem = resultado.Texto };
        }

        return new ResultadoComandoGm
        {
            Sucesso = false,
            Mensagem = "Uso: /tp <x y z> ou /tp <jogador>",
        };
    }

    private async Task<ResultadoComandoGm> ExecutarPuxarAsync(SessaoTempoReal sessao, string[] args)
    {
        if (args.Length < 1)
        {
            return new ResultadoComandoGm { Sucesso = false, Mensagem = "Uso: /puxar <jogador>" };
        }

        var alvoNome = args[0];
        var diretorio = servicos.GetService<IDiretorioDePersonagens>();
        var posicoes = servicos.GetService<IPosicoesDoMundo>();
        var teleporte = servicos.GetService<ITeleporteMundo>();

        if (diretorio == null || posicoes == null || teleporte == null)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Serviços necessários indisponíveis.",
            };
        }

        if (!diretorio.TentarObterPorNome(alvoNome, out var alvoId))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = $"Jogador '{alvoNome}' não encontrado.",
            };
        }

        if (!posicoes.TentarObter(sessao.PersonagemId, out var posGm))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Você não está localizado no mundo.",
            };
        }

        var resultado = teleporte.Teleportar(alvoId, posGm.X, posGm.Y, posGm.Z);
        if (resultado.Aceito)
        {
            await RegistrarAuditoriaAsync(
                sessao,
                "puxar",
                alvoNome,
                FormattableString.Invariant($"Puxou para ({posGm.X}, {posGm.Y}, {posGm.Z})")
            );
            return new ResultadoComandoGm
            {
                Sucesso = true,
                Mensagem = $"Jogador '{alvoNome}' puxado até você.",
            };
        }

        return new ResultadoComandoGm { Sucesso = false, Mensagem = resultado.Texto };
    }

    private async Task<ResultadoComandoGm> ExecutarSilenciarAsync(
        SessaoTempoReal sessao,
        string[] args
    )
    {
        if (args.Length < 1)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Uso: /silenciar <jogador> [minutos]",
            };
        }

        var alvoNome = args[0];
        var minutos = 10;
        if (args.Length >= 2 && int.TryParse(args[1], out var m))
        {
            minutos = m;
        }

        minutos = RegrasModeracao.NormalizarMinutosSilencio(minutos);

        var diretorio = servicos.GetService<IDiretorioDePersonagens>();
        var silenciador = servicos.GetService<ISilenciadorDoChat>();

        if (diretorio == null || silenciador == null)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Serviço de chat indisponível.",
            };
        }

        if (!diretorio.TentarObterPorNome(alvoNome, out var alvoId))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = $"Jogador '{alvoNome}' não encontrado.",
            };
        }

        silenciador.Silenciar(alvoId, TimeSpan.FromMinutes(minutos));
        await RegistrarAuditoriaAsync(
            sessao,
            "silenciar",
            alvoNome,
            $"Silenciado por {minutos} minutos"
        );

        return new ResultadoComandoGm
        {
            Sucesso = true,
            Mensagem = $"Jogador '{alvoNome}' silenciado por {minutos} minuto(s).",
        };
    }

    private async Task<ResultadoComandoGm> ExecutarExpulsarAsync(
        SessaoTempoReal sessao,
        string[] args
    )
    {
        if (args.Length < 1)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Uso: /expulsar <jogador> [motivo]",
            };
        }

        var alvoNome = args[0];
        var motivo = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "Expulso por GM.";

        var diretorio = servicos.GetService<IDiretorioDePersonagens>();
        if (diretorio == null)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Diretório de personagens indisponível.",
            };
        }

        if (!diretorio.TentarObterPorNome(alvoNome, out var alvoId))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = $"Jogador '{alvoNome}' não encontrado.",
            };
        }

        var desconectou = tempoReal.DesconectarPersonagem(alvoId);
        await RegistrarAuditoriaAsync(
            sessao,
            "expulsar",
            alvoNome,
            $"Motivo: {motivo} (Online: {desconectou})"
        );

        return new ResultadoComandoGm
        {
            Sucesso = true,
            Mensagem = desconectou
                ? $"Jogador '{alvoNome}' expulso do servidor."
                : $"Jogador '{alvoNome}' não estava conectado no momento.",
        };
    }

    private async Task<ResultadoComandoGm> ExecutarBanirAsync(SessaoTempoReal sessao, string[] args)
    {
        if (args.Length < 1)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Uso: /banir <jogador> [dias] [motivo]",
            };
        }

        var alvoNome = args[0];
        var dias = 1;
        var indiceMotivo = 1;

        if (args.Length >= 2 && int.TryParse(args[1], out var d))
        {
            dias = d;
            indiceMotivo = 2;
        }

        dias = RegrasModeracao.NormalizarDiasBanimento(dias);
        var motivo =
            args.Length > indiceMotivo
                ? string.Join(" ", args.Skip(indiceMotivo))
                : "Banimento aplicado por GM.";

        var diretorio = servicos.GetService<IDiretorioDePersonagens>();
        if (diretorio == null)
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = "Diretório de personagens indisponível.",
            };
        }

        if (!diretorio.TentarObterPorNome(alvoNome, out var alvoId))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = $"Jogador '{alvoNome}' não encontrado.",
            };
        }

        if (!diretorio.TentarObterContaDoPersonagem(alvoId, out var alvoContaId))
        {
            return new ResultadoComandoGm
            {
                Sucesso = false,
                Mensagem = $"Conta do jogador '{alvoNome}' não encontrada.",
            };
        }

        var dataFim = DateTime.UtcNow.AddDays(dias);

        var escopos = servicos.GetRequiredService<IServiceScopeFactory>();
        using (var escopo = escopos.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var conta = await db.Set<Conta>().FindAsync(alvoContaId);
            if (conta != null)
            {
                conta.BanidaAte = dataFim;
                conta.MotivoBanimento = motivo;
                await db.SaveChangesAsync();
            }
        }

        tempoReal.DesconectarConta(alvoContaId);

        await RegistrarAuditoriaAsync(
            sessao,
            "banir",
            alvoNome,
            $"Dias: {dias}, Motivo: {motivo}, Ate: {dataFim:u}"
        );

        return new ResultadoComandoGm
        {
            Sucesso = true,
            Mensagem = $"Jogador '{alvoNome}' banido por {dias} dia(s). Motivo: {motivo}",
        };
    }

    private async Task RegistrarAuditoriaAsync(
        SessaoTempoReal sessao,
        string acao,
        string alvo,
        string detalhes
    )
    {
        try
        {
            var registro = new RegistroAuditoriaGm
            {
                GmContaId = sessao.ContaId,
                GmPersonagemId = sessao.PersonagemId,
                Acao = acao,
                Alvo = alvo,
                Detalhes = detalhes,
                ExecutadaEm = DateTime.UtcNow,
            };

            var escopos = servicos.GetRequiredService<IServiceScopeFactory>();
            using var escopo = escopos.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            db.Set<RegistroAuditoriaGm>().Add(registro);
            await db.SaveChangesAsync();

            logger.LogInformation(
                "[GM-AUDIT] GM Conta {ContaId} Personagem {PersonagemId} executou {Acao} em {Alvo}. Detalhes: {Detalhes}",
                sessao.ContaId,
                sessao.PersonagemId,
                acao,
                alvo,
                detalhes
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[GM-AUDIT-ERRO] Falha ao persistir auditoria de GM para ação {Acao} em {Alvo}.",
                acao,
                alvo
            );
        }
    }
}
