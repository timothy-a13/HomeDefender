using BlazorApp1.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Blazored.SessionStorage;
using Blazored.Toast;

using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);


var configuration = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .Build();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 新增連線壓縮
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
          new[] { "application/octet-stream" });
});

builder.Services.AddSession();
builder.Services.AddScoped<Connection>();
builder.Services.AddScoped<NatificationHubConn>();
builder.Services.AddScoped<SessionManager>();
builder.Services.AddScoped<SqlManager>();
builder.Services.AddScoped<EventManager>();
builder.Services.AddScoped<SemaphoreManager>();
builder.Services.AddScoped<IniHandler>();
builder.Services.AddSingleton<PythonManager>();
builder.Services.AddBlazoredSessionStorage();
builder.Services.AddDbContextPool<UserContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddServerSideBlazor().AddHubOptions(hub => hub.MaximumReceiveMessageSize = 100 * 1024 * 1024);

builder.Services.AddBlazoredToast();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.MapBlazorHub();

app.MapFallbackToPage("/_Host");

// 新增通知路由
//app.UseResponseCompression();
//app.MapHub<NotificationHub>("/notificationhub");

app.Run();
