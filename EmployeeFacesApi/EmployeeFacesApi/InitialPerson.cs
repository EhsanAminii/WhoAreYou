using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace EmployeeFacesApi
{
    public static class InitialPerson
    {
        private static readonly string PersonGroupId = Environment.GetEnvironmentVariable("personGroupId");

        [FunctionName("InitialPerson")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            [Blob("employeephotos", Connection = "blobconnectionstring")] CloudBlobContainer container,
            ILogger log)
        {
            log.LogInformation($"initialized");

            var userData = await req.Content.ReadAsAsync<OrganizationUser>();

            var subscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
            var faceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");

            await container.CreateIfNotExistsAsync();

            FaceClient faceClient = new FaceClient(
                    new ApiKeyServiceClientCredentials(subscriptionKey),
                    new System.Net.Http.DelegatingHandler[] { })
            { Endpoint = faceEndpoint };

            var userPictureUrl = "https://hitachiemployee.blob.core.windows.net/profilepictures/" + userData.Email + ".jpg";
            var userFaceId = await GetUserProfilePictureFaceId(userPictureUrl, faceClient);
            if (userFaceId == null)
            {
                return null;
            }

            var userPerson = await PersonIsRegistered(userFaceId, userData, faceClient);

            // if person is not registered
            if (userPerson == null)
            {
                userPerson = await faceClient.PersonGroupPerson.CreateAsync(PersonGroupId,
                    userData.Name,
                    JsonConvert.SerializeObject(userData));

                var persistedFace = await faceClient.PersonGroupPerson.AddFaceFromUrlAsync(PersonGroupId, userPerson.PersonId, userPictureUrl);
                await faceClient.PersonGroup.TrainAsync(PersonGroupId);
            }

            log.LogInformation($"{userPerson.Name} is detected! with id: {userPerson.PersonId}");

            return new OkObjectResult(userPerson);
        }

        private static async Task<Guid?> GetUserProfilePictureFaceId(string pictureUrl, FaceClient faceClient)
        {
            var detectedFaces = await faceClient.Face.DetectWithUrlAsync(pictureUrl, recognitionModel: "recognition_01", returnRecognitionModel: false);
            if (detectedFaces.Any())
            {
                var userFaceId = detectedFaces.First().FaceId;
                return userFaceId;
            }

            return null;
        }

        private static async Task<Person> PersonIsRegistered(Guid? faceId, OrganizationUser user, FaceClient faceClient)
        {
            if (faceId == null)
            {
                return null;
            }

            var faceIds = new List<Guid> { (Guid)faceId };
            try
            {
                var identifyResult = await faceClient.Face.IdentifyAsync(faceIds, PersonGroupId);
                if (!identifyResult.Any())
                {
                    return null;
                }

                var candidates = identifyResult.First().Candidates;
                if (!candidates.Any())
                {
                    return null;
                }

                var mostMatchCandidate = candidates.OrderByDescending(x => x.Confidence).First();

                var mostMatchCandidatePerson = await faceClient.PersonGroupPerson.GetAsync(PersonGroupId, mostMatchCandidate.PersonId);

                if (mostMatchCandidatePerson != null && mostMatchCandidatePerson.Name.Equals(user.Name))
                {
                    return mostMatchCandidatePerson;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }

            return null;
        }
    }
}