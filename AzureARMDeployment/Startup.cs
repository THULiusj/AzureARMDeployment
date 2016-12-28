using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AzureARMDeployment.Startup))]
namespace AzureARMDeployment
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
