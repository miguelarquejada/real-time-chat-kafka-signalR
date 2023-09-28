using backend;
using backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicyForDashboard", builder => 
        builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddHostedService<KafkaConsumerService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("CorsPolicyForDashboard");

app.UseAuthorization();

app.UseRouting();

app.MapHub<ChatHub>("/chat");

app.Run();