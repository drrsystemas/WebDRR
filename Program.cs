using LibreriaBase.Areas.Carrito.Clases;
using LibreriaBase.Areas.Visor;
using LibreriaBase.Clases;
using LibreriaBase.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebDRR
{
    public class Program
    {

        public static async Task Main(String[] args)
        {
            //Por defecto, la aplicación cargará el fichero appsettings.json y su variante appsettings.Development.json si estamos depurando
            var builder = WebApplication.CreateBuilder(args);

            //30-05-2022
            builder.Services.AddMemoryCache();

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddMvc();
            builder.Services.AddRazorPages();

            //26/10/2022
            //builder.Services.AddHsts(options =>
            //{
            //    options.Preload = true;
            //    options.IncludeSubDomains = true;
            //    options.MaxAge = TimeSpan.FromDays(30);
            //});

            //21/05/2022
            builder.Services.AddControllers();
            builder.Services.AddControllers().AddNewtonsoftJson();

            #region Inyeccion de dependencias para usar IoC.

            #region Repositorios de Datos.
            builder.Services.AddTransient<IRepositorioTransporte, RepositorioTransporte>();
            builder.Services.AddTransient<IRepositorioCliente, RepositorioCliente>();
            builder.Services.AddTransient<IRepositorioEmpresa, RepositorioEmpresa>();
            builder.Services.AddTransient<IRepositorioProducto, RepositorioProducto>();
            builder.Services.AddTransient<IRepositorioSoporte, RepositorioSoporte>();
            builder.Services.AddTransient<IRepositorioPais, RepositorioPais>();
            builder.Services.AddTransient<IRepositorioUsuarioWeb, RepositorioUsuarioWeb>();
            builder.Services.AddTransient<IRepositorioCobro, RepositorioCobro>();
            builder.Services.AddTransient<IRepositorioPedido, RepositorioPedido>();
            builder.Services.AddTransient<IServicioVisor, ServicioVisor>();
            #endregion

            #region Sercicios personalizados

            //services.AddScoped<Carrito>()
            builder.Services.AddScoped<Carrito>(serviceProvider => SessionCarrito.ObtenerCarrito(serviceProvider));


            //25/04/2022
            //builder.Services.AddScoped<DiaTrabajoVendedor>(serviceProvider => SessionDiaTrabajoVendedor.ObtenerDiaTrabajoVendedor(serviceProvider));


            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            #endregion





            builder.Services.AddTransient<IEnviarCorreo, EnviarCorreo>();
            builder.Services.Configure<List<OpcionesCorreoElectronico>>(builder.Configuration.GetSection("OpcionesCorreoElectronico"));

            #endregion


            builder.Services.AddDistributedMemoryCache();//19/06/2020

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(4);
            });

            builder.Services.AddSession(options =>
            {
                options.Cookie.Name = "AlmaWeb";
                options.IdleTimeout = TimeSpan.FromHours(48); // Tiempo de expiracion - 30min.
            });


            //---------------------------------------------------------------------------------------//

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Notificaciones");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }


            var idiomas = new System.Globalization.CultureInfo[] {
                new System.Globalization.CultureInfo ("es-AR"),
                new System.Globalization.CultureInfo ("es"),
                new System.Globalization.CultureInfo ("en-US"),
                new System.Globalization.CultureInfo ("en")
            };

            //revisarr
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("es-AR"),
                SupportedCultures = idiomas,
                SupportedUICultures = idiomas,
            });



            app.UseHttpsRedirection();

            //nos permite usar archivos estaticos los de wwwrot
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //uso las sesiones.
            app.UseSession();


            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapAreaControllerRoute(
                            name: "pagination",
                            areaName: "Carrito",
                            pattern: "Producto/Page{productPage}",
                            defaults: new { area = "Carrito", Controller = "Producto", action = "Productos" });

            app.Run();

        }
    }

}






