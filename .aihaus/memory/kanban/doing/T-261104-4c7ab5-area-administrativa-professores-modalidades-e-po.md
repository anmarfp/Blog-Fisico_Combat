---
id: T-261104-4c7ab5-area-administrativa-professores-modalidades-e-po
room: feature
created: 2026-11-04T16:50:08.729Z
---

# Goal

Area administrativa: professores, modalidades e postagens (RF01-RF07)

## Acceptance

- [ ] Painel `/Admin` mostra totais de postagens, professores, modalidades e comentários pendentes (com link para `/Admin/Comentarios`), as 10 postagens mais vistas e o total de visualizações por modalidade.
- [ ] Modalidades (RF04/RF05): listar, criar, editar e excluir com confirmação; slug gerado do nome e único (sufixo -2, -3… em colisão); nome duplicado recusado com mensagem.
- [ ] Professores (RF02/RF03): listar, criar, editar e excluir; foto opcional via `ArmazenamentoImagens`; seleção de várias modalidades; trocar ou excluir remove o arquivo antigo.
- [ ] Postagens (RF06/RF07): notícia ou mídia com título, texto e modalidade opcional; mídia exige foto ou link do YouTube válido (`YouTube.ExtrairId`); foto exige descrição (texto alternativo) e confirmação de autorização de uso de imagem; slug único; `PublicadaEm` em UTC e `AutorId` do usuário logado; editar e excluir (remove a foto).
- [ ] Arquivo que não é JPG/PNG/WebP é recusado com mensagem; nada é salvo.
- [ ] Testes de integração cobrem cada operação, as validações acima e o bloqueio para não administradores.

## Context

- Brief: `PROJECT-BRIEF.md`. Procedimentos: `.aihaus/memory/project/procedures.md`.
- Base: commit da fundação em `main`. Modelos em `src/FisicoCombat.Web/Models/`, contexto em `Data/AppDbContext.cs`, serviços `Services/Slug.cs`, `Services/YouTube.cs`, `Services/ArmazenamentoImagens.cs`.
- `/Admin` já exige o papel Administrador (convenção em `Program.cs`).
- Testes usam `tests/FisicoCombat.Tests/Infra/FabricaApp.cs` (`CriarClienteAdminAsync`, `Formularios.EnviarAsync`, `EnviarMultipartAsync`, `PngFalso`).

## Owned files

- `src/FisicoCombat.Web/Pages/Admin/**` exceto `Pages/Admin/Comentarios/**`
- `tests/FisicoCombat.Tests/Admin/**`

## Business-rule gaps

## Log

## Evidence
