using FisicoCombat.Web.Services;

namespace FisicoCombat.Tests.Fundacao;

public class SlugTests
{
    [Theory]
    [InlineData("Jiu-Jitsu Brasileiro", "jiu-jitsu-brasileiro")]
    [InlineData("Notícia: Ação no CT!", "noticia-acao-no-ct")]
    [InlineData("  Muay   Thai  ", "muay-thai")]
    [InlineData("Boxe 2026", "boxe-2026")]
    [InlineData("!!!", "item")]
    [InlineData("", "item")]
    public void Gera_slug_sem_acentos_e_simbolos(string texto, string esperado) =>
        Assert.Equal(esperado, Slug.Gerar(texto));

    [Fact]
    public void Respeita_tamanho_maximo_sem_terminar_em_hifen()
    {
        var slug = Slug.Gerar("abc def ghi", tamanhoMaximo: 4);
        Assert.Equal("abc", slug);
    }
}

public class YouTubeTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=42s", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/live/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData(" https://youtu.be/dQw4w9WgXcQ ", "dQw4w9WgXcQ")]
    public void Extrai_id_de_links_validos(string url, string id) =>
        Assert.Equal(id, YouTube.ExtrairId(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://www.youtube.com/watch?v=curto")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><script>")]
    [InlineData("https://www.youtube.com/channel/UCxyz")]
    public void Recusa_links_invalidos_ou_de_outros_sites(string? url) =>
        Assert.Null(YouTube.ExtrairId(url));
}

public class ArmazenamentoImagensTests
{
    [Fact]
    public void Detecta_jpeg_png_e_webp_pela_assinatura()
    {
        Assert.Equal(".jpg", ArmazenamentoImagens.DetectarExtensao([0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0]));
        Assert.Equal(".png", ArmazenamentoImagens.DetectarExtensao(Infra.Formularios.PngFalso()));
        Assert.Equal(".webp", ArmazenamentoImagens.DetectarExtensao("RIFF\0\0\0\0WEBP"u8));
    }

    [Fact]
    public void Recusa_arquivos_que_nao_sao_imagem()
    {
        Assert.Null(ArmazenamentoImagens.DetectarExtensao("<svg xmlns='x'>"u8));
        Assert.Null(ArmazenamentoImagens.DetectarExtensao("GIF89a\0\0\0\0\0\0"u8));
        Assert.Null(ArmazenamentoImagens.DetectarExtensao("MZ\x90\0\0\0\0\0\0\0\0\0"u8));
        Assert.Null(ArmazenamentoImagens.DetectarExtensao([]));
    }
}
