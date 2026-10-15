// Envia os reports do jogo como issues. O client chama sozinho depois de cada report (e ao
// abrir o Play Mode, para os que ficaram pendentes); à mão só para depurar:
//
//   npm run reporte -- enviar [--id <id>] [--pasta <pasta dos relatos>]
//   npm run reporte -- listar [--pasta <pasta dos relatos>]
//   npm run reporte -- previa [--id <id>] [--pasta <pasta dos relatos>]   (só grava o issue.md)
//
// Para cada `relatos/pendentes/<id>/reporte.json`: sobe as imagens no release `relatos-beta`,
// procura issues parecidas, junta o trecho do log do servidor local e cria a issue. Deu certo:
// grava o número no reporte.json e move a pasta para `relatos/enviados/<id>/`. Deu errado: grava
// o erro e deixa em pendentes para a próxima vez. Uma linha por report na saída, lida pelo
// client: `enviado <id> <número> <url>` ou `falhou <id> <motivo>`.

import { spawnSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import {
  RELEASE_DAS_IMAGENS,
  ROTULO_REPORTE,
  corpoDoReporte,
  rotulosDoReporte,
  tituloDoReporte,
  trechoDoLog,
} from "./corpo-do-reporte.mjs";

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "..");
const PASTA_DO_LOG = path.join(RAIZ, "server", "src", "JogoDoGrupo.Server", "logs");

/** Trava de envio com mais de 10 min é de um processo que morreu. */
const TRAVA_ABANDONADA_MS = 10 * 60 * 1000;

const IMAGENS = [
  {
    arquivo: "folha-impacto.png",
    titulo: "Folha de impacto",
    legenda:
      "12 quadros seguidos em volta do último acerto, com o centro da tela ampliado 2×. O número no canto é o qNNN da linha do tempo.",
  },
  {
    arquivo: "folha-geral.png",
    titulo: "Folha geral",
    legenda: "12 quadros espalhados pelos últimos segundos com imagem.",
  },
  {
    arquivo: "tela.png",
    titulo: "Tela no momento do report",
    legenda: "A tela inteira no último quadro antes de abrir o report.",
  },
];

/** Pastas de `pendentes/` com `reporte.json`, da mais antiga à mais nova. */
export function reportesPendentes(pasta, id = null) {
  const pendentes = path.join(pasta, "pendentes");
  if (!fs.existsSync(pendentes)) return [];
  return fs
    .readdirSync(pendentes, { withFileTypes: true })
    .filter((entrada) => entrada.isDirectory() && (id == null || entrada.name === id))
    .map((entrada) => path.join(pendentes, entrada.name))
    .filter((dir) => fs.existsSync(path.join(dir, "reporte.json")))
    .sort();
}

function lerManifesto(dir) {
  return JSON.parse(fs.readFileSync(path.join(dir, "reporte.json"), "utf8").replace(/^﻿/, ""));
}

function gravarManifesto(dir, manifesto) {
  fs.writeFileSync(path.join(dir, "reporte.json"), `${JSON.stringify(manifesto, null, 2)}\n`);
}

function lerSeExistir(arquivo) {
  return fs.existsSync(arquivo) ? fs.readFileSync(arquivo, "utf8") : "";
}

function gh(args) {
  const resultado = spawnSync("gh", args, { cwd: RAIZ, encoding: "utf8" });
  if (resultado.error) {
    throw new Error(
      resultado.error.code === "ENOENT" ? "gh não encontrado no PATH" : resultado.error.message,
    );
  }
  if (resultado.status !== 0) {
    const erro = (resultado.stderr || resultado.stdout || "").trim().split("\n").pop();
    throw new Error(`gh ${args[0]} ${args[1] ?? ""}: ${erro}`);
  }
  return resultado.stdout;
}

function tentar(funcao, padrao) {
  try {
    return funcao();
  } catch {
    return padrao;
  }
}

function prepararRepositorio() {
  const repositorio = JSON.parse(gh(["repo", "view", "--json", "nameWithOwner"])).nameWithOwner;
  const rotulos = new Set(
    JSON.parse(gh(["label", "list", "--limit", "300", "--json", "name"])).map((r) => r.name),
  );
  if (!rotulos.has(ROTULO_REPORTE)) {
    gh([
      "label",
      "create",
      ROTULO_REPORTE,
      "--color",
      "D93F0B",
      "--description",
      "Aberta pelo report do jogo (F9 ou J)",
    ]);
    rotulos.add(ROTULO_REPORTE);
  }

  if (!tentar(() => gh(["release", "view", RELEASE_DAS_IMAGENS, "--json", "tagName"]), null)) {
    gh([
      "release",
      "create",
      RELEASE_DAS_IMAGENS,
      "--prerelease",
      "--title",
      "Relatos beta (imagens dos reports)",
      "--notes",
      "Imagens anexadas às issues abertas pelo report do jogo. Não é uma versão do jogo.",
    ]);
  }

  return { repositorio, rotulos };
}

function subirImagens(dir, manifesto, repositorio) {
  const presentes = IMAGENS.filter((imagem) => fs.existsSync(path.join(dir, imagem.arquivo)));
  if (presentes.length === 0) return [];
  const temporaria = fs.mkdtempSync(path.join(os.tmpdir(), "reporte-"));
  try {
    const arquivos = presentes.map((imagem) => {
      const destino = path.join(temporaria, `${manifesto.Id}-${imagem.arquivo}`);
      fs.copyFileSync(path.join(dir, imagem.arquivo), destino);
      return destino;
    });
    gh(["release", "upload", RELEASE_DAS_IMAGENS, ...arquivos, "--clobber"]);
  } finally {
    fs.rmSync(temporaria, { recursive: true, force: true });
  }

  return presentes.map((imagem) => ({
    ...imagem,
    url: `https://github.com/${repositorio}/releases/download/${RELEASE_DAS_IMAGENS}/${manifesto.Id}-${imagem.arquivo}`,
  }));
}

// Só o servidor desta máquina: o report do editor e o servidor local rodam juntos.
function logDoServidor(manifesto) {
  const quando = Date.parse(manifesto.QuandoUtc);
  if (!manifesto.Editor || Number.isNaN(quando) || !fs.existsSync(PASTA_DO_LOG)) return [];
  const inicio = quando - (manifesto.SegundosDaLinhaDoTempo + 1) * 1000;
  const fim = quando + 2000;
  const linhas = [];
  for (const nome of fs.readdirSync(PASTA_DO_LOG).filter((n) => n.endsWith(".ndjson"))) {
    const arquivo = path.join(PASTA_DO_LOG, nome);
    if (fs.statSync(arquivo).mtimeMs < inicio) continue;
    linhas.push(...fs.readFileSync(arquivo, "utf8").split("\n"));
  }
  return trechoDoLog(linhas, inicio, fim);
}

function parecidas(manifesto) {
  const palavras = (manifesto.Frase ?? "")
    .toLowerCase()
    .split(/[^\p{L}\p{N}]+/u)
    .filter((palavra) => palavra.length > 3)
    .slice(0, 5);
  if (palavras.length === 0) return [];
  return tentar(
    () =>
      JSON.parse(
        gh([
          "issue",
          "list",
          "--state",
          "open",
          "--search",
          `${palavras.join(" ")} in:title,body`,
          "--json",
          "number,title",
          "--limit",
          "5",
        ]),
      ),
    [],
  );
}

function montarCorpo(dir, manifesto, imagens, comParecidas) {
  return corpoDoReporte({
    manifesto,
    linhaDoTempo: lerSeExistir(path.join(dir, "linha-do-tempo.txt")),
    estado: lerSeExistir(path.join(dir, "estado.txt")),
    console: lerSeExistir(path.join(dir, "console.txt")),
    log: logDoServidor(manifesto),
    imagens,
    parecidas: comParecidas ? parecidas(manifesto) : [],
  });
}

function enviarUm(dir, pasta, contexto) {
  const manifesto = lerManifesto(dir);
  const imagens = subirImagens(dir, manifesto, contexto.repositorio);
  const corpo = montarCorpo(dir, manifesto, imagens, true);
  const arquivoDoCorpo = path.join(dir, "issue.md");
  fs.writeFileSync(arquivoDoCorpo, corpo);
  const rotulos = rotulosDoReporte(manifesto, contexto.rotulos);
  const saida = gh([
    "issue",
    "create",
    "--title",
    tituloDoReporte(manifesto),
    "--body-file",
    arquivoDoCorpo,
    ...rotulos.flatMap((rotulo) => ["--label", rotulo]),
  ]);
  const url = saida.trim().split("\n").pop();
  const numero = Number(/\/issues\/(\d+)/.exec(url)?.[1] ?? 0);

  manifesto.Status = "enviado";
  manifesto.Issue = numero;
  manifesto.Url = url;
  manifesto.Erro = "";
  gravarManifesto(dir, manifesto);

  const enviados = path.join(pasta, "enviados");
  fs.mkdirSync(enviados, { recursive: true });
  fs.renameSync(dir, path.join(enviados, manifesto.Id));
  const ultimo = path.join(pasta, "ultimo.txt");
  if (lerSeExistir(ultimo).trim() === `pendentes/${manifesto.Id}`) {
    fs.writeFileSync(ultimo, `enviados/${manifesto.Id}`);
  }

  return { numero, url };
}

function travar(pasta) {
  fs.mkdirSync(pasta, { recursive: true });
  const trava = path.join(pasta, "enviando.trava");
  if (fs.existsSync(trava) && Date.now() - fs.statSync(trava).mtimeMs > TRAVA_ABANDONADA_MS) {
    fs.rmSync(trava, { force: true });
  }
  try {
    fs.closeSync(fs.openSync(trava, "wx"));
  } catch {
    return null;
  }
  return () => fs.rmSync(trava, { force: true });
}

function enviar(pasta, id) {
  const pendentes = reportesPendentes(pasta, id);
  if (pendentes.length === 0) {
    console.log("nenhum");
    return 0;
  }

  const soltar = travar(pasta);
  if (!soltar) {
    console.log("ocupado outro envio está rodando");
    return 0;
  }

  let falhas = 0;
  try {
    let contexto;
    for (const dir of pendentes) {
      const nome = path.basename(dir);
      try {
        contexto ??= prepararRepositorio();
        const { numero, url } = enviarUm(dir, pasta, contexto);
        console.log(`enviado ${nome} ${numero} ${url}`);
      } catch (erro) {
        falhas++;
        const motivo = erro.message.replace(/\s+/g, " ");
        tentar(() => {
          const manifesto = lerManifesto(dir);
          manifesto.Status = "falhou";
          manifesto.Erro = motivo;
          gravarManifesto(dir, manifesto);
        });
        console.log(`falhou ${nome} ${motivo}`);
      }
    }
  } finally {
    soltar();
  }

  return falhas > 0 ? 1 : 0;
}

// Monta o issue.md de cada pendente sem falar com o GitHub (imagens apontam para o arquivo local).
function previa(pasta, id) {
  const pendentes = reportesPendentes(pasta, id);
  for (const dir of pendentes) {
    const manifesto = lerManifesto(dir);
    const imagens = IMAGENS.filter((imagem) => fs.existsSync(path.join(dir, imagem.arquivo))).map(
      (imagem) => ({ ...imagem, url: imagem.arquivo }),
    );
    const arquivo = path.join(dir, "issue.md");
    fs.writeFileSync(arquivo, montarCorpo(dir, manifesto, imagens, false));
    console.log(`previa ${tituloDoReporte(manifesto)}  →  ${arquivo}`);
  }
  if (pendentes.length === 0) console.log("nenhum");
  return 0;
}

function listar(pasta) {
  for (const sub of ["pendentes", "enviados"]) {
    const dir = path.join(pasta, sub);
    if (!fs.existsSync(dir)) continue;
    for (const nome of fs.readdirSync(dir).sort()) {
      const manifesto = tentar(() => lerManifesto(path.join(dir, nome)), null);
      if (!manifesto) continue;
      const situacao = manifesto.Issue ? `#${manifesto.Issue}` : manifesto.Status;
      console.log(`${sub}/${nome}  ${situacao}  [${manifesto.Tipo}] ${manifesto.Frase}`);
    }
  }
  return 0;
}

function valor(args, nome) {
  const indice = args.indexOf(nome);
  return indice >= 0 ? args[indice + 1] : null;
}

function principal(args) {
  const pasta = path.resolve(valor(args, "--pasta") ?? path.join(RAIZ, "relatos"));
  switch (args[0]) {
    case "enviar":
      return enviar(pasta, valor(args, "--id"));
    case "listar":
      return listar(pasta);
    case "previa":
      return previa(pasta, valor(args, "--id"));
    default:
      console.log(
        "uso: npm run reporte -- enviar | previa [--id <id>] [--pasta <pasta>] | listar [--pasta <pasta>]",
      );
      return args[0] ? 1 : 0;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  process.exitCode = principal(process.argv.slice(2));
}
