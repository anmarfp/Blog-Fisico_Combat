using System.Net;
using System.Text;
using FisicoCombat.Tests.Infra;
using FisicoCombat.Web.Models;
using Microsoft.EntityFrameworkCore;
using static FisicoCombat.Tests.Admin.Auxiliar;

namespace FisicoCombat.Tests.Admin;

public class PostagensTests(FabricaApp app) : IClassFixture<FabricaApp>
{
    private const string MensagemAutorizacao = "Confirme a autorização de uso de imagem para enviar a foto.";

    private Task<Postagem?> BuscarAsync(string titulo) =>
        app.ExecutarAsync(db => db.Postagens.SingleOrDefaultAsync(p => p.Titulo == titulo));

    private Task<HttpResponseMessage> CriarAsync(HttpClient cliente, Dictionary<string, string> campos) =>
        Formularios.EnviarAsync(cliente, "/Admin/Postagens/Criar", campos);

    private static Dictionary<string, string> Noticia(string titulo) => new()
    {
        ["Postagem.Tipo"] = "Noticia",
        ["Postagem.Titulo"] = titulo,
        ["Postagem.Conteudo"] = "Primeiro parágrafo.\n\nSegundo parágrafo.",
    };

    private static Dictionary<string, string> MidiaComFoto(string titulo) => new()
    {
        ["Postagem.Tipo"] = "Midia",
        ["Postagem.Titulo"] = titulo,
        ["Postagem.ImagemDescricao"] = "Alunos no tatame",
        ["AutorizacaoImagem"] = "true",
    };

    [Fact]
    public async Task Cria_noticia_com_autor_data_utc_e_modalidade()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var modalidadeId = await app.ExecutarAsync(async db =>
        {
            var m = new Modalidade { Nome = "Jiu-Jitsu", Slug = "jiu-jitsu" };
            db.Modalidades.Add(m);
            await db.SaveChangesAsync();
            return m.Id;
        });
        var adminId = await app.ExecutarAsync(db => db.Users.Where(u => u.Email == FabricaApp.AdminEmail).Select(u => u.Id).SingleAsync());
        var inicio = DateTime.UtcNow;

        var campos = Noticia("Campeonato Estadual: Ação!");
        campos["Postagem.ModalidadeId"] = modalidadeId.ToString();
        var resposta = await CriarAsync(cliente, campos);

        AssertRedirecionaPara(resposta, "/Admin/Postagens");
        var postagem = await BuscarAsync("Campeonato Estadual: Ação!");
        Assert.NotNull(postagem);
        Assert.Equal(TipoPostagem.Noticia, postagem.Tipo);
        Assert.Equal("campeonato-estadual-acao", postagem.Slug);
        Assert.Equal(modalidadeId, postagem.ModalidadeId);
        Assert.Equal(adminId, postagem.AutorId);
        Assert.InRange(postagem.PublicadaEm, inicio.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
        Assert.Null(postagem.ImagemCaminho);
        Assert.Null(postagem.YoutubeId);

        var lista = await HtmlAsync(await cliente.GetAsync("/Admin/Postagens"));
        Assert.Contains("Campeonato Estadual: Ação!", lista);
        Assert.Contains("Jiu-Jitsu", lista);
    }

    [Fact]
    public async Task Titulo_repetido_gera_slug_com_sufixo()
    {
        var cliente = await app.CriarClienteAdminAsync();
        await CriarAsync(cliente, Noticia("Aula aberta"));
        await CriarAsync(cliente, Noticia("Aula aberta"));

        var slugs = await app.ExecutarAsync(db => db.Postagens.Where(p => p.Titulo == "Aula aberta").OrderBy(p => p.Id).Select(p => p.Slug).ToListAsync());
        Assert.Equal(["aula-aberta", "aula-aberta-2"], slugs);
    }

    [Fact]
    public async Task Noticia_exige_titulo_e_texto()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await CriarAsync(cliente, new() { ["Postagem.Tipo"] = "Noticia", ["Postagem.Titulo"] = "", ["Postagem.Conteudo"] = "" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var html = await HtmlAsync(resposta);
        Assert.Contains("Informe o título.", html);
        Assert.Contains("Escreva o texto da notícia.", html);
    }

    [Fact]
    public async Task Midia_com_link_do_youtube_guarda_so_o_id()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await CriarAsync(cliente, new()
        {
            ["Postagem.Tipo"] = "Midia",
            ["Postagem.Titulo"] = "Vídeo do treino",
            ["LinkYoutube"] = "https://youtu.be/dQw4w9WgXcQ?si=abc",
        });

        AssertRedirecionaPara(resposta, "/Admin/Postagens");
        var postagem = await BuscarAsync("Vídeo do treino");
        Assert.Equal(TipoPostagem.Midia, postagem!.Tipo);
        Assert.Equal("dQw4w9WgXcQ", postagem.YoutubeId);
        Assert.Null(postagem.ModalidadeId);

        // A edição mostra o link completo para o administrador.
        var edicao = await HtmlAsync(await cliente.GetAsync($"/Admin/Postagens/Editar/{postagem.Id}"));
        Assert.Contains("https://www.youtube.com/watch?v=dQw4w9WgXcQ", edicao);
    }

    [Fact]
    public async Task Link_invalido_do_youtube_e_recusado()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await CriarAsync(cliente, new()
        {
            ["Postagem.Tipo"] = "Midia",
            ["Postagem.Titulo"] = "Vídeo com link ruim",
            ["LinkYoutube"] = "https://vimeo.com/123456",
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Informe um link válido do YouTube.", await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Vídeo com link ruim"));
    }

    [Fact]
    public async Task Midia_sem_foto_nem_video_e_recusada()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await CriarAsync(cliente, new() { ["Postagem.Tipo"] = "Midia", ["Postagem.Titulo"] = "Mídia vazia" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Uma mídia precisa de uma foto ou de um link do YouTube.", await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Mídia vazia"));
    }

    [Fact]
    public async Task Midia_com_foto_e_salva_com_descricao()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var resposta = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            MidiaComFoto("Graduação de faixas"), "Foto", Formularios.PngFalso(), "foto.png", "image/png");

        AssertRedirecionaPara(resposta, "/Admin/Postagens");
        var postagem = await BuscarAsync("Graduação de faixas");
        Assert.NotNull(postagem!.ImagemCaminho);
        Assert.StartsWith("/uploads/", postagem.ImagemCaminho);
        Assert.Equal("Alunos no tatame", postagem.ImagemDescricao);
        Assert.True(File.Exists(CaminhoFisico(app, postagem.ImagemCaminho)));
    }

    [Fact]
    public async Task Foto_sem_autorizacao_de_imagem_e_recusada_e_nada_e_salvo()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var antes = ArquivosEnviados(app).Length;
        var campos = MidiaComFoto("Foto sem autorização");
        campos.Remove("AutorizacaoImagem");

        var resposta = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            campos, "Foto", Formularios.PngFalso(), "foto.png", "image/png");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains(MensagemAutorizacao, await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Foto sem autorização"));
        Assert.Equal(antes, ArquivosEnviados(app).Length);
    }

    [Fact]
    public async Task Foto_sem_descricao_e_recusada()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var antes = ArquivosEnviados(app).Length;
        var campos = MidiaComFoto("Foto sem descrição");
        campos["Postagem.ImagemDescricao"] = "";

        var resposta = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            campos, "Foto", Formularios.PngFalso(), "foto.png", "image/png");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Descreva a foto (texto alternativo para leitores de tela).", await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Foto sem descrição"));
        Assert.Equal(antes, ArquivosEnviados(app).Length);
    }

    [Fact]
    public async Task Arquivo_que_nao_e_imagem_e_recusado_e_nada_e_salvo()
    {
        var cliente = await app.CriarClienteAdminAsync();
        var antes = ArquivosEnviados(app).Length;

        var resposta = await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            MidiaComFoto("Arquivo falso"), "Foto", Encoding.UTF8.GetBytes("isto não é uma imagem"), "foto.jpg", "image/jpeg");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Envie uma imagem JPG, PNG ou WebP.", await HtmlAsync(resposta));
        Assert.Null(await BuscarAsync("Arquivo falso"));
        Assert.Equal(antes, ArquivosEnviados(app).Length);
    }

    [Fact]
    public async Task Editar_mantem_data_autor_e_slug_e_troca_a_foto()
    {
        var cliente = await app.CriarClienteAdminAsync();
        await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            MidiaComFoto("Treino de sábado"), "Foto", Formularios.PngFalso(), "a.png", "image/png");
        var original = (await BuscarAsync("Treino de sábado"))!;
        var fotoOriginal = CaminhoFisico(app, original.ImagemCaminho!);

        var campos = MidiaComFoto("Treino de domingo");
        campos["Postagem.Conteudo"] = "Legenda nova";
        var resposta = await Formularios.EnviarMultipartAsync(cliente, $"/Admin/Postagens/Editar/{original.Id}",
            campos, "Foto", Formularios.PngFalso(), "b.png", "image/png");

        AssertRedirecionaPara(resposta, "/Admin/Postagens");
        var editada = await app.ExecutarAsync(db => db.Postagens.SingleAsync(p => p.Id == original.Id));
        Assert.Equal("Treino de domingo", editada.Titulo);
        Assert.Equal("Legenda nova", editada.Conteudo);
        Assert.Equal(original.Slug, editada.Slug);
        Assert.Equal(original.PublicadaEm, editada.PublicadaEm);
        Assert.Equal(original.AutorId, editada.AutorId);
        Assert.NotEqual(original.ImagemCaminho, editada.ImagemCaminho);
        Assert.False(File.Exists(fotoOriginal));
        Assert.True(File.Exists(CaminhoFisico(app, editada.ImagemCaminho!)));

        // Remover a única foto de uma mídia sem vídeo é recusado; com vídeo, a foto é apagada.
        var semMidia = await Formularios.EnviarAsync(cliente, $"/Admin/Postagens/Editar/{original.Id}", new()
        {
            ["Postagem.Tipo"] = "Midia",
            ["Postagem.Titulo"] = "Treino de domingo",
            ["RemoverFoto"] = "true",
        });
        Assert.Contains("Uma mídia precisa de uma foto ou de um link do YouTube.", await HtmlAsync(semMidia));

        var comVideo = await Formularios.EnviarAsync(cliente, $"/Admin/Postagens/Editar/{original.Id}", new()
        {
            ["Postagem.Tipo"] = "Midia",
            ["Postagem.Titulo"] = "Treino de domingo",
            ["RemoverFoto"] = "true",
            ["LinkYoutube"] = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });
        AssertRedirecionaPara(comVideo, "/Admin/Postagens");
        var semFoto = await app.ExecutarAsync(db => db.Postagens.SingleAsync(p => p.Id == original.Id));
        Assert.Null(semFoto.ImagemCaminho);
        Assert.Null(semFoto.ImagemDescricao);
        Assert.Equal("dQw4w9WgXcQ", semFoto.YoutubeId);
        Assert.False(File.Exists(CaminhoFisico(app, editada.ImagemCaminho!)));
    }

    [Fact]
    public async Task Excluir_apaga_foto_e_comentarios()
    {
        var cliente = await app.CriarClienteAdminAsync();
        await Formularios.EnviarMultipartAsync(cliente, "/Admin/Postagens/Criar",
            MidiaComFoto("Postagem que será excluída"), "Foto", Formularios.PngFalso(), "x.png", "image/png");
        var postagem = (await BuscarAsync("Postagem que será excluída"))!;
        var foto = CaminhoFisico(app, postagem.ImagemCaminho!);
        var (_, usuario) = await app.CriarClienteUsuarioAsync("comentarista@teste.local");
        await app.ExecutarAsync(async db =>
        {
            db.Comentarios.Add(new Comentario { PostagemId = postagem.Id, UsuarioId = usuario.Id, Texto = "Parabéns!", CriadoEm = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });

        var confirmacao = await HtmlAsync(await cliente.GetAsync($"/Admin/Postagens/Excluir/{postagem.Id}"));
        Assert.Contains("Os comentários dessa postagem (1) também serão excluídos.", confirmacao);

        var resposta = await Formularios.EnviarAsync(cliente, $"/Admin/Postagens/Excluir/{postagem.Id}", new());

        AssertRedirecionaPara(resposta, "/Admin/Postagens");
        Assert.Null(await BuscarAsync("Postagem que será excluída"));
        Assert.False(await app.ExecutarAsync(db => db.Comentarios.AnyAsync(c => c.PostagemId == postagem.Id)));
        Assert.False(File.Exists(foto));
    }
}
