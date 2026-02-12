using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace WateringController.Frontend.E2E;

public sealed class SmokeTests : PageTest
{
    private string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5291";

    [SetUp]
    public async Task SetUpAsync()
    {
        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    [Test]
    public async Task Home_RendersConnectionsAndScheduling()
    {
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Status", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Connections", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Scheduling", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_WorksAcrossPages()
    {
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Schedules", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Schedules", Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Control", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Control", Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Status", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Status", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Scheduling_ShowsNextWithDateAndFutureLabel()
    {
        await ClearSchedulesAsync();
        var nextUtc = DateTimeOffset.UtcNow.AddHours(1);
        var startTimeUtc = nextUtc.ToString("HH:mm:ss");

        var create = await Page.Context.APIRequest.PostAsync($"{BaseUrl}/api/schedules", new()
        {
            DataObject = new
            {
                enabled = true,
                startTimeUtc,
                runSeconds = 30,
                daysOfWeek = (string?)null
            }
        });
        Assert.That(create.Ok, Is.True, "Failed to create schedule.");

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var schedulingCard = Page.Locator(".card:has-text('Scheduling')");
        var nextLine = schedulingCard.Locator("div", new() { HasTextString = "Next:" }).First;
        await Expect(nextLine).ToBeVisibleAsync();
        await Expect(nextLine).ToContainTextAsync(new Regex(@"\d{4}-\d{2}-\d{2}"));
        await Expect(nextLine).ToContainTextAsync("in ");
    }

    [Test]
    public async Task Alarms_ShowsAtMostFiveAndNoDuplicates()
    {
        var baseTime = DateTimeOffset.UtcNow;
        for (var i = 0; i < 6; i++)
        {
            var message = $"e2e-alarm-{Guid.NewGuid():N}";
            var payload = new
            {
                type = "LOW_WATER",
                severity = "warning",
                message,
                raisedAt = baseTime.AddSeconds(i).ToString("O")
            };

            var response = await Page.Context.APIRequest.PostAsync($"{BaseUrl}/api/test/mqtt/alarm", new()
            {
                DataObject = payload
            });

            Assert.That(response.Ok, Is.True, "Failed to publish test alarm (MQTT must be connected)." );
        }

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var items = Page.Locator(".card:has-text('Alarms') .list-group-item");
        await Expect(items).ToHaveCountAsync(5);

        var texts = await items.AllInnerTextsAsync();
        Assert.That(texts.Distinct().Count(), Is.EqualTo(texts.Count));
        Assert.That(texts.All(text => text.Contains("e2e-alarm-")), Is.True,
            "Expected latest alarms to be the injected test alarms.");
    }

    [Test]
    public async Task Schedules_AllDaysToggle_SelectsAndClears()
    {
        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Schedules", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Schedules", Exact = true })).ToBeVisibleAsync();

        var addCard = Page.Locator(".card:has-text('Add schedule')");
        var allToggle = addCard.GetByRole(AriaRole.Checkbox, new() { Name = "All" });

        await allToggle.CheckAsync();
        var dayCheckboxes = addCard.Locator(".form-check-input").Filter(new LocatorFilterOptions
        {
            HasNot = addCard.GetByRole(AriaRole.Checkbox, new() { Name = "Enabled" })
        });

        for (var i = 0; i < 7; i++)
        {
            await Expect(dayCheckboxes.Nth(i)).ToBeCheckedAsync();
        }

        await allToggle.UncheckAsync();
        for (var i = 0; i < 7; i++)
        {
            await Expect(dayCheckboxes.Nth(i)).Not.ToBeCheckedAsync();
        }
    }

    [Test]
    public async Task Alarms_DedupesOnSignalRPush()
    {
        var message = $"e2e-dup-alarm-{Guid.NewGuid():N}";
        var payload = new
        {
            type = "LOW_WATER",
            severity = "warning",
            message,
            raisedAt = DateTimeOffset.UtcNow.ToString("O")
        };

        var first = await Page.Context.APIRequest.PostAsync($"{BaseUrl}/api/test/mqtt/alarm", new()
        {
            DataObject = payload
        });
        Assert.That(first.Ok, Is.True, "Failed to publish first test alarm.");

        var second = await Page.Context.APIRequest.PostAsync($"{BaseUrl}/api/test/mqtt/alarm", new()
        {
            DataObject = payload
        });
        Assert.That(second.Ok, Is.True, "Failed to publish duplicate test alarm.");

        await Page.GotoAsync(BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var items = Page.Locator(".card:has-text('Alarms') .list-group-item");
        var texts = await items.AllInnerTextsAsync();
        var duplicateCount = texts.Count(text => text.Contains(message, StringComparison.Ordinal));
        Assert.That(duplicateCount, Is.EqualTo(1));
    }

    private async Task ClearSchedulesAsync()
    {
        var response = await Page.Context.APIRequest.GetAsync($"{BaseUrl}/api/schedules");
        if (!response.Ok)
        {
            return;
        }

        var schedules = await response.JsonAsync<List<ScheduleDto>>();
        if (schedules is null)
        {
            return;
        }

        foreach (var schedule in schedules)
        {
            await Page.Context.APIRequest.DeleteAsync($"{BaseUrl}/api/schedules/{schedule.Id}");
        }
    }

    private sealed record ScheduleDto(int Id);
}
