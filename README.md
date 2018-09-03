# Let's Encrypt SSL meets ASP.Net-Core

This example project shows a way to fully automate the Let's Encrypt SSL certificate creation and renewal by
integrating the ACME v2 protocol directly into the ASP.Net-Core web app.

**Note on reverse proxies:** When hosting your web app for the public you should consider using a reverse proxy
like Nginx. [Learn more](https://github.com/JrCs/docker-letsencrypt-nginx-proxy-companion) about how to configure
Let's Encrypt with Nginx.

## Minimal Example

To complete the ACME http challenge a separate minimal http server is spun up. After the challenge has finished
and the certificate is downloaded the actual web app is launched. If a valid certificate already exists this
mechanism will be shortcutted and the production web app starts right away with proper SSL configured.

In this minimal code example the certificate is stored in a file and configurationn is done in code.

```csharp
public static class Program {
    public static void Main(string[] args) =>
        new LetsEncryptBuilder()
            .UseCertificateSaver(new FileCertificateSaver("cert.pfx", X509ContentType.Pfx, "password"))
            .UseCertificateLoader(new FileCertificateLoader("cert.pfx", "password"))
            .WithOptions(options => {
                options.Email = "email@example.com";
                options.Hostname = "hostname.com";
                options.FriendlyName = "hostname-cert";
            })
            .OnError(options => {
                Console.WriteLine(options.Exception.InnerException.Message);
                options.Continue = false; // Exit application now
            })
            .ContinueWith(certificate => CreateWebHostBuilder(args, certificate).Build())
            .Build()
            .Run(); // OR .RunAsync()

    private static IWebHostBuilder CreateWebHostBuilder(string[] args, X509Certificate2 certificate) =>
        WebHost.CreateDefaultBuilder(args)
            .UseKestrel(options => {
                options.ListenAnyIP(80);
                options.ListenAnyIP(443, o => o.UseHttps(certificate));
            })
            .Configure(app => {
                // Return "Hello World!" on every request.
                app.Run(context => context.Response.WriteAsync("Hello World!"));
            });
```
