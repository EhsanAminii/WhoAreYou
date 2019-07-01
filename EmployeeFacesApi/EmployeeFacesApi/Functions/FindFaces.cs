using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public class FindFaces
    {
        private readonly IFaceService _faceService;

        public FindFaces(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("FindFaces")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var users = await _faceService.FindFacesInPictureAsync(req.Body);

            return new OkObjectResult(users);
        }
    }
}