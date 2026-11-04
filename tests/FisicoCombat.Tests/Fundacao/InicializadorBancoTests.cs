using FisicoCombat.Web.Data;

namespace FisicoCombat.Tests.Fundacao;

public class InicializadorBancoTests
{
    [Fact]
    public void Cria_a_pasta_do_banco_quando_ela_nao_existe()
    {
        var raiz = Path.Combine(Path.GetTempPath(), "fc-pasta-" + Guid.NewGuid().ToString("N"));
        var pasta = Path.Combine(raiz, "App_Data");
        try
        {
            InicializadorBanco.GarantirPastaDoBanco($"Data Source={Path.Combine(pasta, "fisicocombat.db")}");
            Assert.True(Directory.Exists(pasta));
        }
        finally
        {
            if (Directory.Exists(raiz)) Directory.Delete(raiz, recursive: true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=fisicocombat.db")]
    public void Ignora_banco_em_memoria_ou_na_pasta_atual(string? conexao) =>
        InicializadorBanco.GarantirPastaDoBanco(conexao);
}
