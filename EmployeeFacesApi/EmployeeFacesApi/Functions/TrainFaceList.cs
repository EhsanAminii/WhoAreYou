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
    public class TrainFaceList
    {
        private readonly IFaceService _faceService;

        public TrainFaceList(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("TrainFaceList")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await _faceService.TrainFaceList(ConfigurationSettings.FaceListId);

            var trainingStatus = await _faceService.GetTrainingFaceListStatus(ConfigurationSettings.FaceListId);

            return new OkObjectResult(trainingStatus);
        }
    }
}