using Nexa.Adapter.Configuration;
using Nexa.Adapter.Extensions;
using Nexa.Adapter.Infrastructure.LLM;
using Nexa.Adapter.Middleware;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddTransient<IPromptBuilder, DefaultPromptBuilder>();
builder.Services.AddTransient<IChatService, ChatService>();
builder.Services.AddSingleton<IFalsePositiveFramework, FalsePositiveFramework>();
builder.Services.AddSingleton<IEvidenceWeightingEngine, EvidenceWeightingEngine>();
builder.Services.AddSingleton<IConfidenceScoreEngine, ConfidenceScoreEngine>();
builder.Services.AddSingleton<IAnalyticalEngine, AnalyticalEngine>();

builder.Services.AddTransient<IBankDataAggregator,  BankDataAggregator>();
builder.Services.AddTransient<IInvestigationOrchestrator, NexaInvestigationOrchestrator>();

builder.AddBankDataApiService();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection("LLM"));
builder.Services.Configure<BankDataApiOptions>(builder.Configuration.GetSection(BankDataApiOptions.configSectionName));

LLMProviderFactory.Register(builder.Services, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");
app.UseMiddleware<AuditMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


