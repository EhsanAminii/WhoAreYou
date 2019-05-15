using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EmployeeFacesApi
{
    public static class GetPerson
    {
        [FunctionName("GetPerson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            var requestObj = await req.Content.ReadAsStringAsync();
            var parameters = JsonConvert.DeserializeObject<JObject>(requestObj);

            FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey), new DelegatingHandler[] { });
            faceClient.Endpoint = faceEndpoint;

            var result = await faceClient.PersonGroupPerson.GetAsync(personGroupId, Guid.Parse(parameters["personId"].ToString()));

            return new OkObjectResult(result);
        }
    }
}