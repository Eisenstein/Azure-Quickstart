using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddAzureClients(clientBuilder =>
{
    var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions{
            ExcludeInteractiveBrowserCredential = false
        });
        
    clientBuilder.AddBlobServiceClient(new Uri(builder.Configuration["BlobStorageEndpoint"]!));
    clientBuilder.AddClient<CosmosClient, CosmosClientOptions>(options => new CosmosClient(builder.Configuration["CosmosDbEndpoint"], credential, options));
    clientBuilder.AddServiceBusClientWithNamespace(builder.Configuration["ServiceBusNamespace"]!);

    clientBuilder.UseCredential(credential);
});

var app = builder.Build();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
