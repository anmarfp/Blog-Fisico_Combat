# Blog Físico Combat — Project Brief

Responsável: Marco Antônio Ferreira Pinto (RU 4411500). Registrado em 2026-11-04
a partir das respostas do responsável e dos documentos em `Docs/` (não versionados).

## Propósito

Blog de divulgação do Centro de Treinamento (CT) de artes marciais Físico Combat,
em Guaratiba, Rio de Janeiro – RJ. Desenvolvido como Atividade Extensionista III
(Análise) do Bacharelado em Engenharia de Software da UNINTER, ODS 3 (Saúde e
bem-estar) e ODS 4 (Educação de qualidade).

## Usuários

- Administrador (proprietário e professores do CT): publica notícias e mídias,
  mantém professores e modalidades, modera comentários.
- Usuário comum (alunos, responsáveis e comunidade): lê, busca, compartilha e,
  com conta, comenta.

## Escopo (todos os requisitos da AE II)

| ID | Requisito |
|---|---|
| RF01 | Login como administrador |
| RF02 | Adicionar professor |
| RF03 | Editar/excluir professor |
| RF04 | Adicionar modalidade |
| RF05 | Editar/excluir modalidade |
| RF06 | Postar mídia |
| RF07 | Postar notícia |
| RF08 | Login como usuário comum |
| RF09 | Buscar mídia por modalidade |
| RF10 | Comentar na postagem |
| RF11 | Compartilhar postagem |
| RNF01 | Paleta de cores em preto, vermelho e branco |

Decidido pelo responsável em 2026-11-04: a primeira versão inclui RF08 e RF10,
com moderação de comentários pelo administrador.

## Objetivos da AE III (proposta validada, `Docs/`)

1. Elaborar o diagrama de classes com base nos requisitos da AE II.
2. Desenvolver e implantar o blog em C# com ASP.NET Core, usando HTML, CSS,
   JavaScript e Bootstrap.
3. Gerenciar a configuração com Git e GitHub
   (https://github.com/anmarfp/Blog-Fisico_Combat, público).
4. Avaliar a qualidade com testes automatizados e métricas de código (SLOC/KSLOC).
5. Analisar o interesse da população por questionário on-line e estatísticas de
   acesso ao blog.

## Decisões técnicas confirmadas pelo responsável (2026-11-04)

- ASP.NET Core com Razor Pages renderizadas no servidor e Bootstrap.
- Banco SQLite.
- Fotos enviadas pelo administrador; vídeos por link do YouTube.
- Interface em português do Brasil.
- Paleta preto/vermelho/branco ajustada para contraste acessível (WCAG AA).
- Hospedagem alvo: Azure App Service (camada gratuita).

## Restrições

- LGPD: dados pessoais mínimos, consentimento no cadastro, exclusão da conta pelo
  próprio usuário; fotos de alunos exigem autorização de uso de imagem.
- Nenhuma senha ou segredo no repositório; o administrador inicial vem de
  configuração externa.
- Os PDFs em `Docs/` (manual da UNINTER, exemplo de terceiro, trabalhos com RU)
  não são versionados no repositório público.

## Definição de pronto

- Requisitos RF01–RF11 e RNF01 funcionando, com testes automatizados passando.
- Código no GitHub com histórico de commits e pull requests.
- Blog publicado e acessível pela internet.
