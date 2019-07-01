using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmployeeFacesApi.RequestModel;
using EmployeeFacesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmployeeFacesApi.Functions
{
    public class GetPerson
    {
        private readonly IFaceService _faceService;

        public GetPerson(IFaceService faceService)
        {
            _faceService = faceService;
        }

        [FunctionName("GetPerson")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var organizationUser = JsonConvert.DeserializeObject<OrganizationUser>(await req.Content.ReadAsStringAsync());
                var person = await _faceService.GetPersonData(organizationUser);

                return new OkObjectResult(person);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}