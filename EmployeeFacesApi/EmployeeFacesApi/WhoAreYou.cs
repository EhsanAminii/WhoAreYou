using System;
using System.Collections.Generic;
using System.Linq;
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
    public static class WhoAreYou
    {
        [FunctionName("WhoAreYou")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var jsonDataString = await req.Content.ReadAsStringAsync();
            log.LogInformation(jsonDataString);
            var picture = JsonConvert.DeserializeObject<Picture>(jsonDataString);

            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
            var personGroupId = Environment.GetEnvironmentVariable("personGroupId");

            FaceClient faceClient = new FaceClient(
                    new ApiKeyServiceClientCredentials(subscriptionKey),
                    new System.Net.Http.DelegatingHandler[] { })
            { Endpoint = faceEndpoint };

            log.LogInformation("detecting with picture url");
            var detectedFaces = await faceClient.Face.DetectWithUrlAsync(picture.Url);

            var faceIds = new List<Guid>();
            foreach (var faces in detectedFaces)
            {
                if (faces.FaceId != null)
                {
                    faceIds.Add((Guid)faces.FaceId);
                }
            }

            var identifyResult = await faceClient.Face.IdentifyAsync(faceIds, personGroupId);

            var foundedPersons = new List<OrganizationUser>();

            foreach (var result in identifyResult)
            {
                if (result.Candidates.Any())
                {
                    var foundedPerson = result.Candidates.OrderByDescending(x => x.Confidence).First();
                    var person = await faceClient.PersonGroupPerson.GetAsync(personGroupId, foundedPerson.PersonId);
                    var organizationUserData = JsonConvert.DeserializeObject<OrganizationUser>(person.UserData);
                    var userPictureUrl = "https://hitachiemployee.blob.core.windows.net/profilepictures/" + organizationUserData.Email + ".jpg";
                    organizationUserData.PictureUrl = userPictureUrl;
                    organizationUserData.Confidence = (decimal)foundedPerson.Confidence * 100;
                    foundedPersons.Add(organizationUserData);
                }
            }

            return new OkObjectResult(foundedPersons);
        }
    }
}