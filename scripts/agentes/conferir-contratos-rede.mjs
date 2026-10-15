// Garante que todo NetManager da LiteNetLib (servidor, client Unity e client de teste) usa
// TransporteTempoReal.Canais do shared: com canais a menos, quem recebe descarta em silêncio
// as posições fora dos primeiros canais (#826). O valor mora só no shared, então mudar o
// número lá basta; o que não pode é um lado voltar ao padrão ou a um literal.
//
// Uso: npm run conferir-contratos-rede

import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = join(dirname(fileURLToPath(import.meta.url)), "../..");

export const SHARED = "shared/Package/Runtime/Core/TempoReal/TransporteTempoReal.cs";

export const NET_MANAGERS = [
  "server/src/JogoDoGrupo.Server/Core/TempoReal/ServidorTempoReal.cs",
  "client/Assets/_Game/Core/Scripts/TempoReal/TransporteLiteNetLib.cs",
  "tests/JogoDoGrupo.Server.Tests/Infra/ClienteTempoRealDeTeste.cs",
];

const PADRAO_CANAIS = /ChannelsCount\s*=\s*TransporteTempoReal\.Canais/;
const PADRAO_ATRIBUICAO = /ChannelsCount\s*=\s*(?!TransporteTempoReal\.Canais)\S/;

/**
 * @param {string} shared conteúdo de TransporteTempoReal.cs
 * @param {Record<string, string>} netManagers caminho → conteúdo
 */
export function verificarCanais(shared, netManagers) {
  const erros = [];
  if (!/public const byte Canais\b/.test(shared)) {
    erros.push(`${SHARED}: falta TransporteTempoReal.Canais.`);
  }
  for (const [caminho, texto] of Object.entries(netManagers)) {
    if (PADRAO_ATRIBUICAO.test(texto)) {
      erros.push(`${caminho}: ChannelsCount com outro valor; use TransporteTempoReal.Canais.`);
    } else if (!PADRAO_CANAIS.test(texto)) {
      erros.push(`${caminho}: falta ChannelsCount = TransporteTempoReal.Canais.`);
    }
  }
  return erros;
}

/** Arquivos C# fora de ThirdParty que atribuem ChannelsCount, além dos conhecidos. */
function outrosNetManagers() {
  try {
    const saida = execFileSync(
      "git",
      ["grep", "-l", "-E", "ChannelsCount *=", "--", "*.cs", ":!**/ThirdParty/**"],
      { cwd: raiz, encoding: "utf8" },
    );
    return saida
      .split("\n")
      .map((l) => l.trim())
      .filter((l) => l && !NET_MANAGERS.includes(l));
  } catch {
    return [];
  }
}

function main() {
  const ler = (relativo) => readFileSync(join(raiz, relativo), "utf8");
  const caminhos = [...NET_MANAGERS, ...outrosNetManagers()];
  const erros = verificarCanais(
    ler(SHARED),
    Object.fromEntries(caminhos.map((c) => [c, ler(c)])),
  );

  if (erros.length) {
    console.error("conferir-contratos-rede: falhou\n" + erros.map((e) => `- ${e}`).join("\n"));
    process.exit(1);
  }
  console.log("conferir-contratos-rede: ok");
}

if (process.argv[1]?.includes("conferir-contratos-rede.mjs")) {
  main();
}
