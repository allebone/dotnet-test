using System.Net.Http.Headers;
using System.Text.Json;

static string? Env(string name) => Environment.GetEnvironmentVariable(name);

var explicitUrl = Env("WELL_KNOWN_URL");
var baseUrl     = Env("KEYCLOAK_BASE_URL");   // e.g., https://identity.omni.af.mil
var realm       = Env("KEYCLOAK_REALM");      // e.g., OMNI
var allowHttp   = (Env("ALLOW_INSECURE_HTTP") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
var ignoreSsl   = (Env("IGNORE_SSL_ERRORS")  ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);

string wellKnownUrl;

if (!string.IsNullOrWhiteSpace(explicitUrl))
{
    wellKnownUrl = explicitUrl!.Trim();
}
else if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(realm))
{
    wellKnownUrl = $"{baseUrl!.TrimEnd('/')}/realms/{realm!.Trim('/')}/.well-known/openid-configuration";
}
else
{
    Console.Error.WriteLine(
        "Missing configuration.\n" +
        "Provide either:\n" +
        "  - WELL_KNOWN_URL\n" +
        "or\n" +
        "  - KEYCLOAK_BASE_URL and KEYCLOAK_REALM\n");
    Environment.ExitCode = 2;
    return;
}

if (!Uri.TryCreate(wellKnownUrl, UriKind.Absolute, out var uri))
{
    Console.Error.WriteLine($"Invalid URL: {wellKnownUrl}");
    Environment.ExitCode = 2;
    return;
}

if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !allowHttp)
{
    Console.Error.WriteLine("Refusing to use http:// without ALLOW_INSECURE_HTTP=true");
    Environment.ExitCode = 2;
    return;
}

var handler = new HttpClientHandler();
if (ignoreSsl)
{
    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    Console.Error.WriteLine("⚠️  WARNING: SSL certificate validation is DISABLED (IGNORE_SSL_ERRORS=true). Do not use in production.");
}

using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
http.DefaultRequestHeaders.Accept.Clear();
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
http.DefaultRequestHeaders.UserAgent.ParseAdd("KeycloakWellKnownFetcher/1.0 (+dotnet8)");

try
{
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    using var resp = await http.GetAsync(uri, cts.Token);
    var content = await resp.Content.ReadAsStringAsync(cts.Token);

    if (!resp.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
        Console.Error.WriteLine(content);
        Environment.ExitCode = 1;
        return;
    }

    // Pretty-print if JSON
    try
    {
        using var doc = JsonDocument.Parse(content);
        var pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(pretty);
    }
    catch
    {
        Console.WriteLine(content);
    }
}
catch (TaskCanceledException ex)
{
    Console.Error.WriteLine($"Request timed out: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Request failed: {ex.Message}");
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex}");
    Environment.ExitCode = 1;
}