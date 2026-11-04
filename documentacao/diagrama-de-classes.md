# Diagrama de classes

Modelo de domínio do blog Físico Combat, elaborado a partir dos requisitos da
Atividade Extensionista II (RF01–RF11). Fonte: `src/FisicoCombat.Web/Models/`.

```mermaid
classDiagram
    direction LR

    class IdentityUser {
        <<ASP.NET Core Identity>>
        +string Id
        +string Email
        +string UserName
        +string PasswordHash
    }

    class Usuario {
        +string NomeExibicao
        +DateTime? ConsentimentoEm
    }

    class Papel {
        <<enumeração de papéis>>
        Administrador
    }

    class Modalidade {
        +int Id
        +string Nome
        +string Slug
        +string? Descricao
    }

    class Professor {
        +int Id
        +string Nome
        +string? Biografia
        +string? FotoCaminho
    }

    class Postagem {
        +int Id
        +TipoPostagem Tipo
        +string Titulo
        +string Slug
        +string? Conteudo
        +string? ImagemCaminho
        +string? ImagemDescricao
        +string? YoutubeId
        +DateTime PublicadaEm
        +int Visualizacoes
    }

    class Comentario {
        +int Id
        +string Texto
        +DateTime CriadoEm
        +StatusComentario Status
    }

    class TipoPostagem {
        <<enumeration>>
        Noticia
        Midia
    }

    class StatusComentario {
        <<enumeration>>
        Pendente
        Aprovado
        Rejeitado
    }

    IdentityUser <|-- Usuario
    Usuario "0..*" -- "0..*" Papel : possui
    Professor "0..*" -- "0..*" Modalidade : ensina
    Modalidade "0..1" -- "0..*" Postagem : classifica
    Usuario "0..1" -- "0..*" Postagem : publica
    Postagem "1" *-- "0..*" Comentario : recebe
    Usuario "1" *-- "0..*" Comentario : escreve
    Postagem ..> TipoPostagem
    Comentario ..> StatusComentario
```

## Regras representadas

| Relação | Regra |
|---|---|
| Professor ↔ Modalidade | Muitos para muitos: um professor ensina várias modalidades e uma modalidade tem vários professores. |
| Modalidade → Postagem | Opcional. Ao excluir uma modalidade, as postagens continuam, sem modalidade. |
| Usuario → Postagem | O administrador que publicou. Se a conta for excluída, a postagem continua. |
| Postagem ◆ Comentario | Composição: ao excluir a postagem, seus comentários são excluídos. |
| Usuario ◆ Comentario | Composição: ao excluir a conta (LGPD), os comentários do usuário são excluídos. |
| Comentario.Status | Todo comentário nasce `Pendente` e só aparece no blog depois de `Aprovado` pelo administrador. |
| Postagem.Tipo | `Midia` exige foto ou vídeo do YouTube; `Noticia` é texto com foto opcional. |

## Rastreabilidade dos requisitos

| Requisito | Classes |
|---|---|
| RF01 Login como administrador | Usuario, Papel (Administrador) |
| RF02, RF03 Professores | Professor |
| RF04, RF05 Modalidades | Modalidade |
| RF06 Postar mídia | Postagem (Tipo = Midia) |
| RF07 Postar notícia | Postagem (Tipo = Noticia) |
| RF08 Login como usuário comum | Usuario |
| RF09 Buscar mídia por modalidade | Postagem, Modalidade |
| RF10 Comentar na postagem | Comentario |
| RF11 Compartilhar postagem | Postagem (Slug, para o link) |
