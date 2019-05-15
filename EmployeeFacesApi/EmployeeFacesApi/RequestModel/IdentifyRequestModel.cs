using System;
using System.Collections.Generic;

namespace EmployeeFacesApi.RequestModel
{
    public class IdentifyRequestModel
    {
        public string personGroupId { get; set; }
        public List<Guid> faceIds { get; set; }
        public int maxNumOfCandidatesReturned { get; set; }
        public double confidenceThreshold { get; set; }
    }
}