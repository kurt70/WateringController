using System;
using WateringApplication.Common.Constants;
using Windows.Devices.Gpio;

namespace WateringApplication.Common.Objects
{
    public delegate void PumpStateIsChangedEventHandler(object sender, PumpStateChangingEventArgs e);
    
    public class PumpStateChangingEventArgs: EventArgs
    {
        public PumpStatus Status { get; set; }
    }

    public interface IPumpEngineController
    {
        event PumpStateIsChangedEventHandler PumpStateIsChanged;
        PumpStatus StartPump();
        PumpStatus StopPump();
        PumpStatus Status { get; }
    }
    public class PumpEngineController:IPumpEngineController
    {        
        public event PumpStateIsChangedEventHandler PumpStateIsChanged;
                
        private IWaterLevelController _waterLevelController;
        private IGPIOControllerWrapper _controller;

        public PumpStatus Status { get; private set; }

        public PumpEngineController(IWaterLevelController levelController, IGPIOControllerWrapper controller) 
        {
            _waterLevelController = levelController;
            _controller = controller;
            Init();
        }

        private void Init()
        {
            var pin = _controller.OpenPin(GPIOPins.RunPumpPin);
            pin.Write(GpioPinValue.Low);
            pin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public PumpStatus StartPump()
        {
            if (Status==PumpStatus.Stopped)
            {
                try
                {
                    //Check if there are water
                    if (_waterLevelController.WaterLevel != WaterLevel.Empty)
                    {
                        var pin = _controller.OpenPin(GPIOPins.RunPumpPin);
                        pin.Write(GpioPinValue.High);
                        Status = PumpStatus.Running;
                        PumpStateIsChanged?.Invoke(this, new PumpStateChangingEventArgs() { Status = Status });
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            return Status;
        }
        public PumpStatus StopPump()
        {
            if (Status == PumpStatus.Running)
            {
                try
                {
                    var pin = _controller.OpenPin(GPIOPins.RunPumpPin);
                    pin.Write(GpioPinValue.Low);
                    Status = PumpStatus.Stopped;
                    PumpStateIsChanged?.Invoke(this, new PumpStateChangingEventArgs() { Status = Status });
                }
                catch (Exception)
                {

                    throw;
                }
            }

            return Status;
        }
    }
}
