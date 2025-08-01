using GenerativeAI;
using GenerativeAI.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Add Generative AI
builder.Services.AddGenerativeAI(new GenerativeAIOptions()
{
    Model = GoogleAIModels.Gemini2Flash,
    Credentials = new GoogleAICredentials()
    {
        ApiKey = EnvironmentVariables.GOOGLE_API_KEY ?? throw new InvalidOperationException("GOOGLE_API_KEY environment variable is not set")
    }
}).WithAdc();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
