using BTSB.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// DATABASE MYSQL
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString!));


// ==========================================
// SESSION
// ==========================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Session berlaku maksimal 60 menit
    // sejak aktivitas terakhir pengguna.
    options.IdleTimeout = TimeSpan.FromMinutes(60);

    // Cookie hanya dapat diakses oleh server.
    options.Cookie.HttpOnly = true;

    // Cookie session tetap dianggap penting
    // oleh aplikasi.
    options.Cookie.IsEssential = true;

    // Nama cookie session aplikasi BTSB.
    options.Cookie.Name = "BTSB.Session";
});


// ==========================================
// RAZOR PAGES
// ==========================================

builder.Services.AddRazorPages();


// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();


// ==========================================
// HTTP REQUEST PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    app.UseHsts();
}


// ==========================================
// HTTPS
// ==========================================

app.UseHttpsRedirection();


// ==========================================
// STATIC FILES
// ==========================================

app.UseStaticFiles();


// ==========================================
// ROUTING
// ==========================================

app.UseRouting();


// ==========================================
// SESSION
// ==========================================

app.UseSession();


// ==========================================
// RAZOR PAGES
// ==========================================

app.MapRazorPages();


// ==========================================
// RUN APPLICATION
// ==========================================

app.Run();