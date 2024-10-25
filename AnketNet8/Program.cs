using AnketNet8.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SurveyContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Kimlik doðrulama hizmetini ekleyin
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Surveys/AdminLogin"; // Giriþ sayfasýnýn yolu
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Kimlik doðrulama ve yetkilendirme middleware'lerini ekleyin
app.UseAuthentication(); // Kimlik doðrulama middleware'i
app.UseAuthorization(); // Yetkilendirme middleware'i

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Surveys}/{action=Index}/{id?}");

app.Run();
