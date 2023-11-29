using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddAzureClients(clientBuilder =>
{
    // Register clients for each service
    // clientBuilder.AddSecretClient(new Uri("<key_vault_url>"));
    clientBuilder.AddBlobServiceClient(new Uri("https://aisekquickstart.blob.core.windows.net"));
    clientBuilder.UseCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions{
            ExcludeInteractiveBrowserCredential = false
        }));
});

var app = builder.Build();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
