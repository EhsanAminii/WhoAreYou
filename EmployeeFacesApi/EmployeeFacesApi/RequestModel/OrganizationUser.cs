﻿namespace EmployeeFacesApi.RequestModel
{
    public class OrganizationUser
    {
        public string UserId { get; set; }

        public string PersonId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PictureUrl { get; set; }

        public decimal Confidence { get; set; }
    }
}