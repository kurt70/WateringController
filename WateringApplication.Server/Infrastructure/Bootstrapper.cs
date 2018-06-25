using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WateringApplication.Common.Objects;
using WateringApplication.Server.Managers;
using WateringApplication.Server.Objects;
using Windows.Devices.Gpio;

namespace WateringApplication.Server.Infrastructure
{
	class Bootstrapper
	{
		static public IContainer Container { get; private set; }
		public static void Init()
		{//GpioController
			
			var b = new ContainerBuilder();
			b.RegisterType<LogManager>().As<ILogger>().SingleInstance();
			//b.Register(c => GPIOControllerWrapper.GetController()).As<GpioController>();
			b.RegisterType<GpioController>().As<IGPIOController>().SingleInstance();
			//b.RegisterType<PumpEngineController>().As<IPumpEngineController>().SingleInstance();
			//b.RegisterType<PumpStation>().As<IPumpStation>().SingleInstance();
			//b.RegisterType<WaterLevelSensor>().As<IWaterLevelSensor>().InstancePerDependency();
			Container = b.Build();
		}
	}
}
