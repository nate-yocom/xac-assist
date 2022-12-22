using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add systemd compat
builder.Host.UseSystemd();

// Custom JSON serialization options for all controllers
builder.Services.AddControllers().AddJsonOptions(j => {
    j.JsonSerializerOptions.PropertyNameCaseInsensitive = true;    
    j.JsonSerializerOptions.WriteIndented = true;
    j.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<XacAssist.Renderer.FeatureStatusRenderer>();
builder.Services.AddSingleton<XacAssist.Pipeline.IPipeline, XacAssist.JitM.JitMPipeline>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<XacAssist.XacAssistWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
//app.UseAuthorization();

app.MapControllers();

app.Run();
