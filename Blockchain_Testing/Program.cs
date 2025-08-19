using BlockchainCore;
using Blockchain_Testing.Data;
using Blockchain_Testing.Models;
using Blockchain_Testing.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using Blockchain_Testing;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddRazorPages();
 
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();
 
builder.Services.AddSingleton<BlockchainCore.BlockchainCore>();
builder.Services.AddScoped<Blockchain_Testing.Services.BlockchainService>(); 
builder.Services.AddSingleton<P2PNode>(sp =>
{
    var blockchainService = sp.GetRequiredService<BlockchainCore.BlockchainCore>(); 
    var seedNodes = new List<string> { /* "127.0.0.1:8889" */ };
    return new P2PNode(blockchainService, "Node A", seedNodes);
});
 
builder.Services.AddHostedService<P2PBackgroundService>();
 
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/Account/Login"));
app.MapRazorPages();
 
app.MapPost("/mine", (BlockchainCore.BlockchainCore blockchain, P2PNode p2pNode) =>
{
    try
    {
        var newBlock = blockchain.MinePendingTransactions(); 
        p2pNode.BroadcastBlock(newBlock);
        return Results.Ok($"[Node A] Đã đào thành công một block mới chứa các giao dịch đang chờ và phát tán tới các node còn lại.");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();