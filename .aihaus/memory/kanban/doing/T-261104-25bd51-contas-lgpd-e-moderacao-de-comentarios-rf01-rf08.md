---
id: T-261104-25bd51-contas-lgpd-e-moderacao-de-comentarios-rf01-rf08
room: feature
created: 2026-11-04T16:50:08.840Z
---

# Goal

Contas, LGPD e moderacao de comentarios (RF01, RF08, RF10)

## Acceptance

- [ ] `/Conta/Entrar` (RF01/RF08): e-mail, senha e "manter conectado" (`Input.Email`, `Input.Senha`, `Input.Lembrar`); mensagem de bloqueio após 5 tentativas; `ReturnUrl` só local.
- [ ] `/Conta/Cadastrar` (RF08): nome de exibição, e-mail, senha, confirmação e aceite obrigatório da Política de Privacidade; grava `ConsentimentoEm`; entra logado.
- [ ] `/Conta/Sair` só por POST; `/Conta/AcessoNegado` explica a falta de permissão.
- [ ] `/Conta/Gerenciar` (logado): ver dados, mudar nome, trocar senha e excluir a conta com confirmação de senha, apagando também os comentários; o último administrador não pode excluir a própria conta.
- [ ] `/Privacidade`: dados coletados, finalidade, cookies (só autenticação e antifalsificação), contador anônimo de visualizações, direitos do titular e autorização de uso de imagem.
- [ ] `/Admin/Comentarios`: lista pendentes por padrão, filtro por status, aprovar, rejeitar e excluir por POST.
- [ ] Testes de integração cobrem login real por formulário, cadastro, consentimento obrigatório, redirecionamento externo recusado, exclusão de conta e moderação.

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

## Evidence
