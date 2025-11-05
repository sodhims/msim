using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ManufacturingSimulation.Database;  // <-- Add this
using ManufacturingSimulation.WPF.Services;  // <-- Add this
using ManufacturingSimulation.WPF.ViewModels.Admin;  // <-- Add this

namespace ManufacturingSimulation.WPF
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var services = new ServiceCollection();
            
            // Register DbContext (CRITICAL - was missing!)
            services.AddDbContext<MesDbContext>();
            
            // Register your services
            services.AddScoped<IAdminService, AdminService>();
            services.AddTransient<DatabaseAdminViewModel>();
            
            // Register windows if using DI for them
            services.AddTransient<MainWindow>();
            
            // Build provider
            ServiceProvider = services.BuildServiceProvider();
            
            // Start main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}