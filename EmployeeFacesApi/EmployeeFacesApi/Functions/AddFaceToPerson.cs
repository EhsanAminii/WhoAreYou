using System;
using System.Threading.Tasks;
using EmployeeFacesApi.Services;
using EmployeeFacesApi.Statics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeFacesApi.Functions
{
    public class AddFaceToPerson
    {
        private readonly IFaceService _faceService;

        public AddFaceToPerson(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("AddFaceToPerson")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Adding face to a person");

            try
            {
                var personIdQuery = req.Query["personId"];
                var personId = Guid.Parse(personIdQuery);
                var userEmail = req.Query["email"];

                var person = await _faceService.AddFaceToPerson(personId, userEmail, req.Body);
                await _faceService.TrainPersonGroup(ConfigurationSettings.PersonGroupId);
                return (ActionResult)new OkObjectResult(person);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                log.LogError($"error adding face to person: {e.Message}");
                return (ActionResult)new OkObjectResult(null);
            }
        }
    }
}