using System;
using Microsoft.Azure.WebJobs.Description;

namespace EmployeeFacesApi.Extensions
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class FromQueryAttribute : Attribute
    {
        public FromQueryAttribute(string name)
        {
            this.Value = "{query." + name + "}";
        }

        /// <summary>The field's value.</summary>
        [AutoResolve]
        public string Value { get; set; }
    }
}