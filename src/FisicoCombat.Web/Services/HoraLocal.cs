namespace FisicoCombat.Web.Services;

/// <summary>Converte datas gravadas em UTC para o horário de Brasília, usado em todo o blog.</summary>
public static class HoraLocal
{
    // Id IANA no Linux e no Windows com ICU; id do Windows sem ICU; por fim UTC-3 fixo
    // (o Brasil não tem horário de verão desde 2019).
    private static readonly TimeZoneInfo Brasilia =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var iana) ? iana
        : TimeZoneInfo.TryFindSystemTimeZoneById("E. South America Standard Time", out var windows) ? windows
        : TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "Brasília (UTC-3)", "Brasília (UTC-3)");

    public static DateTime DeUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Brasilia);
}
