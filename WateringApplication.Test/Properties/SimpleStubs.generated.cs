using System;
using System.Runtime.CompilerServices;
using Etg.SimpleStubs;
using Microsoft.IoT.Lightning.Providers;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Foundation.Metadata;
using WateringApplication.Common.Constants;
using System.Collections.Generic;
using System.Linq;

namespace WateringApplication.Common.Objects
{
    [CompilerGenerated]
    public class StubIGPIOControllerWrapper : IGPIOControllerWrapper
    {
        private readonly StubContainer<StubIGPIOControllerWrapper> _stubs = new StubContainer<StubIGPIOControllerWrapper>();

        public MockBehavior MockBehavior { get; set; }

        global::Windows.Devices.Gpio.GpioController global::WateringApplication.Common.Objects.IGPIOControllerWrapper.Controller
        {
            get
            {
                {
                    Controller_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<Controller_Get_Delegate>("get_Controller");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<Controller_Get_Delegate>("get_Controller", out del))
                        {
                            return default(GpioController);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        public delegate global::Windows.Devices.Gpio.GpioController Controller_Get_Delegate();

        public StubIGPIOControllerWrapper Controller_Get(Controller_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        global::Windows.Devices.Gpio.GpioPin global::WateringApplication.Common.Objects.IGPIOControllerWrapper.OpenPin(int pinNumber)
        {
            OpenPin_Int32_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<OpenPin_Int32_Delegate>("OpenPin");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<OpenPin_Int32_Delegate>("OpenPin", out del))
                {
                    return default(GpioPin);
                }
            }

            return del.Invoke(pinNumber);
        }

        public delegate global::Windows.Devices.Gpio.GpioPin OpenPin_Int32_Delegate(int pinNumber);

        public StubIGPIOControllerWrapper OpenPin(OpenPin_Int32_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        global::Windows.Devices.Gpio.GpioPin global::WateringApplication.Common.Objects.IGPIOControllerWrapper.OpenPin(int pinNumber, global::Windows.Devices.Gpio.GpioSharingMode sharingMode)
        {
            OpenPin_Int32_GpioSharingMode_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<OpenPin_Int32_GpioSharingMode_Delegate>("OpenPin");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<OpenPin_Int32_GpioSharingMode_Delegate>("OpenPin", out del))
                {
                    return default(GpioPin);
                }
            }

            return del.Invoke(pinNumber, sharingMode);
        }

        public delegate global::Windows.Devices.Gpio.GpioPin OpenPin_Int32_GpioSharingMode_Delegate(int pinNumber, global::Windows.Devices.Gpio.GpioSharingMode sharingMode);

        public StubIGPIOControllerWrapper OpenPin(OpenPin_Int32_GpioSharingMode_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        bool global::WateringApplication.Common.Objects.IGPIOControllerWrapper.TryOpenPin(int pinNumber, global::Windows.Devices.Gpio.GpioSharingMode sharingMode, out global::Windows.Devices.Gpio.GpioPin pin, out global::Windows.Devices.Gpio.GpioOpenStatus openStatus)
        {
            TryOpenPin_Int32_GpioSharingMode_GpioPin_GpioOpenStatus_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<TryOpenPin_Int32_GpioSharingMode_GpioPin_GpioOpenStatus_Delegate>("TryOpenPin");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<TryOpenPin_Int32_GpioSharingMode_GpioPin_GpioOpenStatus_Delegate>("TryOpenPin", out del))
                {
                    pin = default(GpioPin); openStatus = default(GpioOpenStatus); return default(bool);
                }
            }

            return del.Invoke(pinNumber, sharingMode, out pin, out openStatus);
        }

        public delegate bool TryOpenPin_Int32_GpioSharingMode_GpioPin_GpioOpenStatus_Delegate(int pinNumber, global::Windows.Devices.Gpio.GpioSharingMode sharingMode, out global::Windows.Devices.Gpio.GpioPin pin, out global::Windows.Devices.Gpio.GpioOpenStatus openStatus);

        public StubIGPIOControllerWrapper TryOpenPin(TryOpenPin_Int32_GpioSharingMode_GpioPin_GpioOpenStatus_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public StubIGPIOControllerWrapper(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            MockBehavior = mockBehavior;
        }
    }
}

namespace WateringApplication.Common.Objects
{
    [CompilerGenerated]
    public class StubIPumpEngineController : IPumpEngineController
    {
        private readonly StubContainer<StubIPumpEngineController> _stubs = new StubContainer<StubIPumpEngineController>();

        public MockBehavior MockBehavior { get; set; }

        global::WateringApplication.Common.Constants.PumpStatus global::WateringApplication.Common.Objects.IPumpEngineController.Status
        {
            get
            {
                {
                    Status_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<Status_Get_Delegate>("get_Status");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<Status_Get_Delegate>("get_Status", out del))
                        {
                            return default(PumpStatus);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        public event global::WateringApplication.Common.Objects.PumpStateIsChangedEventHandler PumpStateIsChanged;

        protected void On_PumpStateIsChanged(object sender, global::WateringApplication.Common.Objects.PumpStateChangingEventArgs e)
        {
            global::WateringApplication.Common.Objects.PumpStateIsChangedEventHandler handler = PumpStateIsChanged;
            if (handler != null) { handler(sender, e); }
        }

        public void PumpStateIsChanged_Raise(object sender, global::WateringApplication.Common.Objects.PumpStateChangingEventArgs e)
        {
            On_PumpStateIsChanged(sender, e);
        }

        global::WateringApplication.Common.Constants.PumpStatus global::WateringApplication.Common.Objects.IPumpEngineController.StartPump()
        {
            StartPump_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<StartPump_Delegate>("StartPump");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<StartPump_Delegate>("StartPump", out del))
                {
                    return default(PumpStatus);
                }
            }

            return del.Invoke();
        }

        public delegate global::WateringApplication.Common.Constants.PumpStatus StartPump_Delegate();

        public StubIPumpEngineController StartPump(StartPump_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        global::WateringApplication.Common.Constants.PumpStatus global::WateringApplication.Common.Objects.IPumpEngineController.StopPump()
        {
            StopPump_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<StopPump_Delegate>("StopPump");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<StopPump_Delegate>("StopPump", out del))
                {
                    return default(PumpStatus);
                }
            }

            return del.Invoke();
        }

        public delegate global::WateringApplication.Common.Constants.PumpStatus StopPump_Delegate();

        public StubIPumpEngineController StopPump(StopPump_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public delegate global::WateringApplication.Common.Constants.PumpStatus Status_Get_Delegate();

        public StubIPumpEngineController Status_Get(Status_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public StubIPumpEngineController(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            MockBehavior = mockBehavior;
        }
    }
}

namespace WateringApplication.Common.Objects
{
    [CompilerGenerated]
    public class StubIPumpStation : IPumpStation
    {
        private readonly StubContainer<StubIPumpStation> _stubs = new StubContainer<StubIPumpStation>();

        public MockBehavior MockBehavior { get; set; }

        global::WateringApplication.Common.Constants.WaterLevel global::WateringApplication.Common.Objects.IPumpStation.Level
        {
            get
            {
                {
                    Level_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<Level_Get_Delegate>("get_Level");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<Level_Get_Delegate>("get_Level", out del))
                        {
                            return default(WaterLevel);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        global::WateringApplication.Common.Constants.PumpStatus global::WateringApplication.Common.Objects.IPumpStation.PumpStatus
        {
            get
            {
                {
                    PumpStatus_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<PumpStatus_Get_Delegate>("get_PumpStatus");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<PumpStatus_Get_Delegate>("get_PumpStatus", out del))
                        {
                            return default(PumpStatus);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        global::System.DateTime global::WateringApplication.Common.Objects.IPumpStation.PumpStarted
        {
            get
            {
                {
                    PumpStarted_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<PumpStarted_Get_Delegate>("get_PumpStarted");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<PumpStarted_Get_Delegate>("get_PumpStarted", out del))
                        {
                            return default(DateTime);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        global::System.TimeSpan global::WateringApplication.Common.Objects.IPumpStation.PumpRunningTime
        {
            get
            {
                {
                    PumpRunningTime_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<PumpRunningTime_Get_Delegate>("get_PumpRunningTime");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<PumpRunningTime_Get_Delegate>("get_PumpRunningTime", out del))
                        {
                            return default(TimeSpan);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        public delegate global::WateringApplication.Common.Constants.WaterLevel Level_Get_Delegate();

        public StubIPumpStation Level_Get(Level_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public delegate global::WateringApplication.Common.Constants.PumpStatus PumpStatus_Get_Delegate();

        public StubIPumpStation PumpStatus_Get(PumpStatus_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public event global::WateringApplication.Common.Objects.WaterLevelChangedEventHandler WaterLevelChanged;

        protected void On_WaterLevelChanged(object sender, global::WateringApplication.Common.Objects.WaterLevelChangedArgs args)
        {
            global::WateringApplication.Common.Objects.WaterLevelChangedEventHandler handler = WaterLevelChanged;
            if (handler != null) { handler(sender, args); }
        }

        public void WaterLevelChanged_Raise(object sender, global::WateringApplication.Common.Objects.WaterLevelChangedArgs args)
        {
            On_WaterLevelChanged(sender, args);
        }

        public event global::WateringApplication.Common.Objects.PumpStateIsChangedEventHandler PumpStatusChanged;

        protected void On_PumpStatusChanged(object sender, global::WateringApplication.Common.Objects.PumpStateChangingEventArgs e)
        {
            global::WateringApplication.Common.Objects.PumpStateIsChangedEventHandler handler = PumpStatusChanged;
            if (handler != null) { handler(sender, e); }
        }

        public void PumpStatusChanged_Raise(object sender, global::WateringApplication.Common.Objects.PumpStateChangingEventArgs e)
        {
            On_PumpStatusChanged(sender, e);
        }

        public delegate global::System.DateTime PumpStarted_Get_Delegate();

        public StubIPumpStation PumpStarted_Get(PumpStarted_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public delegate global::System.TimeSpan PumpRunningTime_Get_Delegate();

        public StubIPumpStation PumpRunningTime_Get(PumpRunningTime_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public StubIPumpStation(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            MockBehavior = mockBehavior;
        }
    }
}

namespace WateringApplication.Common.Objects
{
    [CompilerGenerated]
    public class StubIWaterLevelController : IWaterLevelController
    {
        private readonly StubContainer<StubIWaterLevelController> _stubs = new StubContainer<StubIWaterLevelController>();

        public MockBehavior MockBehavior { get; set; }

        global::WateringApplication.Common.Constants.WaterLevel global::WateringApplication.Common.Objects.IWaterLevelController.WaterLevel
        {
            get
            {
                {
                    WaterLevel_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<WaterLevel_Get_Delegate>("get_WaterLevel");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<WaterLevel_Get_Delegate>("get_WaterLevel", out del))
                        {
                            return default(WaterLevel);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        public delegate global::WateringApplication.Common.Constants.WaterLevel WaterLevel_Get_Delegate();

        public StubIWaterLevelController WaterLevel_Get(WaterLevel_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public event global::WateringApplication.Common.Objects.WaterLevelChangedEventHandler WaterLevelChanged;

        protected void On_WaterLevelChanged(object sender, global::WateringApplication.Common.Objects.WaterLevelChangedArgs args)
        {
            global::WateringApplication.Common.Objects.WaterLevelChangedEventHandler handler = WaterLevelChanged;
            if (handler != null) { handler(sender, args); }
        }

        public void WaterLevelChanged_Raise(object sender, global::WateringApplication.Common.Objects.WaterLevelChangedArgs args)
        {
            On_WaterLevelChanged(sender, args);
        }

        public StubIWaterLevelController(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            MockBehavior = mockBehavior;
        }
    }
}

namespace WateringApplication.Common.Objects
{
    [CompilerGenerated]
    public class StubIWaterLevelSensor : IWaterLevelSensor
    {
        private readonly StubContainer<StubIWaterLevelSensor> _stubs = new StubContainer<StubIWaterLevelSensor>();

        public MockBehavior MockBehavior { get; set; }

        bool global::WateringApplication.Common.Objects.IWaterLevelSensor.WaterIsDetected
        {
            get
            {
                {
                    WaterIsDetected_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<WaterIsDetected_Get_Delegate>("get_WaterIsDetected");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<WaterIsDetected_Get_Delegate>("get_WaterIsDetected", out del))
                        {
                            return default(bool);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        global::WateringApplication.Common.Constants.WaterLevel global::WateringApplication.Common.Objects.IWaterLevelSensor.LevelAssignation
        {
            get
            {
                {
                    LevelAssignation_Get_Delegate del;
                    if (MockBehavior == MockBehavior.Strict)
                    {
                        del = _stubs.GetMethodStub<LevelAssignation_Get_Delegate>("get_LevelAssignation");
                    }
                    else
                    {
                        if (!_stubs.TryGetMethodStub<LevelAssignation_Get_Delegate>("get_LevelAssignation", out del))
                        {
                            return default(WaterLevel);
                        }
                    }

                    return del.Invoke();
                }
            }
        }

        public event global::WateringApplication.Common.Objects.SensorStateIsChangedEventHandler SensorStateIsChanged;

        protected void On_SensorStateIsChanged(object sender, global::WateringApplication.Common.Objects.SensorStateChangingEventArgs e)
        {
            global::WateringApplication.Common.Objects.SensorStateIsChangedEventHandler handler = SensorStateIsChanged;
            if (handler != null) { handler(sender, e); }
        }

        public void SensorStateIsChanged_Raise(object sender, global::WateringApplication.Common.Objects.SensorStateChangingEventArgs e)
        {
            On_SensorStateIsChanged(sender, e);
        }

        public delegate bool WaterIsDetected_Get_Delegate();

        public StubIWaterLevelSensor WaterIsDetected_Get(WaterIsDetected_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public delegate global::WateringApplication.Common.Constants.WaterLevel LevelAssignation_Get_Delegate();

        public StubIWaterLevelSensor LevelAssignation_Get(LevelAssignation_Get_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        void global::WateringApplication.Common.Objects.IWaterLevelSensor.SetInitValues(int pin, global::Windows.Devices.Gpio.GpioPinValue offResult, global::WateringApplication.Common.Constants.WaterLevel levelAssignation)
        {
            SetInitValues_Int32_GpioPinValue_WaterLevel_Delegate del;
            if (MockBehavior == MockBehavior.Strict)
            {
                del = _stubs.GetMethodStub<SetInitValues_Int32_GpioPinValue_WaterLevel_Delegate>("SetInitValues");
            }
            else
            {
                if (!_stubs.TryGetMethodStub<SetInitValues_Int32_GpioPinValue_WaterLevel_Delegate>("SetInitValues", out del))
                {
                    return;
                }
            }

            del.Invoke(pin, offResult, levelAssignation);
        }

        public delegate void SetInitValues_Int32_GpioPinValue_WaterLevel_Delegate(int pin, global::Windows.Devices.Gpio.GpioPinValue offResult, global::WateringApplication.Common.Constants.WaterLevel levelAssignation);

        public StubIWaterLevelSensor SetInitValues(SetInitValues_Int32_GpioPinValue_WaterLevel_Delegate del, int count = Times.Forever, bool overwrite = false)
        {
            _stubs.SetMethodStub(del, count, overwrite);
            return this;
        }

        public StubIWaterLevelSensor(MockBehavior mockBehavior = MockBehavior.Loose)
        {
            MockBehavior = mockBehavior;
        }
    }
}