#include "pump_logic.h"

PumpLogic::PumpLogic(uint32_t waterLevelStaleMs)
  : state_{false, 0, 0, "", "", -1, 0},
    waterLevelStaleMs_(waterLevelStaleMs)
{
}

void PumpLogic::UpdateWaterLevel(int levelPercent, uint32_t nowMs)
{
  state_.lastWaterLevelPercent = levelPercent;
  state_.lastWaterLevelSeenMs = nowMs;
}

bool PumpLogic::IsWaterLevelKnown() const
{
  return state_.lastWaterLevelPercent >= 0;
}

bool PumpLogic::IsWaterLevelStale(uint32_t nowMs) const
{
  if (!IsWaterLevelKnown())
  {
    return true;
  }
  return (nowMs - state_.lastWaterLevelSeenMs) > waterLevelStaleMs_;
}

bool PumpLogic::IsWaterLevelSafe(uint32_t nowMs) const
{
  return IsWaterLevelKnown() && !IsWaterLevelStale(nowMs) && state_.lastWaterLevelPercent > 0;
}

PumpDecision PumpLogic::EvaluateCommand(
  const std::string& action,
  int runSeconds,
  const std::string& requestId,
  uint32_t nowMs) const
{
  if (action == "stop")
  {
    return { PumpDecision::Action::Stop, 0, requestId };
  }

  if (!IsWaterLevelSafe(nowMs))
  {
    return { PumpDecision::Action::None, 0, requestId };
  }

  if (runSeconds <= 0)
  {
    return { PumpDecision::Action::None, 0, requestId };
  }

  return { PumpDecision::Action::Start, static_cast<uint32_t>(runSeconds), requestId };
}

PumpDecision PumpLogic::OnMqttDisconnected() const
{
  if (state_.pumpRunning)
  {
    return { PumpDecision::Action::Stop, 0, state_.lastRequestId };
  }

  return { PumpDecision::Action::None, 0, state_.lastRequestId };
}

PumpDecision PumpLogic::OnTick(uint32_t nowMs) const
{
  if (!state_.pumpRunning)
  {
    return { PumpDecision::Action::None, 0, state_.lastRequestId };
  }

  if (state_.pumpRunSeconds == 0)
  {
    return { PumpDecision::Action::None, 0, state_.lastRequestId };
  }

  const uint32_t elapsedSeconds = (nowMs - state_.pumpStartMs) / 1000;
  if (elapsedSeconds >= state_.pumpRunSeconds)
  {
    return { PumpDecision::Action::Stop, 0, state_.lastRequestId };
  }

  return { PumpDecision::Action::None, 0, state_.lastRequestId };
}

void PumpLogic::ApplyDecision(const PumpDecision& decision, uint32_t nowMs, const std::string& startIso)
{
  if (decision.action == PumpDecision::Action::Start)
  {
    state_.pumpRunning = true;
    state_.pumpStartMs = nowMs;
    state_.pumpRunSeconds = decision.runSeconds;
    state_.lastRequestId = decision.requestId;
    state_.pumpStartIso = startIso;
    return;
  }

  if (decision.action == PumpDecision::Action::Stop)
  {
    state_.pumpRunning = false;
  }
}

const PumpLogicState& PumpLogic::State() const
{
  return state_;
}
