---
id: T-261104-25bd51-contas-lgpd-e-moderacao-de-comentarios-rf01-rf08
room: feature
created: 2026-11-04T16:50:08.840Z
---

# Goal

Contas, LGPD e moderacao de comentarios (RF01, RF08, RF10)

## Acceptance

- [x] `/Conta/Entrar` (RF01/RF08): e-mail, senha e "manter conectado" (`Input.Email`, `Input.Senha`, `Input.Lembrar`); mensagem de bloqueio após 5 tentativas; `ReturnUrl` só local.
- [x] `/Conta/Cadastrar` (RF08): nome de exibição, e-mail, senha, confirmação e aceite obrigatório da Política de Privacidade; grava `ConsentimentoEm`; entra logado.
- [x] `/Conta/Sair` só por POST; `/Conta/AcessoNegado` explica a falta de permissão.
- [x] `/Conta/Gerenciar` (logado): ver dados, mudar nome, trocar senha e excluir a conta com confirmação de senha, apagando também os comentários; o último administrador não pode excluir a própria conta.
- [x] `/Privacidade`: dados coletados, finalidade, cookies necessários (autenticação, antifalsificação e mensagens), vídeos do YouTube, contador anônimo de visualizações, direitos do titular e autorização de uso de imagem.
- [x] `/Admin/Comentarios`: lista pendentes por padrão, filtro por status, aprovar, rejeitar e excluir por POST.
- [x] Testes de integração cobrem login real por formulário, cadastro, consentimento obrigatório, redirecionamento externo recusado, exclusão de conta e moderação.

## Context

- Brief: `PROJECT-BRIEF.md`. Procedimentos: `.aihaus/memory/project/procedures.md`.
- Base: commit da fundação em `main`. Identity configurado em `Program.cs` (cookie, lockout, senha mínima 8, mensagens em `Services/Localizacao.cs`); `/Conta/Gerenciar` já exige login e `/Admin` exige administrador.
- Cascata de comentários na exclusão do usuário já está em `Data/AppDbContext.cs`.

## Owned files

- `src/FisicoCombat.Web/Pages/Conta/**`
- `src/FisicoCombat.Web/Pages/Privacidade.cshtml`, `Pages/Privacidade.cshtml.cs`
- `src/FisicoCombat.Web/Pages/Admin/Comentarios/**`
- `tests/FisicoCombat.Tests/Conta/**`

## Business-rule gaps

## Log

- 2026-11-04: implementado por subagente implementador; correção por subagente novo a partir de 9913e9f; candidato dab3aa2 -> 6dd4cbf; integrado por PR #4 em main.
- Revisão independente de dab3aa2: FAIL (política citava 2 cookies; app grava 3). Correção em contexto novo (6dd4cbf); reverificação independente: PASS. Ajustes restantes em 7f90fd4, revisado: PASS.
- Verificação final em clone limpo de 7f90fd4 (main 1efc7a8): `dotnet test FisicoCombat.sln --no-build -- xUnit.MaxParallelThreads=2 --filter FullyQualifiedName~FisicoCombat.Tests.Conta` -> 31 aprovados (exit 0); suíte completa 143 aprovados (exit 0); build sem avisos.
- Pendências não bloqueantes registradas em `.aihaus/memory/project/knowledge.md` (Próximos passos).
- Critério da política de privacidade atualizado após a revisão: o app usa 3 cookies necessários (autenticação, antifalsificação e mensagens) e incorpora vídeos do YouTube.

## Evidence

Artifact: evidence/T-261104-25bd51.json
