// Publica o servidor JogoDoGrupo.Server para produção (self-contained).
// Uso: node scripts/server/publicar.mjs [--rid linux-arm64|linux-x64] [--config Release|Debug] [--saida <pasta>]

import { execFileSync } from "node:child_process";
import { resolve } from "node:path";

export function lerArgumentos(argv) {
  const args = {
    rid: "linux-arm64",
    config: "Release",
    saida: "",
  };

  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === "--rid") args.rid = argv[++i] ?? "";
    else if (argv[i] === "--config") args.config = argv[++i] ?? "";
    else if (argv[i] === "--saida") args.saida = argv[++i] ?? "";
    else throw new Error(`Argumento desconhecido: ${argv[i]}`);
  }

  if (!args.rid) throw new Error("Informe o RID (ex.: linux-arm64, linux-x64).");
  if (!args.saida) args.saida = `server/publish/${args.rid}`;
  return args;
}

export function montarComandoPublish(args) {
  const projeto = "server/src/JogoDoGrupo.Server/JogoDoGrupo.Server.csproj";
  return [
    "publish",
    projeto,
    "-c",
    args.config,
    "-r",
    args.rid,
    "--self-contained",
    "-o",
    args.saida,
  ];
}

export function executarPublicacao(argv = process.argv.slice(2)) {
  const args = lerArgumentos(argv);
  const cmd = montarComandoPublish(args);
  console.log(`Publicando servidor para ${args.rid} em ${args.saida}...`);
  execFileSync("dotnet", cmd, { stdio: "inherit" });
  console.log(`✔ Publicação concluída com sucesso em ${args.saida}`);
}

if (process.argv[1] && resolve(process.argv[1]) === resolve(import.meta.filename ?? "")) {
  try {
    executarPublicacao();
  } catch (erro) {
    console.error(`Erro ao publicar servidor: ${erro.message}`);
    process.exit(1);
  }
}
