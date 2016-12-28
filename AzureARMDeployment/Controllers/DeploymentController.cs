using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;
using AzureARMDeployment.Models;

namespace AzureARMDeployment.Controllers
{
    public class DeploymentController : Controller
    {
        public ActionResult AutoStart(DeployParamModel input)
        {
            ActionResult result = SignIn(input);
            return result;
        }
        public ActionResult SignIn(DeployParamModel input)
        {
            //string stateMarker = Guid.NewGuid().ToString();
            string stateMarker = input.subscriptionId + "%%" + input.solutionName;
            string authorizationRequest = String.Format(
            "https://login.chinacloudapi.cn/common/oauth2/authorize?response_type=code&client_id={0}&resource={1}&redirect_uri={2}&state={3}",
             Uri.EscapeDataString("AzureADClientID"),
             Uri.EscapeDataString("https://management.chinacloudapi.cn/"),
             Uri.EscapeDataString(this.Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/Deployment/ProcessCode"),
             Uri.EscapeDataString(stateMarker)
             );
            return new RedirectResult(authorizationRequest);
        }

        public string ProcessCode(string code, string error, string error_description, string resource, string state)
        {
            var cc = new ClientCredential("[AzureADClientId]", "[AzureADClientSecret]"); //ClientId and Client Secret generated br Azure AD
            var context = new AuthenticationContext("https://login.chinacloudapi.cn/common");
            AuthenticationResult token = null;
            try
            {
                token = context.AcquireTokenByAuthorizationCode(code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), cc);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            if (token == null)
            {
                throw new InvalidOperationException("Could not get the token.");
            }
            return DoDeploy(token.AccessToken, state);
        }
        public string DoDeploy(string token, string state)
        {
            Random random = new Random();
            int randomNumber = random.Next(1000, 9999);
            string num = randomNumber.ToString();

            string splitStr = "%%";
            int subStrPos = state.IndexOf(splitStr);
            string solutionName = state.Substring(subStrPos+2,state.Length-subStrPos-2);
            string subscriptionId = state.Substring(0,subStrPos);

            //Deploy Parameters
            var groupName = solutionName + num;
            var rgPara = new ResourceGroup("China North");
            var deploymentName = "WebDirectWechatDeploy";

            //Resource Parameters
            string webAppName = solutionName + "web" + num;
            string sasToken = "?st=2016-12-28T06%3A56%3A00Z&se=2017-12-29T06%3A56%3A00Z&sp=rl&sv=2015-12-11&sr=b&sig=JE%2FM78PniHiff2EbPadAzq5NmJHGJPdyZoLk9W0fQp4%3D";//Web App Code Package Url token
            string deployPackageURI = "https://webappstor56.blob.core.chinacloudapi.cn/sampledeploy/LuisDialog_Stock_Bot.zip";//Web App Code package Url
            string storageAccountName = solutionName + "stor" + num;

            string webAppSvcPlanName = solutionName + "plan" + num;
            string sqlDBName = "SEM";
            string sqlServerName = solutionName + "sql" + num;
            string sqlAdministratorLogin = "dbuser";
            string sqlAdministratorLoginPassword = "1QAZ2wsx=";

            //Create Parameter Json
            string parametersJson = UpdateParameterJson(webAppName, sasToken, deployPackageURI, storageAccountName, webAppSvcPlanName, sqlDBName, sqlServerName, sqlAdministratorLogin, sqlAdministratorLoginPassword);

            var credential = new TokenCredentials(token);
            //Create ARM
            var dpResult = CreateTemplateDeploymentAsync(credential, rgPara, groupName, deploymentName, subscriptionId, parametersJson);
            string returnResult = "Deployment task has been successfully submitted. Please wait about 5 minutes. Result website url:  http://" + webAppName + ".chinacloudsites.cn";
            return returnResult;
        }
        public static async Task<DeploymentExtended> CreateTemplateDeploymentAsync(
                        ServiceClientCredentials credential,
                         ResourceGroup rgPara,
                        string groupName,
                        string deploymentName,
                        string subscriptionId,
                        string parametersJson)
        {
            Console.WriteLine("Creating the template deployment...");
            var resourceManagementClient = new ResourceManagementClient(new Uri("https://management.chinacloudapi.cn/"), credential)
            { SubscriptionId = subscriptionId };
            DeploymentExtended aa = null;
            try
            {
                var result = resourceManagementClient.ResourceGroups.CreateOrUpdateAsync(groupName, rgPara).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            string filePath = AppDomain.CurrentDomain.BaseDirectory + "WebappTemplate.json";
            var deployment = new Deployment();
            try
            {
                deployment.Properties = new DeploymentProperties
                {
                    Mode = DeploymentMode.Incremental,
                    Template = System.IO.File.ReadAllText(filePath),
                    Parameters = parametersJson
                };
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            try
            {
                aa = await resourceManagementClient.Deployments.CreateOrUpdateAsync(
                groupName,
                deploymentName,
                deployment);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return aa;
        }
        public static string UpdateParameterJson(
                    string webAppName,
                    string sasToken,
                    string deployPackageURI,
                    string storageAccountName,
                    string webAppSvcPlanName,
                    string sqlDBName,
                    string sqlServerName,
                    string sqlAdminLogin,
                    string sqlAdminPsd)
        {
            StringWriter sw = new StringWriter();
            JsonWriter writer = new JsonTextWriter(sw);
            //string webappName = "abcde";

            writer.WriteStartObject();
            writer.WritePropertyName("$schema");
            writer.WriteValue("http://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json");
            writer.WritePropertyName("contentVersion");
            writer.WriteValue("1.0.0.0");
            writer.WritePropertyName("parameters");
            writer.WriteStartObject();
            //
            writer.WritePropertyName("WebAppName");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(webAppName);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("SasToken");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(sasToken);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("DeployPackageURI");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(deployPackageURI);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("StorageAccountName");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(storageAccountName);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("WebAppSvcPlanName");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(webAppSvcPlanName);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("SQLDBName");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(sqlDBName);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("SQLServerName");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(sqlServerName);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("sqlAdministratorLogin");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(sqlAdminLogin);
            writer.WriteEndObject();
            //
            writer.WritePropertyName("sqlAdministratorLoginPassword");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue(sqlAdminPsd);
            writer.WriteEndObject();
            //
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();

            string jsonText = sw.GetStringBuilder().ToString();
            return jsonText;
        }
    }
}
