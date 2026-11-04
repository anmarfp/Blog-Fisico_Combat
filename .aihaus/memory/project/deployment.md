# Deployment

## Environments

- Produção: MonsterASP.NET, plano gratuito, site `site105006`, endereço público http://fisico-combat.runasp.net
  (IIS Windows, pool 32 bits, hosting in-process, servidor na Europa, dorme após 3 h sem acesso,
  `site105006.siteasp.net` é só o servidor de Web Deploy (porta 8172); a raiz dele mostra a página de
  propaganda do MonsterASP e não é o site. HTTPS no plano grátis: pedido ao suporte.
  256 MB de memória). Decidido pelo responsável em 2026-11-04 depois que a assinatura Azure
  Free Trial recusou o App Service F1 (cota 0 em todas as regiões testadas).
- O grupo de recursos vazio `rg-fisicocombat` (Brazil South) ficou na assinatura Azure; não gera custo.

## Preconditions

- Segredos do repositório GitHub: `WEBSITE_NAME`, `SERVER_COMPUTER_NAME`
  (`https://site105006.siteasp.net:8172`), `SERVER_USERNAME`, `SERVER_PASSWORD` (Web Deploy,
  cadastrado pelo responsável; nunca passa pelo agente).
- Variáveis de ambiente no painel do MonsterASP: `ConnectionStrings__Padrao`
  (`Data Source=App_Data/fisicocombat.db`), `AllowedHosts`, `AdminInicial__Email`,
  `AdminInicial__Senha` (definida pelo responsável).

## Release procedure

- Merge na `main` → job `deploy` do `.github/workflows/ci.yml` (reviewed 2026-11-04): publica
  `win-x86` framework-dependent e envia por Web Deploy com
  `rasmusbuchholdt/simply-web-deploy` fixada no commit da versão 2.2.0. A ação para o pool,
  sincroniza com `DoNotDeleteRule` e religa o pool.

## Smoke checks

- `GET /` e `GET /Postagens` → 200 e título "Início | Físico Combat" (não basta o 200: confira o conteúdo); login do administrador; `/Admin` → 200 logado.
- Erro 500.30 na partida: definir temporariamente `ASPNETCORE_ENVIRONMENT=Development` no painel para ver a exceção e remover em seguida. Em 2026-11-04 a causa foi `AdminInicial__Senha` sem letra minúscula.
- Depois de uma segunda publicação, o banco e as fotos continuam lá (DoNotDeleteRule).

## Rollback

- Reverter o commit na `main` (novo PR) e deixar o job publicar de novo. O banco não é
  versionado: faça cópia de `App_Data/fisicocombat.db` pelo File manager antes de mudanças de
  migração.

## Approval policy

- Publicação em produção autorizada pelo responsável em 2026-11-04 via merge na `main`.
  Credenciais ficam nos segredos do GitHub e no painel do MonsterASP.

Instructions are not containment. Name the external controls and credential
scope required for each environment.
