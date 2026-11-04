# Procedures

## Development closeout

1. Run affected checks.
2. Run broader repository checks required by the Definition of Done.
3. Record commands, exit codes, and degraded checks.
4. Review the diff and update durable memory only when warranted.

## Operational procedures

Add repeatable, verified procedures with prerequisites, rollback, and stop
conditions. Do not copy one-off terminal history here.

### Build e testes (verificado em 2026-11-04)

- Pré-requisito: .NET 10 SDK (10.0.403). Em shells abertos antes da instalação,
  acrescente `C:\Program Files\dotnet` ao PATH.
- `dotnet build FisicoCombat.sln -v q` (exit 0 esperado, sem avisos).
- `dotnet test FisicoCombat.sln -- xUnit.MaxParallelThreads=2` (143 testes em ~10 s; paralelismo
  limitado; cada classe de teste sobe o app com SQLite em memória próprio via
  `tests/FisicoCombat.Tests/Infra/FabricaApp.cs`).
- Migrações: `dotnet ef migrations add <Nome> --project src/FisicoCombat.Web
  --output-dir Data/Migrations` (ferramenta local em `dotnet-tools.json`; rode
  `dotnet tool restore` antes). O app aplica migrações na inicialização.
- CI: `.github/workflows/ci.yml` roda build e testes em push para `main` e em PRs.

### Integração

- Repositório: https://github.com/anmarfp/Blog-Fisico_Combat (público).
- Branch de integração: `main`. Trabalho em branch própria por tarefa, PR para
  `main`, merge após CI verde e revisão.

### Fluxo de entrega com agentes (usado em 2026-11-04, PRs #1–#5)

1. Tarefa no kanban com Acceptance, Context e Owned files disjuntos; commit da
   tarefa em `main` antes de criar a worktree; mover para `doing`.
2. Executor em worktree isolada a partir do SHA base; um commit, sem push;
   relatório com SHA, comando de teste + exit code e
   `node .aihaus/tools/scope-check.mjs --base <sha> --allow <pastas> --json`.
3. Orquestrador confere escopo/diff e dispara revisor somente leitura no SHA
   candidato. FAIL → correção por agente novo na mesma worktree (novo commit)
   → reverificação. Limite: 2 ciclos.
4. Ensaio de integração: mesclar candidatos num branch local temporário e rodar
   a suíte completa antes de abrir PRs.
5. Push do SHA revisado para branch nomeada (`git push origin <sha>:refs/heads/<nome>`),
   PR com resumo + verificação, CI verde, `gh pr merge --merge --delete-branch`.
6. Ajustes transversais (fuso, índices, Program.cs) num PR próprio do
   orquestrador, também com revisão independente.
7. Verificação final em clone limpo; `evidence/<tarefa>.json`
   (`node .aihaus/tools/evidence-validate.mjs`), mover tarefa para `done`,
   remover worktrees já integradas (`git worktree remove`, `git branch -D`).

### Atualizar o pacote aihaus

- npm 12 bloqueia tarballs por URL: use `npm exec --yes --allow-remote=root
  --package=<url do .tgz da release> -- aihaus init --target . --json`
  (rode antes com `--check`).

## Assignments

- Sessão do usuário: orquestrador. Executores: subagentes de implementação em
  worktrees isoladas, no máximo 4 em paralelo, um commit descritivo cada, sem push.
- Revisão: agente separado, somente leitura, sobre o SHA candidato.
- Testes: `xUnit.MaxParallelThreads=2`.

## Autonomy

- 2026-11-04, Marco Antônio Ferreira Pinto: autorizou o orquestrador a fazer
  push, abrir PRs e fazer merge em `main` do repositório acima e encerrar as
  worktrees (pedido de orquestração da sessão; repositório criado pelo
  responsável para este fim). Não autoriza deploy em produção nem uso de
  credenciais de nuvem; esses pedem aprovação explícita.
