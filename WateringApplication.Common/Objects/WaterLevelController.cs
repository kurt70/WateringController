using System;
using System.Collections.Generic;
using System.Linq;
using WateringApplication.Common.Constants;

namespace WateringApplication.Common.Objects
{
    public delegate void WaterLevelChangedEventHandler(object sender, WaterLevelChangedArgs args);

    public class WaterLevelChangedArgs : EventArgs
    {
        public WaterLevel Level { get; set; }
    }

    public interface IWaterLevelController
    {
        WaterLevel WaterLevel { get; }
        event WaterLevelChangedEventHandler WaterLevelChanged;

    }

    public class WaterLevelController : IWaterLevelController
    {
        private List<IWaterLevelSensor> _levelSensorList;

        public event WaterLevelChangedEventHandler WaterLevelChanged;

        public WaterLevel WaterLevel { get; private set; }

        public WaterLevelController(IWaterLevelSensor empty, IWaterLevelSensor low, IWaterLevelSensor medium, IWaterLevelSensor full)
        {
            _levelSensorList = new List<IWaterLevelSensor>();
            empty.SetInitValues(GPIOPins.EmptyWaterLevelPin, Windows.Devices.Gpio.GpioPinValue.Low, WaterLevel.Empty);
            low.SetInitValues(GPIOPins.LowWaterLevelPin, Windows.Devices.Gpio.GpioPinValue.Low, WaterLevel.Low);
            medium.SetInitValues(GPIOPins.MediumWaterLevelPin, Windows.Devices.Gpio.GpioPinValue.Low, WaterLevel.Medium);
            full.SetInitValues(GPIOPins.FullWaterLevelPin, Windows.Devices.Gpio.GpioPinValue.Low, WaterLevel.Full);
            _levelSensorList.AddRange(new []{empty,low,medium,full});
            foreach (var item in _levelSensorList)
            {
                item.SensorStateIsChanged += OnSensorStateIsChanged;
            }
        }

        private void OnSensorStateIsChanged(object sender, SensorStateChangingEventArgs e)
        {
            //Determines the first sensor that is still detecting water by descending values. If none detects water, the barrel is empty
            var sensorThatDetectsWater = _levelSensorList.OrderByDescending(x => x.LevelAssignation).FirstOrDefault(y => y.WaterIsDetected);
            WaterLevel = sensorThatDetectsWater == null ? WaterLevel.Empty : sensorThatDetectsWater.LevelAssignation;
            WaterLevelChanged?.Invoke(sender, new WaterLevelChangedArgs() { Level = WaterLevel });
        }
    }
}
