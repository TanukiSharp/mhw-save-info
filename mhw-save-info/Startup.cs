using Blazor.FileReader;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace mhw_save_info
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddFileReaderService(options => options.UseWasmSharedBuffer = true);
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
