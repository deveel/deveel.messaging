using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Feature", "OpenTelemetry")]
public class OpenTelemetryBuilderExtensionsTests
{
    [Fact]
    public void AddRatatoskInstrumentation_OnTracerProviderBuilder_ExportsClientActivities()
    {
        var processor = new TestActivityProcessor();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddRatatoskInstrumentation()
            .AddProcessor(processor)
            .Build();

        var source = new ActivitySource("Ratatosk.Client");
        using var activity = source.StartActivity("test-activity");
        activity?.Stop();

        Assert.Contains(processor.Exported, a => a.Source.Name == "Ratatosk.Client");
    }

    [Fact]
    public void AddRatatoskInstrumentation_OnTracerProviderBuilder_ExportsConnectorActivities()
    {
        var processor = new TestActivityProcessor();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddRatatoskInstrumentation()
            .AddProcessor(processor)
            .Build();

        var source = new ActivitySource("Ratatosk.Connector.TwilioSms");
        using var activity = source.StartActivity("test-activity");
        activity?.Stop();

        Assert.Contains(processor.Exported, a => a.Source.Name == "Ratatosk.Connector.TwilioSms");
    }

    [Fact]
    public void AddRatatoskInstrumentation_OnMeterProviderBuilder_ExportsClientMetrics()
    {
        var exporter = new TestMetricExporter();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddRatatoskInstrumentation()
            .AddReader(new PeriodicExportingMetricReader(exporter, 10) { TemporalityPreference = MetricReaderTemporalityPreference.Cumulative })
            .Build();

        var meter = new Meter("Ratatosk.Client");
        var counter = meter.CreateCounter<int>("test_counter");
        counter.Add(1);

        meterProvider.ForceFlush();

        var exported = exporter.GetExported();
        Assert.Contains(exported, m => m.MeterName == "Ratatosk.Client");
    }

    [Fact]
    public void AddRatatoskInstrumentation_OnMeterProviderBuilder_ExportsConnectorMetrics()
    {
        var exporter = new TestMetricExporter();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddRatatoskInstrumentation()
            .AddReader(new PeriodicExportingMetricReader(exporter, 10) { TemporalityPreference = MetricReaderTemporalityPreference.Cumulative })
            .Build();

        var meter = new Meter("Ratatosk.Connector.SendGridEmail");
        var counter = meter.CreateCounter<int>("test_counter");
        counter.Add(1);

        meterProvider.ForceFlush();

        var exported = exporter.GetExported();
        Assert.Contains(exported, m => m.MeterName == "Ratatosk.Connector.SendGridEmail");
    }

    [Fact]
    public void AddRatatoskInstrumentation_OnTracerProviderBuilder_CanBeCalledMultipleTimes()
    {
        var exception = Record.Exception(() =>
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddRatatoskInstrumentation()
                .AddRatatoskInstrumentation()
                .Build();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void AddRatatoskInstrumentation_OnMeterProviderBuilder_CanBeCalledMultipleTimes()
    {
        var exception = Record.Exception(() =>
        {
            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddRatatoskInstrumentation()
                .AddRatatoskInstrumentation()
                .Build();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void WithOpenTelemetry_OnMessagingBuilder_RegistersOpenTelemetryServices()
    {
        var services = new ServiceCollection();

        services.AddMessaging()
            .WithOpenTelemetry();

        var provider = services.BuildServiceProvider();

        var tracerProvider = provider.GetService<TracerProvider>();
        var meterProvider = provider.GetService<MeterProvider>();

        Assert.NotNull(tracerProvider);
        Assert.NotNull(meterProvider);
    }

    [Fact]
    public void WithOpenTelemetry_OnMessagingBuilder_ReturnsBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddMessaging()
            .WithOpenTelemetry();

        Assert.NotNull(builder);
        Assert.Same(services, builder.Services);
    }

    private sealed class TestActivityProcessor : BaseProcessor<Activity>
    {
        public List<Activity> Exported { get; } = new();

        public override void OnEnd(Activity data)
        {
            Exported.Add(data);
            base.OnEnd(data);
        }
    }

    private sealed class TestMetricExporter : BaseExporter<Metric>
    {
        private readonly object _lock = new();
        private readonly List<Metric> _exported = new();

        public List<Metric> GetExported()
        {
            lock (_lock)
                return _exported.ToList();
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            lock (_lock)
            {
                foreach (var metric in batch)
                    _exported.Add(metric);
            }
            return ExportResult.Success;
        }
    }
}
