#include <unity.h>
#include <array>
#include "water_level_logic.h"

static std::array<bool, 4> sensors(bool a, bool b, bool c, bool d)
{
  return { { a, b, c, d } };
}

void test_level_percent()
{
  WaterLevelLogic logic(1000);
  auto snap = logic.BuildSnapshot(sensors(false, false, false, false));
  TEST_ASSERT_EQUAL_INT(0, snap.levelPercent);

  snap = logic.BuildSnapshot(sensors(true, false, true, false));
  TEST_ASSERT_EQUAL_INT(50, snap.levelPercent);

  snap = logic.BuildSnapshot(sensors(true, true, true, true));
  TEST_ASSERT_EQUAL_INT(100, snap.levelPercent);
}

void test_change_detection()
{
  WaterLevelLogic logic(1000);
  TEST_ASSERT_FALSE(logic.HasChanged(sensors(false, false, false, false)));
  TEST_ASSERT_TRUE(logic.HasChanged(sensors(true, false, false, false)));
  logic.MarkPublished(sensors(true, false, false, false), 100);
  TEST_ASSERT_FALSE(logic.HasChanged(sensors(true, false, false, false)));
}

void test_should_publish()
{
  WaterLevelLogic logic(1000);
  TEST_ASSERT_TRUE(logic.ShouldPublish(true, 100));
  logic.MarkPublished(sensors(false, false, false, false), 100);
  TEST_ASSERT_FALSE(logic.ShouldPublish(false, 500));
  TEST_ASSERT_TRUE(logic.ShouldPublish(false, 1101));
}

int main(int argc, char** argv)
{
  UNITY_BEGIN();
  RUN_TEST(test_level_percent);
  RUN_TEST(test_change_detection);
  RUN_TEST(test_should_publish);
  return UNITY_END();
}
