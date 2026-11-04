namespace FisicoCombat.Web.Services;

public record ResultadoUpload(string? Caminho, string? Erro)
{
    public bool Sucesso => Erro is null;
}

/// <summary>
/// Salva fotos enviadas pelo administrador em disco com nome aleatório.
/// Aceita só JPEG, PNG e WebP, conferindo a assinatura do arquivo e não apenas a extensão.
/// </summary>
public class ArmazenamentoImagens(IWebHostEnvironment env, IConfiguration config)
{
    public const long TamanhoMaximo = 5 * 1024 * 1024;

    /// <summary>Pasta física dos uploads; servida em /uploads.</summary>
    public string Pasta { get; } = config["Armazenamento:PastaUploads"] is { Length: > 0 } p
        ? p
        : Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

    public async Task<ResultadoUpload> SalvarAsync(IFormFile arquivo, CancellationToken ct = default)
    {
        if (arquivo.Length == 0) return new(null, "O arquivo está vazio.");
        if (arquivo.Length > TamanhoMaximo) return new(null, "A imagem pode ter até 5 MB.");

        var cabecalho = new byte[12];
        await using (var leitura = arquivo.OpenReadStream())
        {
            var lidos = await leitura.ReadAtLeastAsync(cabecalho, cabecalho.Length, throwOnEndOfStream: false, ct);
            if (lidos < cabecalho.Length) return new(null, "Arquivo de imagem inválido.");
        }

        var extensao = DetectarExtensao(cabecalho);
        if (extensao is null) return new(null, "Envie uma imagem JPG, PNG ou WebP.");

        Directory.CreateDirectory(Pasta);
        var nome = $"{Guid.NewGuid():N}{extensao}";
        await using (var destino = File.Create(Path.Combine(Pasta, nome)))
        await using (var origem = arquivo.OpenReadStream())
            await origem.CopyToAsync(destino, ct);

        return new($"/uploads/{nome}", null);
    }

    /// <summary>Apaga um arquivo salvo por este serviço; ignora caminhos de fora de /uploads.</summary>
    public void Excluir(string? caminho)
    {
        if (string.IsNullOrEmpty(caminho) || !caminho.StartsWith("/uploads/")) return;
        var nome = Path.GetFileName(caminho);
        var completo = Path.Combine(Pasta, nome);
        if (File.Exists(completo)) File.Delete(completo);
    }

    internal static string? DetectarExtensao(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return ".jpg";
        if (b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";
        if (b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8)) return ".webp";
        return null;
    }
}
