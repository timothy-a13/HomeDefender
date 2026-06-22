using System.Globalization;
using System.Text.RegularExpressions;

namespace SubprocessHandler;

internal sealed class ProcessOptions
{
    private static readonly Regex CameraKeyPattern =
        new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);

    private ProcessOptions(
        string databaseConnectionString,
        string corePath,
        string videoRoot,
        string pythonExecutable,
        string ffmpegExecutable,
        string rtmpBaseUrl,
        int processStartDelayMs)
    {
        DatabaseConnectionString = databaseConnectionString;
        CorePath = corePath;
        VideoRoot = videoRoot;
        PythonExecutable = pythonExecutable;
        FfmpegExecutable = ffmpegExecutable;
        RtmpBaseUrl = rtmpBaseUrl.TrimEnd('/');
        ProcessStartDelayMs = processStartDelayMs;
    }

    public string DatabaseConnectionString { get; }
    public string CorePath { get; }
    public string VideoRoot { get; }
    public string PythonExecutable { get; }
    public string FfmpegExecutable { get; }
    public string RtmpBaseUrl { get; }
    public int ProcessStartDelayMs { get; }

    public static ProcessOptions FromEnvironment()
    {
        string currentDirectory = Directory.GetCurrentDirectory();
        string corePath = Path.GetFullPath(
            Environment.GetEnvironmentVariable("HOMEDEFENDER_CORE_PATH")
            ?? Path.Combine(currentDirectory, "Core"));
        string videoRoot = Path.GetFullPath(
            Environment.GetEnvironmentVariable("HOMEDEFENDER_VIDEO_ROOT")
            ?? Path.Combine(currentDirectory, "live"));

        if (!Directory.Exists(corePath))
        {
            throw new DirectoryNotFoundException(
                $"HomeDefender Core directory was not found: {corePath}");
        }

        Directory.CreateDirectory(videoRoot);

        return new ProcessOptions(
            Environment.GetEnvironmentVariable("HOMEDEFENDER_DB_CONNECTION_STRING")
                ?? "Server=localhost;Database=detection_sys_db;Integrated Security=True;TrustServerCertificate=True;",
            corePath,
            videoRoot,
            GetPythonExecutable(),
            Environment.GetEnvironmentVariable("HOMEDEFENDER_FFMPEG_EXECUTABLE")
                ?? "ffmpeg",
            Environment.GetEnvironmentVariable("HOMEDEFENDER_RTMP_BASE_URL")
                ?? "rtmp://127.0.0.1/live",
            GetStartDelay());
    }

    public string GetStreamUrl(string cameraKey)
    {
        ValidateCameraKey(cameraKey);
        return $"{RtmpBaseUrl}/{cameraKey}";
    }

    public static void ValidateCameraKey(string cameraKey)
    {
        if (string.IsNullOrWhiteSpace(cameraKey)
            || !CameraKeyPattern.IsMatch(cameraKey))
        {
            throw new InvalidOperationException(
                "Camera keys may contain only letters, numbers, underscores, and hyphens.");
        }
    }

    private static string GetPythonExecutable()
    {
        string? explicitExecutable =
            Environment.GetEnvironmentVariable("HOMEDEFENDER_PYTHON_EXECUTABLE");
        if (!string.IsNullOrWhiteSpace(explicitExecutable))
        {
            return explicitExecutable;
        }

        string? pythonHome =
            Environment.GetEnvironmentVariable("HOMEDEFENDER_PYTHON_HOME");
        if (string.IsNullOrWhiteSpace(pythonHome))
        {
            return "python";
        }

        return Path.Combine(
            pythonHome,
            OperatingSystem.IsWindows() ? "python.exe" : "bin/python");
    }

    private static int GetStartDelay()
    {
        const int defaultDelayMs = 7000;
        string? value =
            Environment.GetEnvironmentVariable("HOMEDEFENDER_PROCESS_START_DELAY_MS");

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultDelayMs;
        }

        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int delay)
            || delay < 0)
        {
            throw new InvalidOperationException(
                "HOMEDEFENDER_PROCESS_START_DELAY_MS must be a non-negative integer.");
        }

        return delay;
    }
}
