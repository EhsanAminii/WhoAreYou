using System;
using EmployeeFacesApi;
using EmployeeFacesApi.Services;
using EmployeeFacesApi.Statics;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

[assembly: FunctionsStartup(typeof(Startup))]

namespace EmployeeFacesApi
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<FaceClient>(
                new FaceClient(
                        new ApiKeyServiceClientCredentials(ConfigurationSettings.SubscriptionKey),
                        new System.Net.Http.DelegatingHandler[] { })
                {
                    Endpoint = ConfigurationSettings.FaceEndpoint
                });

            builder.Services.AddSingleton<CloudBlobClient>(
                new CloudBlobClient(
                    new Uri(ConfigurationSettings.BlobStorageBaseUrl),
                    new StorageCredentials(
                        ConfigurationSettings.BlobStorageAccountName,
                        ConfigurationSettings.BlobStorageAccountKey))
            );

            builder.Services.AddScoped<IFaceService, FaceService>();
        }
    }
}