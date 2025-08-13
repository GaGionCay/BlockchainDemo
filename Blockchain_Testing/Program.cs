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

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container.
builder.Services.AddRazorPages();

// Cấu hình cơ sở dữ liệu và Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();

// CHỈ SỬ DỤNG MỘT SERVICE BLOCKCHAIN DUY NHẤT.
// Dịch vụ này sẽ được chia sẻ cho cả P2PNode và các trang Razor.
builder.Services.AddSingleton<BlockchainCore.BlockchainService>();
builder.Services.AddScoped<Blockchain_Testing.Services.BlockchainService>();
// Sử dụng factory để khởi tạo P2PNode và tiêm BlockchainService
// Sửa đổi để sử dụng constructor có seedNodes
builder.Services.AddSingleton<P2PNode>(sp =>
{
    var blockchainService = sp.GetRequiredService<BlockchainCore.BlockchainService>();
    // Bạn có thể thêm các seed node vào danh sách này nếu cần
    var seedNodes = new List<string> { /* "127.0.0.1:8889" */ };
    return new P2PNode(blockchainService, "Node A", seedNodes);
});

// Đăng ký dịch vụ nền để chạy P2PNode
builder.Services.AddHostedService<P2PBackgroundService>();

// Cấu hình xác thực
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

// API endpoint mới để đào và phát tán block
app.MapPost("/mine", (BlockchainCore.BlockchainService blockchain, P2PNode p2pNode) =>
{
    try
    {
        var newBlock = blockchain.MinePendingTransactions();
        // Cần đảm bảo rằng p2pNode.BroadcastBlock có thể nhận được block đã đào.
        // Cần thay đổi P2PNode.BroadcastBlock để có thể nhận block và gửi đi
        p2pNode.BroadcastBlock(newBlock);
        return Results.Ok($"[Node A] Đã đào thành công một block mới chứa các giao dịch đang chờ và phát tán tới các node còn lại.");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();

// Dịch vụ nền để khởi động P2PNode
public class P2PBackgroundService : IHostedService
{
    private readonly P2PNode _p2pNode;

    public P2PBackgroundService(P2PNode p2pNode)
    {
        _p2pNode = p2pNode;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _p2pNode.Start(8888);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
