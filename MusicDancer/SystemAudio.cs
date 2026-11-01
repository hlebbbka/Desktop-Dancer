using System.Runtime.InteropServices;

namespace MusicDancer;

// WASAPI loopback reads the default playback endpoint, not the microphone.
internal sealed class SystemAudio : IDisposable
{
    private readonly CancellationTokenSource cancel = new();
    private readonly Thread thread;
    public volatile float MixerLevel; public volatile float Level; public volatile float Bass, Mid, High; private double low, mid;
    public float[] Spectrum = new float[52]; private readonly float[] samples = new float[1024]; private readonly double[] spectrumWindow = new double[1024], spectrumCoefficients = new double[52]; private int sampleCount; private int sampleRate; private DateTime lastPacket = DateTime.UtcNow;
    public event Action<string>? Status;
    public SystemAudio() { thread = new Thread(Run) { IsBackground = true, Name = "System audio" }; }
    public void Start() => thread.Start();
    private void Run()
    {
        IAudioMeterInformation? meter = null; IAudioClient? client = null; IAudioCaptureClient? capture = null; IMMDevice? device = null; IMMDeviceEnumerator? enumerator = null; IntPtr format = IntPtr.Zero;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(0, 1, out device));
            Guid meterId = typeof(IAudioMeterInformation).GUID;
            if (device.Activate(ref meterId, 23, IntPtr.Zero, out object meterObject) >= 0) meter = (IAudioMeterInformation)meterObject;
            Guid clientId = typeof(IAudioClient).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref clientId, 23, IntPtr.Zero, out object obj)); client = (IAudioClient)obj;
            Marshal.ThrowExceptionForHR(client.GetMixFormat(out format));
            sampleRate = Marshal.ReadInt32(format, 4);
            for (int i = 0; i < spectrumWindow.Length; i++) spectrumWindow[i] = .5 - .5 * Math.Cos(2 * Math.PI * i / (spectrumWindow.Length - 1));
            for (int band = 0; band < spectrumCoefficients.Length; band++)
            {
                double freq = 55 * Math.Pow(12000.0 / 55, band / 51.0);
                spectrumCoefficients[band] = 2 * Math.Cos(2 * Math.PI * Math.Min(freq, sampleRate * .48) / sampleRate);
            }
            int tag = Marshal.ReadInt16(format), channels = Marshal.ReadInt16(format, 2), bits = Marshal.ReadInt16(format, 14), block = Marshal.ReadInt16(format, 12);
            // WAVE_FORMAT_EXTENSIBLE embeds the PCM/float subformat GUID at byte 24.
            bool floating = tag == 3 || (tag == 0xfffe && Marshal.ReadInt32(format, 24) == 3);
            if (!(floating && bits == 32) && !(bits == 16 || bits == 24 || bits == 32)) throw new NotSupportedException("Неподдерживаемый формат звука");
            Marshal.ThrowExceptionForHR(client.Initialize(0, 0x20000, 10000000, 0, format, IntPtr.Zero));
            Guid captureId = typeof(IAudioCaptureClient).GUID;
            Marshal.ThrowExceptionForHR(client.GetService(ref captureId, out object service)); capture = (IAudioCaptureClient)service;
            Marshal.ThrowExceptionForHR(client.Start()); Status?.Invoke("Системный звук подключён");
            while (!cancel.IsCancellationRequested)
            {
                Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out uint frames));
                double sum = 0, bassSum = 0, midSum = 0, highSum = 0; long count = 0;
                while (frames > 0)
                {
                    Marshal.ThrowExceptionForHR(capture.GetBuffer(out IntPtr data, out uint n, out uint flags, out _, out _));
                    try
                    {
                        if ((flags & 2) == 0 && data != IntPtr.Zero)
                        {
                            int length = checked((int)n * block); byte[] bytes = new byte[length]; Marshal.Copy(data, bytes, 0, length);
                            int stride = bits / 8;
                            for (int f = 0; f < n; f++) for (int c = 0; c < channels; c++)
                            {
                                int i = f * block + c * stride; float v;
                                if (floating) v = BitConverter.ToSingle(bytes, i);
                                else if (bits == 16) v = BitConverter.ToInt16(bytes, i) / 32768f;
                                else if (bits == 24) { int x = bytes[i] | bytes[i+1] << 8 | bytes[i+2] << 16; if ((x & 0x800000) != 0) x |= unchecked((int)0xff000000); v = x / 8388608f; }
                                else v = BitConverter.ToInt32(bytes, i) / 2147483648f;
                                if (float.IsFinite(v)) { if (c == 0) { samples[sampleCount++] = v; if (sampleCount == samples.Length) { CalculateSpectrum(); sampleCount = 0; } } sum += v * v; low += 0.018 * (v - low); mid += 0.16 * (v - mid); bassSum += low * low; midSum += (mid - low) * (mid - low); highSum += (v - mid) * (v - mid); count++; }
                            }
                        }
                    }
                    finally { capture.ReleaseBuffer(n); }
                    Marshal.ThrowExceptionForHR(capture.GetNextPacketSize(out frames));
                }
                if (count > 0) { lastPacket = DateTime.UtcNow; Level = (float)Math.Sqrt(sum / count); Bass = (float)Math.Sqrt(bassSum / count); Mid = (float)Math.Sqrt(midSum / count); High = (float)Math.Sqrt(highSum / count); } else if ((DateTime.UtcNow - lastPacket).TotalMilliseconds > 150) { Level = Bass = Mid = High = 0; Spectrum = new float[52]; sampleCount = 0; }
                MixerLevel = meter != null && meter.GetPeakValue(out float peak) >= 0 && float.IsFinite(peak) ? Math.Clamp(peak, 0, 1) : 0;
                cancel.Token.WaitHandle.WaitOne(25);
            }
        }
        catch (Exception e) { if (!cancel.IsCancellationRequested) Status?.Invoke("Звук недоступен: " + e.Message); }
        finally { MixerLevel = 0; if (meter != null) Marshal.ReleaseComObject(meter); if (client != null) { try { client.Stop(); } catch { } } if (format != IntPtr.Zero) Marshal.FreeCoTaskMem(format); if (capture != null) Marshal.ReleaseComObject(capture); if (client != null) Marshal.ReleaseComObject(client); if (device != null) Marshal.ReleaseComObject(device); if (enumerator != null) Marshal.ReleaseComObject(enumerator); }
    }
    private void CalculateSpectrum()
    {
        var result = new float[52]; int n = samples.Length;
        for (int band = 0; band < result.Length; band++)
        {
            double coefficient = spectrumCoefficients[band], a = 0, b = 0;
            for (int i = 0; i < n; i++) { double next = samples[i] * spectrumWindow[i] + coefficient * a - b; b = a; a = next; }
            result[band] = (float)(Math.Sqrt(Math.Max(0, a * a + b * b - coefficient * a * b)) * 4 / n);
        }
        Spectrum = result;
    }
    public void Dispose() { cancel.Cancel(); if (Thread.CurrentThread != thread) thread.Join(1000); }
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] private class MMDeviceEnumerator { }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IMMDeviceEnumerator { [PreserveSig] int EnumAudioEndpoints(int flow, int mask, out IntPtr devices); [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device); }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IMMDevice { [PreserveSig] int Activate(ref Guid iid, int clsctx, IntPtr activation, [MarshalAs(UnmanagedType.IUnknown)] out object result); }
    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IAudioClient
    { [PreserveSig] int Initialize(int share, uint flags, long duration, long periodicity, IntPtr format, IntPtr session); [PreserveSig] int GetBufferSize(out uint frames); [PreserveSig] int GetStreamLatency(out long latency); [PreserveSig] int GetCurrentPadding(out uint padding); [PreserveSig] int IsFormatSupported(int share, IntPtr format, out IntPtr closest); [PreserveSig] int GetMixFormat(out IntPtr format); [PreserveSig] int GetDevicePeriod(out long def, out long min); [PreserveSig] int Start(); [PreserveSig] int Stop(); [PreserveSig] int Reset(); [PreserveSig] int SetEventHandle(IntPtr handle); [PreserveSig] int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object result); }
    [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IAudioMeterInformation
    {
        [PreserveSig] int GetPeakValue(out float peak);
        [PreserveSig] int GetMeteringChannelCount(out uint channels);
        [PreserveSig] int GetChannelsPeakValues(uint channels, IntPtr peaks);
        [PreserveSig] int QueryHardwareSupport(out uint mask);
    }
    [ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IAudioCaptureClient { [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition); [PreserveSig] int ReleaseBuffer(uint frames); [PreserveSig] int GetNextPacketSize(out uint frames); }
}
