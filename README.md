# Blog Físico Combat

Blog de divulgação do Centro de Treinamento de artes marciais **Físico Combat**,
em Guaratiba, Rio de Janeiro – RJ. Projeto da Atividade Extensionista III
(Análise) do Bacharelado em Engenharia de Software da UNINTER, alinhado aos
ODS 3 (Saúde e bem-estar) e 4 (Educação de qualidade).

## Funcionalidades

| ID | Requisito |
|---|---|
| RF01 | Login como administrador |
| RF02 / RF03 | Adicionar, editar e excluir professores |
| RF04 / RF05 | Adicionar, editar e excluir modalidades |
| RF06 / RF07 | Postar mídias (foto ou vídeo do YouTube) e notícias |
| RF08 | Cadastro e login de usuário comum |
| RF09 | Buscar mídias por modalidade |
| RF10 | Comentar nas postagens (com moderação) |
| RF11 | Compartilhar postagens |
| RNF01 | Paleta preto, vermelho e branco com contraste acessível |

## Tecnologias

C# e ASP.NET Core 10 (Razor Pages), Entity Framework Core com SQLite,
ASP.NET Core Identity, HTML, CSS, JavaScript e Bootstrap 5. Testes com xUnit.

## Como executar

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# administrador inicial (fica fora do repositório, em user-secrets)
dotnet user-secrets --project src/FisicoCombat.Web set "AdminInicial:Email" "seu-email@exemplo.com"
dotnet user-secrets --project src/FisicoCombat.Web set "AdminInicial:Senha" "uma-senha-forte"

dotnet run --project src/FisicoCombat.Web
```

O banco `fisicocombat.db` e a pasta de fotos `wwwroot/uploads/` são criados na
primeira execução.

Em produção, configure por variáveis de ambiente:

| Variável | Uso |
|---|---|
| `ConnectionStrings__Padrao` | Conexão SQLite, ex.: `Data Source=/home/data/fisicocombat.db` |
| `Armazenamento__PastaUploads` | Pasta persistente das fotos, ex.: `/home/data/uploads` |
| `AdminInicial__Email` / `AdminInicial__Senha` | Administrador criado na primeira execução |

## Testes

```bash
dotnet test FisicoCombat.sln -- xUnit.MaxParallelThreads=2
```

## Estrutura

```text
src/FisicoCombat.Web/     aplicação (Models, Data, Services, Pages, wwwroot)
tests/FisicoCombat.Tests/ testes de unidade e de integração
```
