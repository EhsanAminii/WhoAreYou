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
    public class TrainPersonGroup
    {
        private readonly IFaceService _faceService;

        public TrainPersonGroup(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("TrainPersonGroup")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation($"Start training person group");

            await _faceService.TrainPersonGroup(ConfigurationSettings.PersonGroupId);

            var trainingStatus = await _faceService.GetTrainingPersonGroupStatus(ConfigurationSettings.PersonGroupId);

            return new OkObjectResult(trainingStatus);
        }
    }
}