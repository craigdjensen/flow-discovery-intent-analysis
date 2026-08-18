using FlowDiscovery.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Flow Discovery API", Version = "v1" });
});

// Memory cache
builder.Services.AddMemoryCache();

// HTTP clients
builder.Services.AddHttpClient<ICognigyClient, CognigyClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["Cognigy:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-API-Key", config["Cognigy:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IPromptManagerClient, PromptManagerClient>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var timeout = config.GetValue<int>("PromptManager:TimeoutSeconds", 30);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

// App services
builder.Services.AddSingleton<IFlowCacheService, FlowCacheService>();
builder.Services.AddScoped<IFlowSearchService, FlowSearchService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:4200" };
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
