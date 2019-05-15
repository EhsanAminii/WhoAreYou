using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi
{
    public static class AddPerson
    {
        [FunctionName("AddPerson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            var content = await req.Content.ReadAsStreamAsync();
            var imageFile = await req.Content.ReadAsStreamAsync();

            FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey), new DelegatingHandler[] { });
            faceClient.Endpoint = faceEndpoint;

            try
            {
                var detectedFaces = await faceClient.Face.DetectWithStreamAsync(content);
                if (detectedFaces.Count > 0)
                {
                    var faceIds = new List<Guid>();
                    var detectedFaceId = detectedFaces[0].FaceId;
                    faceIds.Add((Guid)detectedFaceId);
                    var identifiedFaces = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);
                    foreach (var face in identifiedFaces)
                    {
                        if (face.Candidates.Count == 0)
                        {
                            var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, "new person");
                            var persistedFace = await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageFile);

                            await faceClient.PersonGroup.TrainAsync(personGroupId);
                            return new OkObjectResult(person);
                        }
                        else
                        {
                            var personId = face.Candidates[0].PersonId;
                            var existingPerson = await faceClient.PersonGroupPerson.GetAsync(personGroupId, personId);
                            return new OkObjectResult(existingPerson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }

            return new OkObjectResult("No face detected!");
        }
    }
}