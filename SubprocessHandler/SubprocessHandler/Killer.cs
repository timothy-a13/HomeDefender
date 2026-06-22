using System.Data;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Data.SqlClient;

namespace SubprocessHandler;

public class Killer
{
    private readonly ProcessOptions options;

    public Killer()
    {
        options = ProcessOptions.FromEnvironment();
    }

    public void DelSubprocess(int cameraId)
    {
        (int? coreProcessId, int? saveProcessId) = GetProcessIds(cameraId);

        StopProcess("AI", coreProcessId);
        StopProcess("HLS", saveProcessId);
        ClearProcessIds(cameraId);
    }

    private (int? CoreProcessId, int? SaveProcessId) GetProcessIds(int cameraId)
    {
        using SqlConnection connection = new(options.DatabaseConnectionString);
        using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "SELECT core_pid, save_pid "
            + "FROM process_info "
            + "WHERE cam_id = @cameraId";
        command.Parameters.Add("@cameraId", SqlDbType.Int).Value = cameraId;

        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new InvalidOperationException(
                $"No process_info row exists for camera {cameraId}.");
        }

        int? coreProcessId =
            reader.IsDBNull(reader.GetOrdinal("core_pid"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("core_pid"));
        int? saveProcessId =
            reader.IsDBNull(reader.GetOrdinal("save_pid"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("save_pid"));

        return (coreProcessId, saveProcessId);
    }

    private void ClearProcessIds(int cameraId)
    {
        using SqlConnection connection = new(options.DatabaseConnectionString);
        using SqlCommand command = connection.CreateCommand();
        command.CommandText =
            "UPDATE process_info "
            + "SET core_pid = NULL, save_pid = NULL "
            + "WHERE cam_id = @cameraId";
        command.Parameters.Add("@cameraId", SqlDbType.Int).Value = cameraId;

        connection.Open();
        command.ExecuteNonQuery();
    }

    private static void StopProcess(string processName, int? processId)
    {
        if (processId is null)
        {
            return;
        }

        try
        {
            if (processId.Value <= 0)
            {
                return;
            }

            using Process process = Process.GetProcessById(processId.Value);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (ArgumentException)
        {
            Console.WriteLine(
                $"{processName} process {processId.Value} has already exited.");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine(
                $"{processName} process {processId.Value} is no longer available.");
        }
        catch (Win32Exception exception)
        {
            Console.WriteLine(
                $"Unable to stop {processName} process {processId.Value}: "
                + exception.Message);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine(
                $"Unable to stop {processName} process {processId.Value}: "
                + exception.Message);
        }
    }
}
