using Moq;

namespace Ratatosk.Senders;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "InMemorySenderRepository")]
public class InMemorySenderRepositoryTests
{
    [Fact]
    public async Task Should_FindByName_When_SenderExists()
    {
        var sender = new SenderEntity { Id = "1", Name = "test-sender", Address = "+123", Type = EndpointType.PhoneNumber };
        sender.Activate();
        var repo = new InMemorySenderRepository(new[] { sender });

        var result = await repo.FindByNameAsync("test-sender");

        Assert.NotNull(result);
        Assert.Equal("test-sender", result.Name);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FindByNameNotFound()
    {
        var repo = new InMemorySenderRepository();

        var result = await repo.FindByNameAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_FindByEndpoint_When_SenderExists()
    {
        var sender = new SenderEntity { Id = "1", Name = "test", Address = "+123", Type = EndpointType.PhoneNumber };
        sender.Activate();
        var repo = new InMemorySenderRepository(new[] { sender });

        var result = await repo.FindByEndpointAsync("+123", EndpointType.PhoneNumber);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task Should_ReturnNull_When_FindByEndpointNotFound()
    {
        var repo = new InMemorySenderRepository();

        var result = await repo.FindByEndpointAsync("+999", EndpointType.PhoneNumber);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_GetAllActive_When_ActiveSendersExist()
    {
        var active = new SenderEntity { Id = "1", Name = "active", Address = "+1", Type = EndpointType.PhoneNumber };
        active.Activate();
        var inactive = new SenderEntity { Id = "2", Name = "inactive", Address = "+2", Type = EndpointType.PhoneNumber };
        inactive.Deactivate();
        var repo = new InMemorySenderRepository(new[] { active, inactive });

        var result = await repo.GetAllActiveAsync();

        Assert.Single(result);
        Assert.Contains(result, s => s.Name == "active");
    }

    [Fact]
    public async Task Should_SetActive_When_ActivateCalled()
    {
        var sender = new SenderEntity { Id = "1", Name = "test", Address = "+1", Type = EndpointType.PhoneNumber };
        sender.Deactivate();
        var repo = new InMemorySenderRepository(new[] { sender });

        await repo.SetActiveAsync(sender, true);

        Assert.True(sender.IsActive);
    }

    [Fact]
    public async Task Should_SetInactive_When_DeactivateCalled()
    {
        var sender = new SenderEntity { Id = "1", Name = "test", Address = "+1", Type = EndpointType.PhoneNumber };
        sender.Activate();
        var repo = new InMemorySenderRepository(new[] { sender });

        await repo.SetActiveAsync(sender, false);

        Assert.False(sender.IsActive);
    }
}
