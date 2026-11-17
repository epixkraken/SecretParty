using Microsoft.EntityFrameworkCore;
using SecretParty.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using SecretParty.Servicios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Nuestro servicio para guardar/borrar fotos. "Scoped" = uno nuevo por cada petición.
// Los controladores lo reciben en su constructor (inyección de dependencias), igual que AppDbContext.
builder.Services.AddScoped<AlmacenFotos>();

// Trazas (paper de patrones de diseño): anota qué pasó en cada petición
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Rastreador>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";        // a dónde te manda si no estás logueada
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado"; // logueada pero sin permiso (no es admin)
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // La cuenta con el email de "Admin:Email" (appsettings.json) se vuelve admin al arrancar
    var adminEmail = builder.Configuration["Admin:Email"];
    if (!string.IsNullOrWhiteSpace(adminEmail))
    {
        var admin = db.Usuarios.FirstOrDefault(u => u.Email == adminEmail);
        if (admin != null && !admin.EsAdmin)
        {
            admin.EsAdmin = true;
            db.SaveChanges();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// MapStaticAssets (abajo) solo conoce los archivos que existían al COMPILAR la app.
// Las fotos que suben los usuarios aparecen después, así que UseStaticFiles las sirve
// leyendo wwwroot en el momento (ej: /uploads/perfiles/abc.jpg).
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
