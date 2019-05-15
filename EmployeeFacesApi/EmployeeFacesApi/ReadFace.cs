using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmployeeFacesApi
{
    public static class ReadFace
    {
        [FunctionName("ReadFace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var content = await req.Content.ReadAsStreamAsync();

            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = "https://westeurope.api.cognitive.microsoft.com";
            FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey), new DelegatingHandler[] { });
            faceClient.Endpoint = faceEndpoint;

            var result = await faceClient.Face.DetectWithStreamAsync(content);

            return new OkObjectResult(result);
        }
    }
}