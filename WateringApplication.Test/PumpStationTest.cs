using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WateringApplication.Common.Objects;

namespace WateringApplication.Test
{
    [TestClass]
    public class PumpStationTest
    {
        private IContainer _container;

        [TestInitialize]
        public void Setup()
        {
            var b = new ContainerBuilder();
            b.Register(c => GenerateMockController()).As<IGPIOControllerWrapper>().SingleInstance();
            b.RegisterType<WaterLevelSensor>().As<IWaterLevelSensor>().InstancePerDependency();
            b.RegisterType<WaterLevelController>().As<IWaterLevelController>().SingleInstance();
            b.RegisterType<PumpEngineController>().As<IPumpEngineController>().SingleInstance();
            b.RegisterType<PumpStation>().As<IPumpStation>().SingleInstance();
            _container = b.Build();
        }

        private IGPIOControllerWrapper GenerateMockController()
        {
            var p = new StubIGPIOControllerWrapper();
            return p;
        }

        [TestMethod]
        public void Test()
        {
            var ps =_container.Resolve<IPumpStation>();            
        }
    }
}
