---
id: T-261104-611cf6-paginas-publicas-busca-postagem-compartilhar-e-c
room: feature
created: 2026-11-04T16:50:08.788Z
---

# Goal

Paginas publicas: busca, postagem, compartilhar e comentar (RF09-RF11)

## Acceptance

- [x] Início mostra as 6 postagens mais recentes e as modalidades.
- [x] `/Postagens` lista 12 por página, do mais recente, com filtros `tipo` (noticia|midia), `modalidade` (slug) e `q` (título ou texto); RF09 = `/Postagens?tipo=midia&modalidade=<slug>`.
- [x] `/Postagens/{id}/{slug?}` exibe a postagem: texto com HTML codificado e quebras de linha em parágrafos, foto com texto alternativo, vídeo em `youtube-nocookie` com `title`; soma 1 em `Visualizacoes` a cada abertura; 404 para id inexistente.
- [x] Comentários (RF10): só aprovados aparecem; usuário logado envia comentário que fica `Pendente` com aviso de moderação; visitante vê link para `/Conta/Entrar?ReturnUrl=…`; envio sem login não grava nada.
- [x] Compartilhar (RF11): links para WhatsApp, Facebook e X, botão copiar link e botão de compartilhamento nativo (Web Share API) quando disponível; metatags Open Graph.
- [x] `/Modalidades`, `/Modalidades/{slug}` (descrição, professores, postagens) e `/Professores` (foto, nome, modalidades).
- [x] Testes de integração cobrem filtros, exibição, contador, comentários (logado, anônimo, só aprovados) e 404.

## Context

- Brief: `PROJECT-BRIEF.md`. Procedimentos: `.aihaus/memory/project/procedures.md`.
- Base: commit da fundação em `main`. Ver modelos em `src/FisicoCombat.Web/Models/` e `Services/YouTube.cs` (`UrlIncorporacao`).
- O layout (`Pages/Shared/_Layout.cshtml`) já tem `@RenderSection("Meta")`, mensagem via `TempData["Mensagem"]` e links do menu para estas páginas.

## Owned files

- `src/FisicoCombat.Web/Pages/Index.cshtml`, `Pages/Index.cshtml.cs`
- `src/FisicoCombat.Web/Pages/Postagens/**`, `Pages/Modalidades/**`, `Pages/Professores/**`
- Novos parciais em `src/FisicoCombat.Web/Pages/Shared/` com prefixo `_Publico`
- `src/FisicoCombat.Web/wwwroot/css/site.css` (apenas acréscimos), `wwwroot/js/site.js`
- `tests/FisicoCombat.Tests/Publico/**`

## Business-rule gaps

## Log

- 2026-11-04: implementado por subagente implementador (worktree própria) a partir de 9913e9f; candidato 7b1df15; integrado por PR #2 em main.
- Revisão independente somente leitura no SHA 7b1df15: PASS, sem achados bloqueantes. Ajustes não bloqueantes (comentário de conta excluída, canonical, resumo) integrados em 7f90fd4, revisado: PASS.
- Verificação final em clone limpo de 7f90fd4 (main 1efc7a8): `dotnet test FisicoCombat.sln --no-build -- xUnit.MaxParallelThreads=2 --filter FullyQualifiedName~FisicoCombat.Tests.Publico` -> 37 aprovados (exit 0); suíte completa 143 aprovados (exit 0); build sem avisos.
- Pendências não bloqueantes registradas em `.aihaus/memory/project/knowledge.md` (Próximos passos).

## Evidence

Artifact: evidence/T-261104-611cf6.json
