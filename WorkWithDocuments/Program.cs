using Microsoft.EntityFrameworkCore;
using WorkWithDocuments.Data;
using WorkWithDocuments.Services;
using WorkWithDocuments.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Registration of DbContext V.S
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
        ));

// 2. Registration of FileProcessingService
// Unity: container.RegisterType<IFileProcessingService, FileProcessingService>();
// .NET Core Native: V.S
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ... (standart pipeline) V.S

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();