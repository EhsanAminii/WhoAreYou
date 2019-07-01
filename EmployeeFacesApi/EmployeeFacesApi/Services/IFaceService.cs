using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.WindowsAzure.Storage.Blob;

namespace EmployeeFacesApi.Services
{
    public interface IFaceService
    {
        Task<Person> GetPersonData(OrganizationUser organizationUser);

        Task<MemoryStream> GetBlobImageFileStream(string blobName, string containerName);

        Task<DetectedFace> DetectUserProfilePhotoFaceAsync(MemoryStream imageStream);

        Task<List<FoundedPersonFace>> FindFacesInPictureAsync(Stream photoStream);

        Task<List<Picture>> FindPeopleInEventPicturesAsync(Stream photoStream);

        Task<Person> AddFaceToPerson(Guid personId, string userEmail, Stream photoStream);

        Task<List<PersistedFace>> AddFaceToFaceList(Stream photo, string picture);

        Task<bool> UpdateProfilePhoto(string email, Stream photoStream);

        Task<Person> CreatePersonWithFace(DetectedFace face, OrganizationUser organizationUser, Stream imageStream);

        //Task<bool> PersonIsRegistered();
        Task CreateLargeFaceList(string faceListId);

        Task InitializeApplication(string command);

        Task TrainPersonGroup(string personGroupId);

        Task<TrainingStatus> GetTrainingPersonGroupStatus(string personGroupId);

        Task TrainFaceList(string largeFaceId);

        Task<TrainingStatus> GetTrainingFaceListStatus(string largeFaceId);
    }
}