using Moq;

namespace Ratatosk.Senders;

[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "SenderRepositoryAdapter")]
public class SenderRepositoryAdapterTests
{
    [Fact]
    public void Should_ThrowArgumentNullException_When_InnerIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SenderRepositoryAdapter<SenderEntity>(null!));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_ToTypedWithWrongType()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);
        var wrongSender = new Mock<ISender>();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            var _ = adapter.GetEntityKey(wrongSender.Object);
        });

        Assert.Contains("SenderEntity", ex.Message);
    }

    [Fact]
    public async Task Should_ThrowInvalidOperation_When_GetEntityKeyReturnsNull()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        mockInner.Setup(x => x.GetEntityKey(It.IsAny<SenderEntity>())).Returns((object?)null!);
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);
        var sender = new SenderEntity { Id = "1", Name = "test" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            adapter.GetEntityKey(sender);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Should_DelegateFindByName_ToInner()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        var expected = new SenderEntity { Id = "1", Name = "test" };
        mockInner.Setup(x => x.FindByNameAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);

        var result = await adapter.FindByNameAsync("test");

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task Should_DelegateFindByEndpoint_ToInner()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        var expected = new SenderEntity { Id = "1", Name = "test", Address = "+123", Type = EndpointType.PhoneNumber };
        mockInner.Setup(x => x.FindByEndpointAsync("+123", EndpointType.PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);

        var result = await adapter.FindByEndpointAsync("+123", EndpointType.PhoneNumber);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async Task Should_DelegateGetAllActive_ToInner()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        var senders = new List<SenderEntity>
        {
            new() { Id = "1", Name = "active1" },
            new() { Id = "2", Name = "active2" }
        };
        senders[0].Activate();
        senders[1].Activate();
        mockInner.Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(senders);
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);

        var result = await adapter.GetAllActiveAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Should_DelegateSetActive_ToInner()
    {
        var mockInner = new Mock<ISenderRepository<SenderEntity>>();
        var adapter = new SenderRepositoryAdapter<SenderEntity>(mockInner.Object);
        var sender = new SenderEntity { Id = "1", Name = "test" };

        await adapter.SetActiveAsync(sender, true);

        mockInner.Verify(x => x.SetActiveAsync(sender, true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
