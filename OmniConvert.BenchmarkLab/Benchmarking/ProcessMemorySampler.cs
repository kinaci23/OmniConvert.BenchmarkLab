using System.Diagnostics;

namespace OmniConvert.BenchmarkLab.Benchmarking;

public sealed class ProcessMemorySampler : IAsyncDisposable
{
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cts = new();
    private Task? _samplingTask;

    private long _peakPrivateBytes;
    private long _lastPrivateBytes;

    public ProcessMemorySampler(TimeSpan? interval = null)
    {
        _interval = interval ?? TimeSpan.FromMilliseconds(10);
    }

    public long PeakPrivateBytes => Interlocked.Read(ref _peakPrivateBytes);

    public long LastPrivateBytes => Interlocked.Read(ref _lastPrivateBytes);

    public void Start()
    {
        _samplingTask = Task.Run(async () =>
        {
            using var process = Process.GetCurrentProcess();

            while (!_cts.IsCancellationRequested)
            {
                process.Refresh();

                long current = process.PrivateMemorySize64;

                Interlocked.Exchange(ref _lastPrivateBytes, current);

                long snapshotPeak;

                do
                {
                    snapshotPeak = _peakPrivateBytes;

                    if (current <= snapshotPeak)
                        break;

                } while (Interlocked.CompareExchange(
                    ref _peakPrivateBytes,
                    current,
                    snapshotPeak) != snapshotPeak);

                try
                {
                    await Task.Delay(_interval, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

        });
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_samplingTask != null)
        {
            try
            {
                await _samplingTask;
            }
            catch
            {
            }
        }

        _cts.Dispose();
    }
}