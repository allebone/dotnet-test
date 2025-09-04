using System.Net.Http.Headers;
using System.Text.Json;

internal class NewBaseType
{
    private static async global::System.Threading.Tasks.Task<global::System.Int32> Main(string[] args)
    {
        static string GetEnv(string name) => Environment.GetEnvironmentVariable(name);

        var explicitUrl = GetEnv("WELL_KNOWN_URL");
        var baseUrl = GetEnv("KEYCLOAK_BASE_URL");   // e.g. https://auth.example.com
        var realm = GetEnv("KEYCLOAK_REALM");      // e.g. myrealm
        var allowHttp = (GetEnv("ALLOW_INSECURE_HTTP") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);

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
            return 2;
        }

        if (Uri.TryCreate(wellKnownUrl, UriKind.Absolute, out var uri) is false)
        {
            Console.Error.WriteLine($"Invalid URL: {wellKnownUrl}");
            return 2;
        }

        if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !allowHttp)
        {
            Console.Error.WriteLine("Refusing to use http:// without ALLOW_INSECURE_HTTP=true");
            return 2;
        }

        using var handler = new HttpClientHandler
        {
            // If you need to allow self-signed certs temporarily, uncomment with care:
            // ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        using var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        // Minimal headers
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.UserAgent.ParseAdd("KeycloakWellKnownFetcher/1.0 (+dotnet8)");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await http.GetAsync(uri, cts.Token);
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.Error.WriteLine(content);
                return 1;
            }

            // Pretty-print JSON if possible
            try
            {
                using var doc = JsonDocument.Parse(content);
                var pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(pretty);
            }
            catch
            {
                // Not JSON? Just print raw.
                Console.WriteLine(content);
            }

            return 0;
        }
        catch (TaskCanceledException ex)
        {
            Console.Error.WriteLine($"Request timed out: {ex.Message}");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Request failed: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex}");
            return 1;
        }
    }
}

internal class Program : NewBaseType
{
}