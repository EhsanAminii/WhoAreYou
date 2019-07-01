using System;
using System.Threading.Tasks;
using EmployeeFacesApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public class AddFacesToFaceList
    {
        private readonly IFaceService _faceService;

        public AddFacesToFaceList(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("AddFacesToFaceList")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var pictureUrl = req.Query["pictureUrl"];
                var persistedFaceList = await _faceService.AddFaceToFaceList(req.Body, pictureUrl);
                return new OkObjectResult(persistedFaceList);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.LogError(e.Message);
                return new OkObjectResult(null);
            }
        }
    }
}