using JobOnlineAPI.Controllers;
using JobOnlineAPI.Repositories;
using JobOnlineAPI.Services;
using JobOnlineAPI.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobOnlineAPI.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // เพิ่ม JobOnlineAPI assembly เข้า application parts (จำเป็นสำหรับ WebApplicationFactory)
            services.AddControllersWithViews()
                .AddApplicationPart(typeof(LoginController).Assembly);
        });

        builder.ConfigureTestServices(services =>
        {
            // ลบ background services ทั้งหมด (EmailQueueProcessorService)
            services.RemoveAll<IHostedService>();

            // แทนที่ services ที่แตะ DB/Email ด้วย fakes
            services.Replace(ServiceDescriptor.Scoped<IUserService, FakeUserService>());
            services.Replace(ServiceDescriptor.Scoped<IAdminRepository, FakeAdminRepository>());
            services.Replace(ServiceDescriptor.Scoped<IEmailService, FakeEmailService>());
            services.Replace(ServiceDescriptor.Scoped<IEmailNotificationService, FakeEmailNotificationService>());

            // Job repository — singleton เพื่อให้ test access ได้ผ่าน factory.Services
            services.RemoveAll<IJobRepository>();
            services.AddSingleton<FakeJobRepository>();
            services.AddScoped<IJobRepository>(sp => sp.GetRequiredService<FakeJobRepository>());

            // ปิด log ทั้งหมดที่รบกวน — เอา providers ออกเลย (log ไม่มีที่ไป = เงียบ)
            services.AddLogging(logging => logging.ClearProviders());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        try
        {
            return base.CreateHost(builder);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"App startup failed: {ex.GetType().Name}: {ex.Message}", ex);
        }
    }
}
