using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Data.SqlClient;

namespace SubprocessHandler;

public class Runner
{
    private readonly ProcessOptions options;

    public Runner()
    {
        options = ProcessOptions.FromEnvironment();
    }

    public void RunSubprocess(int cameraId)
    {
        string cameraKey = GetCameraKey(cameraId);
        ProcessOptions.ValidateCameraKey(cameraKey);

        string streamUrl = options.GetStreamUrl(cameraKey);
        string cameraVideoDirectory = Path.Combine(options.VideoRoot, cameraKey);
        Directory.CreateDirectory(cameraVideoDirectory);

        using Process coreProcess = Process.Start(CreateCoreProcess(cameraId, cameraKey, streamUrl))
            ?? throw new InvalidOperationException("Failed to start the AI analysis process.");

        Process? saveProcess = null;
        try
        {
            Thread.Sleep(options.ProcessStartDelayMs);
            saveProcess = Process.Start(CreateHlsProcess(streamUrl, cameraVideoDirectory))
                ?? throw new InvalidOperationException("Failed to start the HLS storage process.");

            UpdateProcessIds(cameraId, coreProcess.Id, saveProcess.Id);
            Console.WriteLine(
                $"Camera {cameraId}: AI PID {coreProcess.Id}, HLS PID {saveProcess.Id}.");
        }
        catch
        {
            StopProcess(coreProcess);
            if (saveProcess is not null)
            {
                StopProcess(saveProcess);
            }

            throw;
        }
        finally
        {
            saveProcess?.Dispose();
        }
    }

    private string GetCameraKey(int cameraId)
    {
        using SqlConnection connection = new(options.DatabaseConnectionString);
        using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT g_key FROM camera_info WHERE cam_id = @cameraId";
        command.Parameters.Add("@cameraId", SqlDbType.Int).Value = cameraId;

        connection.Open();
        return command.ExecuteScalar() as string
            ?? throw new InvalidOperationException($"Camera {cameraId} was not found.");
    }

    private ProcessStartInfo CreateCoreProcess(
        int cameraId,
        string cameraKey,
        string streamUrl)
    {
        ProcessStartInfo processInfo = new()
        {
            FileName = options.PythonExecutable,
            WorkingDirectory = options.CorePath,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        processInfo.ArgumentList.Add("core.py");
        processInfo.ArgumentList.Add("--source");
        processInfo.ArgumentList.Add(streamUrl);
        processInfo.ArgumentList.Add("--max-det");
        processInfo.ArgumentList.Add("10");
        processInfo.ArgumentList.Add("--cam-id");
        processInfo.ArgumentList.Add(cameraId.ToString());
        processInfo.ArgumentList.Add("--g-key");
        processInfo.ArgumentList.Add(cameraKey);
        processInfo.ArgumentList.Add("--tracking-method");
        processInfo.ArgumentList.Add("ocsort");

        return processInfo;
    }

    private ProcessStartInfo CreateHlsProcess(
        string streamUrl,
        string cameraVideoDirectory)
    {
        string segmentTemplate =
            Path.Combine(cameraVideoDirectory, "%Y-%m-%d-%H-%M-%S.ts");
        string playlistPath = Path.Combine(cameraVideoDirectory, "index.m3u8");

        ProcessStartInfo processInfo = new()
        {
            FileName = options.FfmpegExecutable,
            WorkingDirectory = options.VideoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        string[] arguments =
        {
            "-r", "24",
            "-i", streamUrl,
            "-c", "copy",
            "-an",
            "-hls_segment_type", "mpegts",
            "-hls_time", "2",
            "-hls_list_size", "43200",
            "-hls_delete_threshold", "60",
            "-hls_flags", "delete_segments+split_by_time+append_list",
            "-strftime", "1",
            "-hls_segment_filename", segmentTemplate,
            "-y", playlistPath,
        };

        foreach (string argument in arguments)
        {
            processInfo.ArgumentList.Add(argument);
        }

        return processInfo;
    }

    private void UpdateProcessIds(int cameraId, int coreProcessId, int saveProcessId)
    {
        using SqlConnection connection = new(options.DatabaseConnectionString);
        using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "UPDATE process_info "
            + "SET core_pid = @coreProcessId, save_pid = @saveProcessId "
            + "WHERE cam_id = @cameraId";
        command.Parameters.Add("@coreProcessId", SqlDbType.Int).Value = coreProcessId;
        command.Parameters.Add("@saveProcessId", SqlDbType.Int).Value = saveProcessId;
        command.Parameters.Add("@cameraId", SqlDbType.Int).Value = cameraId;

        connection.Open();
        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException(
                $"No process_info row exists for camera {cameraId}.");
        }
    }

    private static void StopProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and the kill request.
        }
        catch (Win32Exception)
        {
            // Cleanup must not hide the original process-start failure.
        }
        catch (NotSupportedException)
        {
            // Process termination is not supported on the current platform.
        }
    }
}
