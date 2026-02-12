#include <unity.h>
#include <string>
#include "pump_logic.h"

static void assert_action(PumpDecision::Action expected, PumpDecision::Action actual)
{
  TEST_ASSERT_EQUAL_INT(static_cast<int>(expected), static_cast<int>(actual));
}

void test_start_requires_safe()
{
  PumpLogic logic(60000);
  auto decision = logic.EvaluateCommand("start", 30, "req", 1000);
  assert_action(PumpDecision::Action::None, decision.action);

  logic.UpdateWaterLevel(50, 1000);
  decision = logic.EvaluateCommand("start", 30, "req", 2000);
  assert_action(PumpDecision::Action::Start, decision.action);
  logic.ApplyDecision(decision, 2000, "2026-02-10T10:00:00Z");
  TEST_ASSERT_TRUE(logic.State().pumpRunning);
}

void test_start_blocks_when_stale_or_empty()
{
  PumpLogic logic(5000);
  logic.UpdateWaterLevel(50, 1000);
  auto decision = logic.EvaluateCommand("start", 30, "req", 7000);
  assert_action(PumpDecision::Action::None, decision.action);

  logic.UpdateWaterLevel(0, 8000);
  decision = logic.EvaluateCommand("start", 30, "req", 9000);
  assert_action(PumpDecision::Action::None, decision.action);
}

void test_stop_command_always_stops()
{
  PumpLogic logic(60000);
  logic.UpdateWaterLevel(50, 1000);
  auto decision = logic.EvaluateCommand("start", 10, "req", 1000);
  logic.ApplyDecision(decision, 1000, "2026-02-10T10:00:00Z");
  TEST_ASSERT_TRUE(logic.State().pumpRunning);

  decision = logic.EvaluateCommand("stop", 0, "req", 2000);
  assert_action(PumpDecision::Action::Stop, decision.action);
}

void test_mqtt_disconnect_stops_when_running()
{
  PumpLogic logic(60000);
  logic.UpdateWaterLevel(50, 1000);
  auto decision = logic.EvaluateCommand("start", 10, "req", 1000);
  logic.ApplyDecision(decision, 1000, "2026-02-10T10:00:00Z");

  decision = logic.OnMqttDisconnected();
  assert_action(PumpDecision::Action::Stop, decision.action);
}

void test_tick_stops_after_duration()
{
  PumpLogic logic(60000);
  logic.UpdateWaterLevel(50, 1000);
  auto decision = logic.EvaluateCommand("start", 5, "req", 1000);
  logic.ApplyDecision(decision, 1000, "2026-02-10T10:00:00Z");

  decision = logic.OnTick(4000);
  assert_action(PumpDecision::Action::None, decision.action);

  decision = logic.OnTick(7000);
  assert_action(PumpDecision::Action::Stop, decision.action);
}

void test_water_level_known_and_stale()
{
  PumpLogic logic(5000);
  TEST_ASSERT_FALSE(logic.IsWaterLevelKnown());
  TEST_ASSERT_TRUE(logic.IsWaterLevelStale(1000));

  logic.UpdateWaterLevel(10, 1000);
  TEST_ASSERT_TRUE(logic.IsWaterLevelKnown());
  TEST_ASSERT_FALSE(logic.IsWaterLevelStale(2000));
  TEST_ASSERT_TRUE(logic.IsWaterLevelStale(7001));
}

int main(int argc, char** argv)
{
  UNITY_BEGIN();
  RUN_TEST(test_start_requires_safe);
  RUN_TEST(test_start_blocks_when_stale_or_empty);
  RUN_TEST(test_stop_command_always_stops);
  RUN_TEST(test_mqtt_disconnect_stops_when_running);
  RUN_TEST(test_tick_stops_after_duration);
  RUN_TEST(test_water_level_known_and_stale);
  return UNITY_END();
}
