using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using EmployeeFacesApi.Services;
using EmployeeFacesApi.Statics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public class InitialPerson
    {
        private readonly IFaceService _faceService;

        public InitialPerson(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("InitialPerson")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            var organizationUserData = await req.Content.ReadAsAsync<OrganizationUser>();

            log.LogInformation($"Person {organizationUserData.Name} Initialized");

            try
            {
                var person = await _faceService.GetPersonData(organizationUserData);
                if (person == null)
                {
                    var userProfileImageName = $"{organizationUserData.Email}.jpg";
                    var imageStream = await _faceService.GetBlobImageFileStream(userProfileImageName, ContainerSettings.ProfilePictureContainer);

                    var newImageStream = new MemoryStream();
                    imageStream.Position = 0;
                    imageStream.CopyTo(newImageStream);

                    var detectedFace = await _faceService.DetectUserProfilePhotoFaceAsync(imageStream);

                    person = await _faceService.CreatePersonWithFace(detectedFace, organizationUserData, newImageStream);

                    await _faceService.TrainPersonGroup(ConfigurationSettings.PersonGroupId);

                    log.LogInformation($"{organizationUserData.Name} is registered as a person with id: {person}");
                }

                log.LogInformation($"{organizationUserData.Name} is detected! with id: {person}");

                return new OkObjectResult(person);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.LogError($"error initializing person: {e.Message}");
                return new OkObjectResult(null);
            }
        }
    }
}