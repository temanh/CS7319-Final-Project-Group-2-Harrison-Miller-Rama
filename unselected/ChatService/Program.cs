using ChatService.Data;
using ChatService.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MessageDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR();
builder.Services.AddControllers();

builder.Services.AddAuthentication().AddJwtBearer();

var app = builder.Build();

app.MapHub<ChatHub>("/ws/chat");
app.MapControllers();

app.Run();