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
    public class FindMorePictures
    {
        private readonly IFaceService _faceService;

        public FindMorePictures(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("FindMorePictures")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var pictures = await _faceService.FindPeopleInEventPicturesAsync(req.Body);

                return new OkObjectResult(pictures);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}