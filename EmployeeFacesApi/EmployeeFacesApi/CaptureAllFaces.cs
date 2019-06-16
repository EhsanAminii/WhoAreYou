using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EmployeeFacesApi
{
    public static class CaptureAllFaces
    {
        private static readonly string PersonGroupId = Environment.GetEnvironmentVariable("personGroupId");

        [FunctionName("CaptureAllFaces")]
        public static async Task<List<PersistedFace>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequestMessage req,
            [Blob("events", Connection = "blobconnectionstring")]
            CloudBlobContainer container,
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

            //await faceClient.FaceList.CreateAsync("events", "implexisevents", null, RecognitionModel.Recognition02);

            //var blockBlob = container.GetBlockBlobReference("1.JPG");

            //var ms = new MemoryStream();
            //await blockBlob.DownloadToStreamAsync(ms);

            var detectedFaces = await faceClient.Face.DetectWithUrlAsync("https://hitachiemployee.blob.core.windows.net/captured/photo-05-21-2019-23:31:12.jpg", recognitionModel: RecognitionModel.Recognition02);
            var result = new List<PersistedFace>();

            foreach (var detectedFace in detectedFaces)
            {
                var targetFaces = new List<int>
                {
                    detectedFace.FaceRectangle.Left,
                    detectedFace.FaceRectangle.Top,
                    detectedFace.FaceRectangle.Width,
                    detectedFace.FaceRectangle.Height
                };

                var persistedFace = await faceClient.FaceList.AddFaceFromUrlAsync("events",
                    "https://hitachiemployee.blob.core.windows.net/captured/photo-05-21-2019-23:31:12.jpg",
                    null,
                    targetFaces);
                result.Add(persistedFace);
            }

            var detectedProfileFaces = await faceClient.Face.DetectWithUrlAsync("https://hitachiemployee.blob.core.windows.net/profilepictures/ehsan.amini@implexis-solutions.com.jpg", recognitionModel: RecognitionModel.Recognition02);
            var detectedFaceId = detectedProfileFaces.First().FaceId;
            if (detectedFaceId != null)
            {
                var similarFaces = await faceClient.Face.FindSimilarAsync(detectedFaceId.Value, "events", null);
            }

            return result;
        }

        private static async Task<Guid?> GetUserProfilePictureFaceId(string pictureUrl, FaceClient faceClient)
        {
            var detectedFaces = await faceClient.Face.DetectWithUrlAsync(pictureUrl);
            var userFaceId = detectedFaces.First().FaceId;
            return userFaceId;
        }

        private static async Task<Person> PersonIsRegistered(Guid? faceId, OrganizationUser user, FaceClient faceClient)
        {
            if (faceId == null)
            {
                return null;
            }

            var faceIds = new List<Guid> { (Guid)faceId };
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

            return null;
        }
    }
}