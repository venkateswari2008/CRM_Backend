namespace CRM.Infrastructure.Cache;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 6380;
    public string Password { get; set; } = "";
    public bool Ssl { get; set; } = true;

    public string BuildConnectionString() =>
        $"{Host}:{Port},password={Password},ssl={Ssl},abortConnect=false";
}
