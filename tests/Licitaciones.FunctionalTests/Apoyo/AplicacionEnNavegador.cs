using System.Diagnostics;
using System.Net.Sockets;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Testcontainers.PostgreSql;

namespace Licitaciones.FunctionalTests.Apoyo;

/// <summary>
/// Levanta la aplicación web real sobre PostgreSQL real y abre un navegador contra ella.
/// </summary>
/// <remarks>
/// La aplicación se arranca como un proceso aparte y no con el servidor en memoria de
/// <c>WebApplicationFactory</c>: ese servidor no escucha en ningún puerto, así que un
/// navegador no puede visitarlo. Lo que se prueba aquí es la aplicación tal como se
/// publica, servida por Kestrel y recorrida por un navegador de verdad.
/// </remarks>
public sealed class AplicacionEnNavegador : IAsyncLifetime
{
    private static readonly TimeSpan EsperaMaximaDeArranque = TimeSpan.FromSeconds(90);

    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("licitaciones_e2e")
        .WithUsername("licitaciones_e2e")
        .WithPassword("licitaciones_e2e")
        .Build();

    private readonly string _directorioPublicado = Path.Combine(
        Path.GetTempPath(),
        $"licitaciones-e2e-{Guid.NewGuid():N}");

    private Process? _aplicacion;
    private IPlaywright? _playwright;

    /// <summary>Navegador compartido por todas las pruebas del recorrido.</summary>
    public IBrowser Navegador { get; private set; } = null!;

    /// <summary>Dirección en la que quedó escuchando la aplicación.</summary>
    public string DireccionBase { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        await using (var contexto = CrearContexto())
        {
            await contexto.Database.MigrateAsync();
        }

        await PublicarAplicacionAsync();

        DireccionBase = $"http://127.0.0.1:{PuertoLibre()}";
        _aplicacion = ArrancarAplicacion(DireccionBase, _contenedor.GetConnectionString());
        await EsperarAQueRespondaAsync();

        _playwright = await Playwright.CreateAsync();
        Navegador = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (Navegador is not null)
        {
            await Navegador.CloseAsync();
        }

        _playwright?.Dispose();

        if (_aplicacion is { HasExited: false })
        {
            _aplicacion.Kill(entireProcessTree: true);
            await _aplicacion.WaitForExitAsync();
        }

        _aplicacion?.Dispose();

        await _contenedor.DisposeAsync();

        try
        {
            Directory.Delete(_directorioPublicado, recursive: true);
        }
        catch (IOException)
        {
            // La carpeta temporal se limpiará sola. No vale la pena fallar por esto.
        }
    }

    /// <summary>Abre una pestaña nueva y aislada apuntando a la aplicación.</summary>
    /// <remarks>
    /// Cada prueba usa su propio contexto para que el tema y la moneda que una elija no
    /// lleguen a la siguiente: la preferencia vive en el almacenamiento del navegador.
    /// </remarks>
    public async Task<IPage> AbrirPaginaAsync()
    {
        var contexto = await Navegador.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = DireccionBase,
            // Una pantalla de escritorio: el menú se despliega sin tener que abrirlo.
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        });

        return await contexto.NewPageAsync();
    }

    /// <summary>Crea un contexto contra la misma base que usa la aplicación.</summary>
    public LicitacionesDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(_contenedor.GetConnectionString())
            .Options;

        return new LicitacionesDbContext(opciones, new SystemClock());
    }

    // Se pide al sistema operativo un puerto libre y se suelta enseguida, en vez de fijar
    // uno: en la máquina de quien ejecute las pruebas podría estar ocupado.
    private static int PuertoLibre()
    {
        using var escucha = new TcpListener(System.Net.IPAddress.Loopback, 0);
        escucha.Start();
        var puerto = ((System.Net.IPEndPoint)escucha.LocalEndpoint).Port;
        escucha.Stop();

        return puerto;
    }

    /// <summary>
    /// Publica la aplicación en una carpeta temporal y la ejecuta desde ahí.
    /// </summary>
    /// <remarks>
    /// No se ejecuta la carpeta de compilación: <c>wwwroot</c> y el paquete de estilos de
    /// las vistas solo se colocan en su sitio al publicar, así que desde <c>bin</c> los
    /// archivos estáticos se sirven vacíos y la página aparece sin estilos ni guiones. Lo
    /// que se prueba aquí es la aplicación tal como se despliega.
    /// </remarks>
    private async Task PublicarAplicacionAsync()
    {
        var (_, directorioProyecto) = RutaDeLaAplicacion();

        var inicio = new ProcessStartInfo
        {
            FileName = RutaDeDotnet(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        inicio.ArgumentList.Add("publish");
        inicio.ArgumentList.Add(Path.Combine(directorioProyecto, "Licitaciones.Web.csproj"));
        inicio.ArgumentList.Add("-c");
        inicio.ArgumentList.Add("Release");
        inicio.ArgumentList.Add("-o");
        inicio.ArgumentList.Add(_directorioPublicado);
        inicio.ArgumentList.Add("--nologo");

        using var publicacion = Process.Start(inicio)
            ?? throw new InvalidOperationException("No se pudo publicar la aplicación web.");

        var salida = await publicacion.StandardOutput.ReadToEndAsync();
        await publicacion.WaitForExitAsync();

        if (publicacion.ExitCode != 0)
        {
            throw new InvalidOperationException($"La publicación de la aplicación web falló. {salida}");
        }
    }

    private Process ArrancarAplicacion(string direccion, string cadenaConexion)
    {
        var ejecutable = Path.Combine(_directorioPublicado, "Licitaciones.Web.dll");

        var inicio = new ProcessStartInfo
        {
            FileName = RutaDeDotnet(),
            WorkingDirectory = _directorioPublicado,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        inicio.ArgumentList.Add(ejecutable);
        inicio.Environment["ASPNETCORE_URLS"] = direccion;
        inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        inicio.Environment["ConnectionStrings__Default"] = cadenaConexion;
        // Sin dirección de la API, el menú simplemente no dibuja ese enlace.
        inicio.Environment["Api__UrlDocumentacion"] = string.Empty;

        return Process.Start(inicio)
            ?? throw new InvalidOperationException("No se pudo arrancar la aplicación web.");
    }

    // El anfitrión de pruebas puede estar corriendo con un dotnet que no esté en el PATH,
    // o con un PATH que apunte a otra versión del tiempo de ejecución.
    private static string RutaDeDotnet()
    {
        var raiz = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (string.IsNullOrWhiteSpace(raiz))
        {
            return "dotnet";
        }

        var ejecutable = Path.Combine(raiz, OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

        return File.Exists(ejecutable) ? ejecutable : "dotnet";
    }

    /// <summary>
    /// Localiza el ensamblado publicado de la aplicación web subiendo hasta la raíz de la
    /// solución, para no depender de una ruta relativa que cambie con la configuración.
    /// </summary>
    private static (string Ejecutable, string DirectorioProyecto) RutaDeLaAplicacion()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Licitaciones.sln")))
        {
            directorio = directorio.Parent;
        }

        if (directorio is null)
        {
            throw new InvalidOperationException("No se encontró la raíz de la solución.");
        }

        // Se usa la misma configuración con la que se compilaron las pruebas.
        var configuracion = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";

        var proyecto = Path.Combine(directorio.FullName, "src", "Licitaciones.Web");
        var ruta = Path.Combine(proyecto, "bin", configuracion, "net9.0", "Licitaciones.Web.dll");

        return File.Exists(ruta)
            ? (ruta, proyecto)
            : throw new FileNotFoundException(
                $"No se encontró la aplicación web compilada en {ruta}. Ejecute dotnet build antes.",
                ruta);
    }

    private async Task EsperarAQueRespondaAsync()
    {
        using var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var limite = DateTimeOffset.UtcNow + EsperaMaximaDeArranque;

        while (DateTimeOffset.UtcNow < limite)
        {
            if (_aplicacion is { HasExited: true })
            {
                throw new InvalidOperationException(
                    $"La aplicación web terminó al arrancar con código {_aplicacion.ExitCode}. "
                    + await _aplicacion.StandardError.ReadToEndAsync());
            }

            try
            {
                var respuesta = await cliente.GetAsync(new Uri($"{DireccionBase}/health/ready"));
                if (respuesta.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Todavía no escucha.
            }
            catch (TaskCanceledException)
            {
                // El primer arranque abre la conexión a la base y puede tardar más que el
                // tiempo de espera de una sola petición.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException("La aplicación web no respondió dentro del tiempo previsto.");
    }
}

/// <summary>Agrupa las pruebas que comparten el navegador y la aplicación.</summary>
[CollectionDefinition(Nombre)]
public sealed class NavegadorCollection : ICollectionFixture<AplicacionEnNavegador>
{
    public const string Nombre = "Navegador";
}
