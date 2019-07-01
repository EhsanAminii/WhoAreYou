using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using EmployeeFacesApi.Statics;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace EmployeeFacesApi.Services
{
    public class FaceService : IFaceService
    {
        private readonly FaceClient _faceClient;
        private readonly CloudBlobClient _cloudBlobClient;

        public FaceService(FaceClient faceClient, CloudBlobClient cloudBlobClient)
        {
            _faceClient = faceClient;
            _cloudBlobClient = cloudBlobClient;
        }

        public async Task<DetectedFace> DetectUserProfilePhotoFaceAsync(MemoryStream imageStream)
        {
            imageStream.Seek(0L, SeekOrigin.Begin);
            var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(
                imageStream, true, false, null, "recognition_01", false);

            if (detectedFaces == null || !detectedFaces.Any())
            {
                throw new ArgumentException("No face has been detected on the photo");
            }

            if (detectedFaces.Count > 1)
            {
                throw new ArgumentException("More than one face has been detected in the picture");
            }

            return detectedFaces.FirstOrDefault();
        }

        public async Task<List<FoundedPersonFace>> FindFacesInPictureAsync(Stream photoStream)
        {
            photoStream.Seek(0L, SeekOrigin.Begin);
            var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(
                photoStream, true, false, null, RecognitionModel.Recognition01);

            if (detectedFaces == null || !detectedFaces.Any())
            {
                throw new ArgumentException("No face has been detected on the photo");
            }

            var faceIds = new List<Guid>();
            foreach (var face in detectedFaces)
            {
                if (face.FaceId != null)
                {
                    faceIds.Add((Guid)face.FaceId);
                }
            }

            var identifyResult = await _faceClient.Face.IdentifyAsync(faceIds, ConfigurationSettings.PersonGroupId);

            var foundedPersonFaces = new List<FoundedPersonFace>();

            foreach (var result in identifyResult)
            {
                if (result.Candidates.Any())
                {
                    var detectedFace = detectedFaces.FirstOrDefault(x => x.FaceId == result.FaceId);

                    var foundedPerson = result.Candidates.OrderByDescending(x => x.Confidence).First();
                    var person = await _faceClient.PersonGroupPerson.GetAsync(ConfigurationSettings.PersonGroupId, foundedPerson.PersonId);
                    var foundedPersonData = JsonConvert.DeserializeObject<FoundedPersonFace>(person.UserData);
                    var userPictureUrl = $"{ConfigurationSettings.BlobStorageBaseUrl}/{ContainerSettings.ProfilePictureContainer}/{foundedPersonData.Email}.jpg";
                    foundedPersonData.PictureUrl = userPictureUrl;
                    foundedPersonData.Confidence = (decimal)foundedPerson.Confidence * 100;
                    foundedPersonData.FaceRectangle = detectedFace?.FaceRectangle;
                    foundedPersonData.FaceId = detectedFace?.FaceId?.ToString();

                    foundedPersonFaces.Add(foundedPersonData);
                }
            }

            return foundedPersonFaces;
        }

        public async Task<List<Picture>> FindPeopleInEventPicturesAsync(Stream photoStream)
        {
            var detectedFacesInPhoto = await _faceClient.Face.DetectWithStreamAsync(photoStream, recognitionModel: "recognition_01");

            var persistedFaces = new List<PersistedFace>();
            foreach (var face in detectedFacesInPhoto)
            {
                if (face.FaceId != null)
                {
                    var faceGuid = (Guid)face.FaceId;
                    var similarFaces = await _faceClient.Face.FindSimilarAsync(faceGuid, null, ConfigurationSettings.FaceListId);

                    foreach (var similarFace in similarFaces)
                    {
                        if (similarFace.PersistedFaceId != null)
                        {
                            var largeFaceGuid = (Guid)similarFace.PersistedFaceId;
                            var persistedFace = await _faceClient.LargeFaceList.GetFaceAsync(ConfigurationSettings.FaceListId, largeFaceGuid);
                            persistedFaces.Add(persistedFace);
                        }
                    }
                }
            }

            var foundedPictureUrls = new List<Picture>();

            foreach (var persistedFace in persistedFaces)
            {
                var pictureData = new Picture
                {
                    Url = persistedFace.UserData
                };

                foundedPictureUrls.Add(pictureData);
            }

            return foundedPictureUrls;
        }

        public async Task<Person> AddFaceToPerson(Guid personId, string userEmail, Stream photoStream)
        {
            await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(
                ConfigurationSettings.PersonGroupId,
                personId,
                photoStream);

            var person = await _faceClient.PersonGroupPerson.GetAsync(ConfigurationSettings.PersonGroupId, personId);

            var blobName = $"{userEmail}.json";
            await CreateBlobContent(blobName, ContainerSettings.PersonsContainer, person);

            return person;
        }

        public async Task<List<PersistedFace>> AddFaceToFaceList(Stream photo, string pictureUrl)
        {
            var newImageStream = new MemoryStream();
            var photoStream = new MemoryStream();
            photo.Position = 0;
            photo.CopyTo(newImageStream);

            photo.Position = 0;
            photo.CopyTo(photoStream);

            photo.Seek(0L, SeekOrigin.Begin);
            var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(photo, recognitionModel: "recognition_01");
            var persistedFacesList = new List<PersistedFace>();

            newImageStream.Seek(0L, SeekOrigin.Begin);
            var eventContainer = _cloudBlobClient.GetContainerReference(ContainerSettings.EventPhotoContainer);
            var eventBlockBlob = eventContainer.GetBlockBlobReference(pictureUrl);
            await eventBlockBlob.UploadFromStreamAsync(newImageStream);

            foreach (var face in detectedFaces)
            {
                if (face.FaceId != null)
                {
                    var targetFace = new List<int> { face.FaceRectangle.Left, face.FaceRectangle.Top, face.FaceRectangle.Width, face.FaceRectangle.Height };
                    var eventBlobStream = new MemoryStream();
                    await eventBlockBlob.DownloadToStreamAsync(eventBlobStream);
                    eventBlobStream.Seek(0L, SeekOrigin.Begin);
                    var persistedFace = await _faceClient.LargeFaceList.AddFaceFromStreamAsync(
                        ConfigurationSettings.FaceListId,
                        eventBlobStream,
                        pictureUrl,
                        targetFace);

                    persistedFacesList.Add(persistedFace);
                }
            }

            return persistedFacesList;
        }

        public async Task<bool> UpdateProfilePhoto(string email, Stream photoStream)
        {
            var userProfilePictureContainer = _cloudBlobClient.GetContainerReference(ContainerSettings.ProfilePictureContainer);
            var blockBlob = userProfilePictureContainer.GetBlockBlobReference($"{email}.jpg");
            await blockBlob.UploadFromStreamAsync(photoStream);

            return true;
        }

        public async Task<Person> CreatePersonWithFace(DetectedFace detectedFace, OrganizationUser organizationUser, Stream imageStream)
        {
            if (detectedFace?.FaceId == null)
            {
                return null;
            }

            // Create person
            var userPerson = await _faceClient.PersonGroupPerson.CreateAsync(
                ConfigurationSettings.PersonGroupId,
                organizationUser.Name,
                JsonConvert.SerializeObject(organizationUser));
            imageStream.Seek(0L, SeekOrigin.Begin);

            await _faceClient.PersonGroupPerson.AddFaceFromStreamAsync(
                ConfigurationSettings.PersonGroupId,
                userPerson.PersonId,
                imageStream);

            var person = await _faceClient.PersonGroupPerson.GetAsync(ConfigurationSettings.PersonGroupId, userPerson.PersonId);

            var blobName = $"{organizationUser.Email}.json";
            await CreateBlobContent(blobName, ContainerSettings.PersonsContainer, person);

            return person;
        }

        public async Task InitializeApplication(string command)
        {
            try
            {
                await _faceClient.PersonGroup.CreateAsync(ConfigurationSettings.PersonGroupId, "HitachiFaces");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task CreateLargeFaceList(string faceListId)
        {
            try
            {
                await _faceClient.LargeFaceList.CreateAsync(faceListId, "hitachievent", recognitionModel: "recognition_02");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task TrainPersonGroup(string personGroupId)
        {
            await _faceClient.PersonGroup.TrainAsync(personGroupId);
        }

        public async Task<TrainingStatus> GetTrainingPersonGroupStatus(string personGroupId)
        {
            TrainingStatus trainingStatus;
            while (true)
            {
                trainingStatus = await _faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);

                if (trainingStatus.Status != TrainingStatusType.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            return trainingStatus;
        }

        public async Task TrainFaceList(string largeFaceId)
        {
            await _faceClient.LargeFaceList.TrainAsync(largeFaceId);
        }

        public async Task<TrainingStatus> GetTrainingFaceListStatus(string largeFaceId)
        {
            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await _faceClient.LargeFaceList.GetTrainingStatusAsync(largeFaceId);

                if (trainingStatus.Status != TrainingStatusType.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            return trainingStatus;
        }

        public async Task<Person> GetPersonData(OrganizationUser organizationUser)
        {
            return (Person)await GetBlobContent<Person>($"{organizationUser.Email}.json", ContainerSettings.PersonsContainer);
        }

        public async Task<MemoryStream> GetBlobImageFileStream(string blobName, string containerName)
        {
            var userProfilePictureContainer = _cloudBlobClient.GetContainerReference(containerName);
            var imageBlockBlob = userProfilePictureContainer.GetBlockBlobReference(blobName);

            if (!await imageBlockBlob.ExistsAsync())
            {
                throw new FileNotFoundException("The profile picture was not found");
            }

            var memoryStream = new MemoryStream();
            await imageBlockBlob.DownloadToStreamAsync(memoryStream);
            memoryStream.Seek(0L, SeekOrigin.Begin);
            return memoryStream;
        }

        private async Task<object> GetBlobContent<T>(string blobName, string containerName)
        {
            var userProfilePictureContainer = _cloudBlobClient.GetContainerReference(containerName);
            var blockBlob = userProfilePictureContainer.GetBlockBlobReference(blobName);
            if (await blockBlob.ExistsAsync())
            {
                var blobContent = await blockBlob.DownloadTextAsync();
                return JsonConvert.DeserializeObject<T>(blobContent);
            }

            return null;
        }

        private async Task CreateBlobContent(string blobName, string containerName, object content)
        {
            var userProfilePictureContainer = _cloudBlobClient.GetContainerReference(containerName);
            var blockBlob = userProfilePictureContainer.GetBlockBlobReference(blobName);
            await blockBlob.UploadTextAsync(JsonConvert.SerializeObject(content));
        }
    }
}