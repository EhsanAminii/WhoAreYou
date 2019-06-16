using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi
{
    public static class DetectFace
    {
        [FunctionName("DetectFace")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var detectedFaces = await req.Content.ReadAsAsync<List<DetectedFace>>();

            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            FaceClient faceClient = new FaceClient(
                new ApiKeyServiceClientCredentials(subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { })
            { Endpoint = faceEndpoint };

            var faceIds = new List<Guid>();
            foreach (var faces in detectedFaces)
            {
                if (faces.FaceId != null)
                {
                    faceIds.Add((Guid)faces.FaceId);
                }
            }

            var result = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);

            return new OkObjectResult(result);
        }
    }
}