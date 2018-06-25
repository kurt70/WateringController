using Windows.ApplicationModel.Background;
using WateringApplication.Common;
using WateringApplication.Server.Controllers;
using Autofac;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace WateringApplication.Server
{
	public sealed class StartupTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
			var deferral = taskInstance.GetDeferral();

			Bootstrapper.Init();

			var mctrl = Bootstrapper.Container.Resolve<IMainController>();

			mctrl.Run();

			deferral.Complete();
		}
    }
}
