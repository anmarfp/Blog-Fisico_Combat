#!/usr/bin/env node
// Conta SLOC (linhas de código-fonte, sem linhas em branco e sem comentários) do blog.
// Uso: node ferramentas/sloc.mjs [--json]
// Exclui bibliotecas de terceiros (wwwroot/lib), migrações geradas, bin/ e obj/.
import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, extname } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = fileURLToPath(new URL("..", import.meta.url));
const pastas = { "src/FisicoCombat.Web": "Aplicação", "tests/FisicoCombat.Tests": "Testes" };
const ignorar = [/[\\/](bin|obj|lib|Migrations)[\\/]/];
const linguagens = { ".cs": "C#", ".cshtml": "Razor", ".css": "CSS", ".js": "JavaScript" };

// shortcut: reconhece comentários de linha inteira e blocos /* */ e @* *@ que começam no início da linha;
// comentários no fim de uma linha de código contam como código. Suficiente para SLOC físico.
function contar(texto) {
  let sloc = 0;
  let emBloco = null;
  for (const bruta of texto.split(/\r?\n/)) {
    const l = bruta.trim();
    if (emBloco) {
      if (l.includes(emBloco)) emBloco = null;
      continue;
    }
    if (l === "" || l.startsWith("//")) continue;
    if (l.startsWith("/*")) { if (!l.includes("*/")) emBloco = "*/"; continue; }
    if (l.startsWith("@*")) { if (!l.includes("*@")) emBloco = "*@"; continue; }
    if (l.startsWith("<!--")) { if (!l.includes("-->")) emBloco = "-->"; continue; }
    sloc++;
  }
  return sloc;
}

function* arquivos(dir) {
  for (const nome of readdirSync(dir)) {
    const caminho = join(dir, nome);
    if (ignorar.some((r) => r.test(caminho + "/"))) continue;
    if (statSync(caminho).isDirectory()) yield* arquivos(caminho);
    else if (linguagens[extname(nome)]) yield caminho;
  }
}

const resultado = { total: 0, porParte: {}, porLinguagem: {}, arquivos: 0 };
for (const [pasta, parte] of Object.entries(pastas)) {
  for (const arquivo of arquivos(join(raiz, pasta))) {
    const n = contar(readFileSync(arquivo, "utf8"));
    const lang = linguagens[extname(arquivo)];
    resultado.total += n;
    resultado.arquivos++;
    resultado.porParte[parte] = (resultado.porParte[parte] ?? 0) + n;
    resultado.porLinguagem[lang] = (resultado.porLinguagem[lang] ?? 0) + n;
  }
}
resultado.ksloc = Number((resultado.total / 1000).toFixed(2));

if (process.argv.includes("--json")) {
  console.log(JSON.stringify(resultado, null, 2));
} else {
  console.log(`Arquivos: ${resultado.arquivos}`);
  for (const [k, v] of Object.entries(resultado.porParte)) console.log(`${k}: ${v} SLOC`);
  for (const [k, v] of Object.entries(resultado.porLinguagem)) console.log(`  ${k}: ${v}`);
  console.log(`Total: ${resultado.total} SLOC (${resultado.ksloc} KSLOC)`);
}
