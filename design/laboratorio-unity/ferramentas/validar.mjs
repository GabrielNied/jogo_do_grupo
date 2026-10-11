import assert from "node:assert/strict";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const feature = "client/Assets/_Game/Features/LaboratorioVisual/";

export function conferirFonte(bytes, peso) {
  assert(bytes.length >= 12 && bytes.readUInt32BE(0) === 0x00010000, "TTF inválida ou ponteiro LFS");
  const tabelas = new Map();
  const quantidade = bytes.readUInt16BE(4);
  assert(12 + quantidade * 16 <= bytes.length, "Diretório TTF truncado");
  for (let i = 0; i < quantidade; i++) {
    const inicio = 12 + i * 16;
    const tag = bytes.toString("ascii", inicio, inicio + 4);
    const offset = bytes.readUInt32BE(inicio + 8);
    const length = bytes.readUInt32BE(inicio + 12);
    assert(offset + length <= bytes.length, "Tabela TTF truncada");
    tabelas.set(tag, { offset, length });
  }
  assert(!tabelas.has("fvar"), "Fonte variável onde deveria haver instância estática");
  const os2 = tabelas.get("OS/2");
  assert(os2 && os2.length >= 6, "Peso da fonte ausente");
  assert.equal(bytes.readUInt16BE(os2.offset + 4), peso, "Peso diferente do aprovado");
}

export function contraste(a, b) {
  function luminancia(hex) {
    assert.match(hex, /^#[0-9a-f]{6}$/i);
    const canais = [1, 3, 5].map(i => parseInt(hex.slice(i, i + 2), 16) / 255)
      .map(c => c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
    return canais[0] * 0.2126 + canais[1] * 0.7152 + canais[2] * 0.0722;
  }
  const x = luminancia(a), y = luminancia(b);
  return (Math.max(x, y) + 0.05) / (Math.min(x, y) + 0.05);
}

export function validar() {
  const ler = caminho => readFileSync(resolve(raiz, caminho));
  const manifesto = JSON.parse(ler("design/laboratorio-unity/recursos.json"));
  for (const recurso of manifesto.recursos) {
    const bytes = ler("design/" + recurso.caminho);
    assert.equal(createHash("sha256").update(bytes).digest("hex"), recurso.sha256,
      "Recurso alterado ou ponteiro LFS: " + recurso.caminho);
    if (recurso.peso) conferirFonte(bytes, recurso.peso);
  }
  const assembly = JSON.parse(ler(feature + "Editor/JogoDoGrupo.Client.LaboratorioVisual.Editor.asmdef"));
  assert.deepEqual(assembly.includePlatforms, ["Editor"], "Laboratório vazando para o build");
  assert.deepEqual(assembly.references, [], "Laboratório depende de outra funcionalidade");
  const tela = ler(feature + "Editor/TelaLaboratorio.cs").toString();
  assert(!/RuntimeInitializeOnLoadMethod|InitializeOnLoad|UnityWebRequest|HttpClient/.test(tela), "Integração automática ou rede proibida");
  const estilo = ler(feature + "Editor/Laboratorio.uss").toString();
  for (const cor of ["#173B50", "#39B9AE", "#F5BE63", "#F4E6CB", "#688A48"])
    assert(estilo.includes(cor), "Token de paleta ausente: " + cor);
  for (const par of [["#F4E6CB", "#173B50"], ["#173B50", "#F5BE63"], ["#C7D6D5", "#173B50"]]) {
    const ratio = contraste(...par);
    assert(ratio >= 4.5, "Contraste sólido insuficiente: " + par);
    console.log(par.join(" / ") + ": " + ratio.toFixed(2) + ":1");
  }
  console.log("Recursos, fontes estáticas, paleta e isolamento verificados. Renderização Unity permanece uma etapa independente.");
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) validar();
