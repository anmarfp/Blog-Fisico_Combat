# Knowledge

## Verified facts

- Verified: o blog grava exatamente 3 cookies próprios (`fisicocombat.auth`, `.AspNetCore.Antiforgery.*`, `.AspNetCore.Mvc.CookieTempDataProvider`); o último nasce de qualquer `TempData["Mensagem"]`. `tests/FisicoCombat.Tests/Conta/GerenciarTests.cs` (`PrivacidadeTests`) falha se surgir cookie não documentado em `Pages/Privacidade.cshtml` — ao adicionar cookie, atualize a política. Source: `src/FisicoCombat.Web/Program.cs`, `src/FisicoCombat.Web/Pages/Privacidade.cshtml` (reviewed 1efc7a8).
- Verified: datas são gravadas em UTC e exibidas por `Services/HoraLocal.DeUtc` (Brasília, com fallback para Windows sem ICU e UTC−3). Não use `FindSystemTimeZoneById` direto. Source: `src/FisicoCombat.Web/Services/HoraLocal.cs` (reviewed 1efc7a8).
- Verified: `Postagens.Slug` e `Modalidades.Slug`/`Nome` têm índice único; dados de teste precisam de slugs únicos (ex.: sufixo de Guid). Source: `src/FisicoCombat.Web/Data/AppDbContext.cs` (reviewed 1efc7a8).
- Verified: `tests/FisicoCombat.Tests/Infra/FabricaApp.cs` autentica pelo cabeçalho `X-Teste-Usuario` (procura o usuário no banco a cada requisição); sem o cabeçalho, vale o cookie real do Identity. Um usuário excluído vira anônimo nesse esquema, então ele não simula "cookie válido de conta excluída". Source: `tests/FisicoCombat.Tests/Infra/FabricaApp.cs` (reviewed 1efc7a8).

## Recurring gotchas

- Git Bash no Windows passa argumentos de `curl --data-urlencode` em Windows-1252: acentos chegam corrompidos ao servidor (ex.: slug `inscri-e7-f5es`). Para enviar formulários em testes manuais, use `fetch` do Node (UTF-8). Evidência: teste de fumaça de 2026-11-04.
- Edge em modo headless renderiza com viewport mínima de 496 px mesmo com `--window-size=390,…` e corta a imagem; capturas de celular devem usar `--window-size=500,…`. Evidência: `innerWidth=496` medido em 2026-11-04.
- `docs/` e `Docs/` são a mesma pasta no Windows; `/Docs/` (PDFs da UNINTER) é ignorado pelo Git, então documentação versionada fica em `documentacao/`.
- Shells abertos antes da instalação do .NET não têm `dotnet` no PATH: use `export PATH="$PATH:/c/Program Files/dotnet"`.
- Arquivos com CRLF: substituições por script precisam tratar `\r\n` (o `.gitattributes` normaliza no commit).
- `await` não é permitido em filtro `catch (...) when (...)` no C#; faça a checagem dentro do bloco `catch`.
- GitHub Actions pode ficar minutos em `queued`; `gh run rerun` não funciona enquanto está na fila. Acompanhe com laço de `gh run view` em segundo plano.

## Useful code analogs

- CRUD administrativo com upload e regras de imagem: `src/FisicoCombat.Web/Pages/Admin/Postagens/FormularioPostagemModel.cs` (validação completa antes de salvar o arquivo; arquivo antigo apagado só depois do `SaveChanges`).
- Teste de formulário com antiforgery e multipart: `tests/FisicoCombat.Tests/Infra/FabricaApp.cs` (`Formularios.EnviarAsync`, `EnviarMultipartAsync`).
- Operação atômica com Identity + SQLite: `src/FisicoCombat.Web/Pages/Conta/Gerenciar/Index.cshtml.cs` (exclusão do último administrador em `BeginTransactionAsync`).

## Próximos passos (não bloqueantes, registrados em 2026-11-04)

- Publicar o blog (Azure App Service, camada gratuita): exige conta Azure do responsável e aprovação explícita; configurar `ConnectionStrings__Padrao` e `Armazenamento__PastaUploads` em `/home/data`, `AdminInicial__*` e `AllowedHosts`.
- Política de privacidade: incluir contato do controlador (e-mail/WhatsApp do CT) — depende do responsável.
- Cadastro: verificação de idade/consentimento do responsável para menores (LGPD art. 14) — decisão do responsável.
- Comentário com cookie válido de conta excluída: hoje cai no login e volta ao início sem mensagem; encerrar a sessão (`SignOut`) e testar com cookie real.
- Limite de envio de comentários por usuário (fila de moderação).
- Paginação nas listas do admin e da moderação; checagem de bloqueio antes de validar senha em "Minha conta"; equalizar tempo de login para e-mail inexistente.

Each entry should name its evidence and last review date. Transient research
belongs in the task that used it, not here.
