using System;
using WateringApplication.Common.Constants;

namespace WateringApplication.Common.Objects
{
    public interface IPumpStation
    {
        WaterLevel Level { get; }
        PumpStatus PumpStatus { get; }
        event WaterLevelChangedEventHandler WaterLevelChanged;
        event PumpStateIsChangedEventHandler PumpStatusChanged;
        DateTime PumpStarted { get; }
        TimeSpan PumpRunningTime{ get; }
    }
    public class PumpStation : IPumpStation
    {
        public WaterLevel Level => _waterLevelController.WaterLevel;

        public PumpStatus PumpStatus => _pumpEngineController.Status;

        public DateTime PumpStarted => throw new NotImplementedException();

        public TimeSpan PumpRunningTime => throw new NotImplementedException();
        

        private IPumpEngineController _pumpEngineController;
        private IWaterLevelController _waterLevelController;

        public event WaterLevelChangedEventHandler WaterLevelChanged;
        public event PumpStateIsChangedEventHandler PumpStatusChanged;

        public PumpStation(IPumpEngineController pumpEngineCtrl, IWaterLevelController waterLevelCtrl)
        {
            _pumpEngineController = pumpEngineCtrl;
            _waterLevelController = waterLevelCtrl;
            Init();            
        }

        private void Init()
        {
            _pumpEngineController.PumpStateIsChanged += OnPumpStateIsChanged;
            _waterLevelController.WaterLevelChanged += OnWaterLevelChanged;
        }

        private void OnPumpStateIsChanged(object sender, PumpStateChangingEventArgs e)
        {
            PumpStatusChanged?.Invoke(sender, e);
        }

        private void OnWaterLevelChanged(object sender, WaterLevelChangedArgs e)
        {
            WaterLevelChanged?.Invoke(sender, e);
        }
    }
}
