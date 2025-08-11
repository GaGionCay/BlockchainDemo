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

// Đặt encoding để hiển thị tiếng Việt chính xác
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Thêm các dịch vụ vào container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<BlockchainCore.BlockchainService>();
builder.Services.AddSingleton<Blockchain_Testing.Services.BlockchainService>();
// Cấu hình cơ sở dữ liệu và Identity
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();

// Thêm dịch vụ BlockchainService và P2PNode dưới dạng Singleton
builder.Services.AddSingleton<BlockchainCore.BlockchainService>();

// Sử dụng factory để khởi tạo P2PNode và tiêm BlockchainService
builder.Services.AddSingleton<P2PNode>(sp =>
{
    var blockchainService = sp.GetRequiredService<BlockchainCore.BlockchainService>();
    return new P2PNode(blockchainService, "Node A");
});

// Đăng ký dịch vụ nền để chạy P2PNode
// Điều này giúp P2P Node chạy độc lập với máy chủ web.
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
// Bạn có thể gọi endpoint này bằng HTTP POST để tạo block mới.
app.MapPost("/mine", (string data, BlockchainCore.BlockchainService blockchain, P2PNode p2pNode) =>
{
    var newBlock = blockchain.AddBlock(data);
    p2pNode.BroadcastBlock(newBlock, "Node A");
    return Results.Ok($"[Node A] Đã tạo 1 block mới với dữ liệu '{data}' và phát tán tới các node còn lại.");
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
        // Khởi động P2P Node của Node A.
        // Chú ý: Node A không cần kết nối tới seed node nào vì nó là node đầu tiên.
        _p2pNode.Start(8888, new List<string>());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Thực hiện dọn dẹp nếu cần thiết khi ứng dụng dừng.
        return Task.CompletedTask;
    }
}
