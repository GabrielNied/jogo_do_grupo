import assert from "node:assert/strict";
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { execFileSync } from "node:child_process";

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
export const pastaVisual = "client/Assets/_Game/Core/UI/Visual/";
export const nomeUss = (grupo, nome) => "--lumora-" + grupo.toLowerCase() + "-" + nome.replace(/[A-Z]/g, (c, i) => (i ? "-" : "") + c.toLowerCase());

export function gerar(config) {
  assert.equal(config.versao, 1, "Versão de configuração desconhecida");
  for (const grupo of ["Cores", "Tipografia", "Espacamento", "Dimensoes", "Bordas", "Ornamentos", "Responsividade", "Fontes"])
    assert(config[grupo] && Object.keys(config[grupo]).length, "Grupo ausente: " + grupo);
  assert(config.Tipografia.Leitura >= 16 && config.Tipografia.LeituraMaxima >= config.Tipografia.Leitura, "Leitura abaixo do mínimo ou limites invertidos");
  assert(config.Dimensoes.AlvoMinimo >= 44, "Alvos menores que 44 px");
  assert(config.Responsividade.EstreitoAte < config.Responsividade.CompactoAte, "Limiares responsivos invertidos");
  let uss = "/* Gerado de design/interface/config-visual.json. Não editar à mão. */\n.lumora-ui {\n";
  let cs = "// Gerado de design/interface/config-visual.json. Não editar à mão.\nnamespace JogoDoGrupo.Client.Core.Visual\n{\n    public static class ConfigVisual\n    {\n";
  for (const [grupo, valores] of Object.entries(config)) {
    if (grupo === "versao") continue;
    cs += `        public static class ${grupo}\n        {\n`;
    for (const [nome, valor] of Object.entries(valores)) {
      assert.match(nome, /^[A-Z][A-Za-z0-9]*$/);
      if (grupo === "Cores") assert.match(valor, /^(#[0-9A-Fa-f]{6}|rgba\(\d+, \d+, \d+, (?:0(?:\.\d+)?|1)\))$/, "Cor inválida");
      if (grupo !== "Cores" && grupo !== "Fontes") {
        assert.equal(typeof valor, "number", "Dimensão deve ser numérica");
        assert(Number.isInteger(valor), "Dimensão deve ser inteira");
        assert(grupo === "Ornamentos" && nome === "Deslocamento" ? valor <= 0 : valor > 0, "Dimensão fora do intervalo");
        if (nome.endsWith("Percentual")) assert(valor <= 100, "Percentual maior que 100");
      }
      if (grupo === "Fontes") assert.match(valor, /^Lumora\/Fontes\/[A-Za-z0-9-]+$/, "Caminho de fonte inválido");
      if (typeof valor === "number") assert(Number.isInteger(valor), "Dimensão não inteira");
      const tipo = typeof valor === "number" ? "int" : "string";
      cs += `            public const ${tipo} ${nome} = ${JSON.stringify(valor)};\n`;
      if (grupo !== "Fontes" && grupo !== "Responsividade") {
        const unidade = typeof valor === "number" ? (nome.startsWith("Fatia") ? "" : nome.endsWith("Percentual") ? "%" : "px") : "";
        uss += `    ${nomeUss(grupo, nome)}: ${valor}${unidade};\n`;
      }
    }
    cs += "        }\n";
  }
  return { cs: cs + "    }\n}\n", uss: uss + "}\n" };
}

export function executar(conferir = false) {
  const config = JSON.parse(readFileSync(resolve(raiz, "design/interface/config-visual.json")));
  const saida = gerar(config);
  const arquivos = new Map([
    [pastaVisual + "ConfigVisual.cs", saida.cs],
    [pastaVisual + "Resources/Lumora/TokensVisuais.uss", saida.uss],
  ]);
  for (const fonte of ["LumoraNunito-800.ttf", "LumoraSource-500.ttf", "LumoraSource-600.ttf", "LumoraSource-700.ttf", "nunitosans-OFL.txt", "sourcesans3-OFL.txt"])
    arquivos.set(pastaVisual + "Resources/Lumora/Fontes/" + fonte, readFileSync(resolve(raiz, "design/laboratorio-unity/fontes/" + fonte)));
  for (const [caminho, valor] of arquivos) {
    const absoluto = resolve(raiz, caminho);
    if (conferir) {
      // CSharpier é o formatador oficial também para a saída gerada.
      if (caminho.endsWith(".cs")) continue;
      assert.deepEqual(readFileSync(absoluto), Buffer.from(valor), "Saída desatualizada: " + caminho);
    } else {
      mkdirSync(dirname(absoluto), { recursive: true });
      writeFileSync(absoluto, valor);
    }
  }
  if (!conferir) execFileSync("dotnet", ["csharpier", "format", resolve(raiz, pastaVisual + "ConfigVisual.cs")], { stdio: "inherit" });
  // O CSharpier altera apenas whitespace neste arquivo de constantes.
  const atual = readFileSync(resolve(raiz, pastaVisual + "ConfigVisual.cs"), "utf8");
  assert.equal(atual.replace(/\s/g, ""), saida.cs.replace(/\s/g, ""), "ConfigVisual.cs desatualizado");
  for (const arquivo of [pastaVisual + "Resources/Lumora/ComponentesVisuais.uss", "client/Assets/_Game/Features/LaboratorioVisual/Editor/Laboratorio.uss"]) {
    const estilo = readFileSync(resolve(raiz, arquivo), "utf8");
    assert(!/#[0-9a-f]{6}|rgba\(/i.test(estilo), "Cor duplicada fora da configuração: " + arquivo);
    for (const m of estilo.matchAll(/var\((--lumora-[a-z-]+)\)/g))
      assert(saida.uss.includes(m[1] + ":"), "Token inexistente: " + m[1]);
  }
  console.log(conferir ? "Configuração, tokens e fontes sincronizados." : "ConfigVisual, tokens e fontes gerados.");
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) executar(process.argv.includes("--conferir"));
