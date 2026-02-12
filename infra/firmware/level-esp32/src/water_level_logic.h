#ifndef WATER_LEVEL_LOGIC_H
#define WATER_LEVEL_LOGIC_H

#include <array>
#include <cstdint>

/// <summary>
/// Snapshot of water level sensor readings.
/// </summary>
struct WaterLevelSnapshot
{
  std::array<bool, 4> sensors;
  int levelPercent;
};

/// <summary>
/// Encapsulates water level change detection and publish timing.
/// </summary>
class WaterLevelLogic
{
public:
  explicit WaterLevelLogic(uint32_t publishIntervalMs);

  WaterLevelSnapshot BuildSnapshot(const std::array<bool, 4>& sensors) const;
  bool HasChanged(const std::array<bool, 4>& sensors) const;
  bool ShouldPublish(bool changed, uint32_t nowMs) const;
  void MarkPublished(const std::array<bool, 4>& sensors, uint32_t nowMs);

  const std::array<bool, 4>& LastSensors() const;
  uint32_t LastPublishMs() const;

private:
  std::array<bool, 4> lastSensors_;
  uint32_t lastPublishMs_;
  uint32_t publishIntervalMs_;
};

#endif
