#ifndef PUMP_LOGIC_H
#define PUMP_LOGIC_H

#include <stdint.h>
#include <string>

/// <summary>
/// Describes a pump action derived from commands or safety logic.
/// </summary>
struct PumpDecision
{
  enum class Action
  {
    None,
    Start,
    Stop
  };

  Action action;
  uint32_t runSeconds;
  std::string requestId;
};

/// <summary>
/// Holds the current pump and water level state for decision making.
/// </summary>
struct PumpLogicState
{
  bool pumpRunning;
  uint32_t pumpStartMs;
  uint32_t pumpRunSeconds;
  std::string lastRequestId;
  std::string pumpStartIso;
  int lastWaterLevelPercent;
  uint32_t lastWaterLevelSeenMs;
};

/// <summary>
/// Encapsulates pump command decisions without direct hardware dependencies.
/// </summary>
class PumpLogic
{
public:
  explicit PumpLogic(uint32_t waterLevelStaleMs);

  void UpdateWaterLevel(int levelPercent, uint32_t nowMs);
  bool IsWaterLevelKnown() const;
  bool IsWaterLevelStale(uint32_t nowMs) const;
  bool IsWaterLevelSafe(uint32_t nowMs) const;

  PumpDecision EvaluateCommand(
    const std::string& action,
    int runSeconds,
    const std::string& requestId,
    uint32_t nowMs) const;

  PumpDecision OnMqttDisconnected() const;
  PumpDecision OnTick(uint32_t nowMs) const;

  void ApplyDecision(const PumpDecision& decision, uint32_t nowMs, const std::string& startIso);
  const PumpLogicState& State() const;

private:
  PumpLogicState state_;
  uint32_t waterLevelStaleMs_;
};

#endif
