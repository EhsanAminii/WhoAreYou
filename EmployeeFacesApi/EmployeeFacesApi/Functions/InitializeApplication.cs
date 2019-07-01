using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.Services;
using EmployeeFacesApi.Statics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public class InitializeApplication
    {
        private readonly IFaceService _faceService;

        public InitializeApplication(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("InitializeApplication")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var commands = await req.Content.ReadAsStringAsync();
                await _faceService.InitializeApplication(commands);
                await _faceService.CreateLargeFaceList(ConfigurationSettings.FaceListId);
                await _faceService.TrainPersonGroup(ConfigurationSettings.PersonGroupId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return (ActionResult)new BadRequestResult();
            }

            return (ActionResult)new OkResult();
        }
    }
}