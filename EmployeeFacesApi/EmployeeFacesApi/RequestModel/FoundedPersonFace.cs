using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace EmployeeFacesApi.RequestModel
{
    public class FoundedPersonFace : OrganizationUser
    {
        public string FaceId { get; set; }

        public FaceRectangle FaceRectangle { get; set; }
    }
}