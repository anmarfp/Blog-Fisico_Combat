---
id: T-261104-611cf6-paginas-publicas-busca-postagem-compartilhar-e-c
room: feature
created: 2026-11-04T16:50:08.788Z
---

# Goal

Paginas publicas: busca, postagem, compartilhar e comentar (RF09-RF11)

## Acceptance

- [ ] Início mostra as 6 postagens mais recentes e as modalidades.
- [ ] `/Postagens` lista 12 por página, do mais recente, com filtros `tipo` (noticia|midia), `modalidade` (slug) e `q` (título ou texto); RF09 = `/Postagens?tipo=midia&modalidade=<slug>`.
- [ ] `/Postagens/{id}/{slug?}` exibe a postagem: texto com HTML codificado e quebras de linha em parágrafos, foto com texto alternativo, vídeo em `youtube-nocookie` com `title`; soma 1 em `Visualizacoes` a cada abertura; 404 para id inexistente.
- [ ] Comentários (RF10): só aprovados aparecem; usuário logado envia comentário que fica `Pendente` com aviso de moderação; visitante vê link para `/Conta/Entrar?ReturnUrl=…`; envio sem login não grava nada.
- [ ] Compartilhar (RF11): links para WhatsApp, Facebook e X, botão copiar link e botão de compartilhamento nativo (Web Share API) quando disponível; metatags Open Graph.
- [ ] `/Modalidades`, `/Modalidades/{slug}` (descrição, professores, postagens) e `/Professores` (foto, nome, modalidades).
- [ ] Testes de integração cobrem filtros, exibição, contador, comentários (logado, anônimo, só aprovados) e 404.

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

## Evidence
