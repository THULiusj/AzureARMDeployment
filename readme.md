# Azure China services automatically set up #

This project is to demonstrate how to automatically create Azure China services and deploy code to it using Azure ARM template and Azure Active Directory.

## Basic environment and technology ##

**.NET framework 4.5**
* MVC

**Azure China Services**

* Azure Active Directory
* ARM Template
 
## User step ##

Start the demo and input your subscription id and solution name in the text boxes.

![Start page Capture](../master/images/startpage.jpg?raw=true)

Press start button, then the login page will be popped up.

![Login page Capture](../master/images/loginpage.jpg?raw=true)

Log in with your Azure China account and the deployment task will be submitted. The result website url is highlighted.

![Task page Capture](../master/images/taskpage.jpg?raw=true)

You can also monitor deployment status in Azure Portal.

![Status page Capture](../master/images/statuspage.jpg?raw=true)

## Project description ##
### *Azure Active Directory configuration* ###
Register the web app in Azure Active Directory in classic Azure portal.
* Select web application
* Sign-on URL: provide the reply URL of the application (this is where Azure AD will return the user/code+token after authentication). For example: http://[**Your_Web_App_Name**].chinacloudsites.cn/Deployment
* AppIDURI: http://<**domain_name_of_you_directory**>/<**name_of_the_app**>. For example: http://[**Your_Azure_Domain_Name**].partner.onmschina.cn/[**Your_Web_App_Name**]
* Set the app as multi-tenant
* Save client id
* Create the secret and save it as client secret
* Set the redirect uri as http://[**Your_Web_App_Directory**]/Deployment/ProcessCode
* Add Access permition to **Windows Azure Service Management API**

### *Code description* ###
The core cs file is Deployment Controller. In this controller:

Turn to redirect uri by OAuth 2.0.
```.NET
string authorizationRequest = String.Format(
"https://login.chinacloudapi.cn/common/oauth2/authorize?response_type=code&client_id={0}&resource={1}&redirect_uri={2}&state={3}",
 Uri.EscapeDataString("[ClientId]"),
 Uri.EscapeDataString("https://management.chinacloudapi.cn/"),
 Uri.EscapeDataString(this.Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/Deployment/ProcessCode"),
 Uri.EscapeDataString(stateMarker)
 );
```
Get access token by logging in  Azure.
```.NET
 var cc = new ClientCredential("[Client Id]", "[Client Secret]"); //
 var context = new AuthenticationContext("https://login.chinacloudapi.cn/common");
 AuthenticationResult token = null;
 try
 {
     token = context.AcquireTokenByAuthorizationCode(code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), cc);
 }
```
Initialize deployment model with ARM template to update it to Azure. The ARM template is in WebappTemplate.json.
```.NET
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
```
Note: The initial project is created based on MVC template in visual studio. So there are some useless files in this project.