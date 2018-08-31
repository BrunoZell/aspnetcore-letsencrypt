using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace WebApp {
    public class Program {
        public static void Main(string[] args) {
            CreateAcmeHostBuilder()
                .Build()
                .Run();

            CreateWebHostBuilder(args)
                .Build()
                .Run();
        }

        public static IWebHostBuilder CreateAcmeHostBuilder()
           => new WebHostBuilder()
                .UseKestrel(options => options.ListenAnyIP(80))
                .UseStartup<StartupAcme>();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
