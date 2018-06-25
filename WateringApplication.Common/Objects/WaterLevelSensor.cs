using System;
using WateringApplication.Common.Constants;
using Windows.Devices.Gpio;

namespace WateringApplication.Common.Objects
{
    public delegate void SensorStateIsChangedEventHandler(object sender, SensorStateChangingEventArgs e);
        
    public class SensorStateChangingEventArgs:EventArgs
    {
        public GpioPinEdge SensorState { get; set; }
    }

    public interface IWaterLevelSensor
    {
        event SensorStateIsChangedEventHandler SensorStateIsChanged;
        bool WaterIsDetected { get; }
        WaterLevel LevelAssignation { get; }
        void SetInitValues(int pin, GpioPinValue offResult, WaterLevel levelAssignation);
    }

    public class WaterLevelSensor : IWaterLevelSensor
    {
        public event SensorStateIsChangedEventHandler SensorStateIsChanged;
                
        private int _pinNumber;
        private GpioPinValue _offResult;
        
        private bool _waterIsDetected;
        private IGPIOControllerWrapper _controller;

        public GpioPinEdge Status { get; private set; }

        public bool WaterIsDetected => _waterIsDetected ;

        public WaterLevel LevelAssignation { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pin">The pin number that is read</param>
        /// <param name="offResult"> Determines if High or Low is indicative of no water at this level</param>
        /// <param name="levelAssignation">The waterlevel assosiated with this sensor</param>
        public WaterLevelSensor(IGPIOControllerWrapper controller)
        {
            _controller = controller;
            Init();
        }

        private void Init()
        {
            var pin = _controller.OpenPin(_pinNumber);            
            pin.SetDriveMode(GpioPinDriveMode.Input);
            pin.DebounceTimeout = new TimeSpan(0, 0, 0, 1);//How often the value is checked
            pin.ValueChanged += Pin_ValueChanged;
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            DetermineIfWaterIsPresent(args.Edge);
            SensorStateIsChanged?.Invoke(sender, new SensorStateChangingEventArgs() { SensorState = args.Edge });
        }

        private bool DetermineIfWaterIsPresent(GpioPinEdge edge)
        {
            _waterIsDetected = edge == GpioPinEdge.RisingEdge && _offResult == GpioPinValue.Low ? true : false;
            return _waterIsDetected;
        }

        public void SetInitValues(int pin, GpioPinValue offResult, WaterLevel levelAssignation)
        {
            LevelAssignation = levelAssignation;
            _pinNumber = pin;
            _offResult = offResult;
        }
    }
}
