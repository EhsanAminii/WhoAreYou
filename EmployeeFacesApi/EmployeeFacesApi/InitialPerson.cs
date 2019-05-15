using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EmployeeFacesApi
{
    public static class InitialPerson
    {
        [FunctionName("InitialPerson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "initialperson/{email}")] HttpRequestMessage req,
            string email,
            [Blob("employeephotos", Connection = "blobconnectionstring")] CloudBlobContainer container,
            ILogger log)
        {
            log.LogInformation($"email address {email} initialized");
            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            await container.CreateIfNotExistsAsync();

            var result = false;
            var blob = container.GetBlockBlobReference($"{email}.jpg");

            // if the user picture is in blob storage registered
            if (await blob.ExistsAsync())
            {
                result = true;
            }

            log.LogInformation($"The person is registered? {result}");

            return new OkObjectResult(result);
        }
    }
}