using System;

namespace EmployeeFacesApi.Statics
{
    public class ConfigurationSettings
    {
        public static readonly string SubscriptionKey = Environment.GetEnvironmentVariable("subscriptionkey");
        public static readonly string FaceEndpoint = Environment.GetEnvironmentVariable("faceEndpoint");
        public static readonly string PersonGroupId = Environment.GetEnvironmentVariable("personGroupId");
        public static readonly string BlobStorageBaseUrl = Environment.GetEnvironmentVariable("blobstoragebaseurl");
        public static readonly string BlobStorageAccountName = Environment.GetEnvironmentVariable("blobstorageaccountname");
        public static readonly string BlobStorageAccountKey = Environment.GetEnvironmentVariable("blobstorageaccountkey");

        public static readonly string FaceListId = Environment.GetEnvironmentVariable("largefaceid");
    }
}