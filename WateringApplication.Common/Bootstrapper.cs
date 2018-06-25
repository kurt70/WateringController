using Autofac;
using WateringApplication.Common.Objects;
using Windows.Devices.Gpio;

namespace WateringApplication.Common
{
    public class Bootstrapper
    {
        static public IContainer Container { get; private set; }
        public static void Init()
        {//GpioController
            var b = new ContainerBuilder();
            b.Register(c => GPIOControllerWrapper.GetController()).As<GpioController>();
            b.RegisterType<WaterLevelController>().As<IWaterLevelController>().SingleInstance();
            b.RegisterType<PumpEngineController>().As<IPumpEngineController>().SingleInstance();
            b.RegisterType<PumpStation>().As<IPumpStation>().SingleInstance();
            b.RegisterType<WaterLevelSensor>().As<IWaterLevelSensor>().InstancePerDependency();            
            Container = b.Build();
        }
    }
}
