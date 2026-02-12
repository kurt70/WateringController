#include "water_level_logic.h"

WaterLevelLogic::WaterLevelLogic(uint32_t publishIntervalMs)
  : lastSensors_{ { false, false, false, false } },
    lastPublishMs_(0),
    publishIntervalMs_(publishIntervalMs)
{
}

WaterLevelSnapshot WaterLevelLogic::BuildSnapshot(const std::array<bool, 4>& sensors) const
{
  int count = 0;
  for (bool value : sensors)
  {
    if (value)
    {
      count++;
    }
  }

  WaterLevelSnapshot snapshot;
  snapshot.sensors = sensors;
  snapshot.levelPercent = (count * 100) / 4;
  return snapshot;
}

bool WaterLevelLogic::HasChanged(const std::array<bool, 4>& sensors) const
{
  return sensors != lastSensors_;
}

bool WaterLevelLogic::ShouldPublish(bool changed, uint32_t nowMs) const
{
  if (changed)
  {
    return true;
  }

  return (nowMs - lastPublishMs_) >= publishIntervalMs_;
}

void WaterLevelLogic::MarkPublished(const std::array<bool, 4>& sensors, uint32_t nowMs)
{
  lastSensors_ = sensors;
  lastPublishMs_ = nowMs;
}

const std::array<bool, 4>& WaterLevelLogic::LastSensors() const
{
  return lastSensors_;
}

uint32_t WaterLevelLogic::LastPublishMs() const
{
  return lastPublishMs_;
}
