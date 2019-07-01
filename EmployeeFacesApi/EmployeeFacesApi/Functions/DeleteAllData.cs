using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public static class DeleteAllData
    {
        [FunctionName("DeleteAllData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            FaceClient faceClient = new FaceClient(
                new ApiKeyServiceClientCredentials(subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });
            faceClient.Endpoint = faceEndpoint;

            var personGroupPersons = await faceClient.PersonGroupPerson.ListAsync(personGroupId);

            foreach (var personGroupPerson in personGroupPersons)
            {
                await faceClient.PersonGroupPerson.DeleteAsync(personGroupId, personGroupPerson.PersonId);
            }

            await faceClient.PersonGroup.TrainAsync(personGroupId);

            return (ActionResult)new OkObjectResult($"All data deleted");
        }
    }
}