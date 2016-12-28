using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureARMDeployment.Models
{
    public class DeployParamModel
    {
        public string subscriptionId { get; set; }
        public string solutionName { get; set; }
    }
}