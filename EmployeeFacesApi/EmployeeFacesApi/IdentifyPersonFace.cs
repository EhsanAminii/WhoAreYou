using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmployeeFacesApi
{
    public static class IdentifyPersonFace
    {
        [FunctionName("IdentifyPersonFace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestData = await req.Content.ReadAsStringAsync();
            var identifyRequestMessage = JsonConvert.DeserializeObject<IdentifyRequestModel>(requestData);

            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");

            FaceClient faceClient = new FaceClient(
                new ApiKeyServiceClientCredentials(subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });
            faceClient.Endpoint = faceEndpoint;

            var result = await faceClient.Face.IdentifyAsync(
                identifyRequestMessage.faceIds,
                identifyRequestMessage.personGroupId);

            return (ActionResult)new OkObjectResult(result);
        }
    }
}