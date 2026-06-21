using System.Data.SqlClient;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace BlazorApp1.Data
{
    public class NatificationHubConn : IAsyncDisposable
    {
        private static readonly SqlConnection sql =
            new(AppEnvironment.DatabaseConnectionString);
        private static readonly SemaphoreSlim semaphore = new(1, 1);

        private bool _isInitialized;
        private readonly NamedPipeServerStream _pipe;
        private readonly Guid _guid;
        private readonly SemaphoreSlim _semaphoreOfInit = new(1, 1);
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly bool _debug;

        public delegate void CommandEventHandler(object sender, CommandEventArgs e);
        public event CommandEventHandler? OnCommandChanged;

        [SupportedOSPlatform("windows")]
        public NatificationHubConn(IniHandler ini)
        {
            _isInitialized = false;
            _guid = Guid.NewGuid();
            _debug = Convert.ToBoolean(ini["DEBUG"]["Enable"]);

            PipeSecurity pipe_security = new();
            pipe_security.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            _pipe = NamedPipeServerStreamAcl.Create(
                _guid.ToString(), PipeDirection.In, -1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                4096, 4096, pipe_security
            );

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(() =>
            {
                _pipe.Close();
                _pipe.Dispose();
                if (_debug)
                    using (StreamWriter sw = new("log_file.log", true))
                        sw.WriteLine("Dispose the pipeline");
            });

            Console.WriteLine("Debug: {0}", _debug);
        }

        public async Task InitializeAsync(List<int> cam_ids)
        {
            await _semaphoreOfInit.WaitAsync();
            if (!_isInitialized)
            {
                if (cam_ids.Count != 0)
                {
                    await semaphore.WaitAsync();
                    await sql.OpenAsync();
                    using (SqlCommand cmd = sql.CreateCommand())
                    {
                        cmd.CommandText = string.Empty;
                        foreach (int cam_id in cam_ids)
                        {
                            cmd.CommandText += $"INSERT INTO IPC_table VALUES({cam_id}, '{_guid}');\n";
                        }
                        await cmd.ExecuteNonQueryAsync();
                    }
                    await sql.CloseAsync();
                    semaphore.Release();
                }

                if (_debug)
                    using (StreamWriter sw = new("ConnectionId.txt", true))
                        sw.WriteLine(_guid.ToString());

                WaitForNotificationFromOtherProcess();
                _isInitialized = true;
            }
            _semaphoreOfInit.Release();
        }

        private async void WaitForNotificationFromOtherProcess()
        {
            CancellationToken token = _cancellationTokenSource.Token;

            while (true)
            {
                try
                {
                    await _pipe.WaitForConnectionAsync(token);
                    StringStream pipeStream = new(_pipe);
                    while (true)
                    {
                        string msg = await pipeStream.ReadAsync(token);
                        if (!_pipe.IsConnected)
                        {
                            if (_debug)
                                using (StreamWriter sw = new("log_file.log", true))
                                    sw.WriteLine("Client is close, pipe is not connected.");
                            _pipe.Disconnect(); break;
                        }
                        else if (msg.Length == 0)
                            throw new Exception("There is no command, maybe pipe is broken.");

                        string[] split = msg.Split(';', 3, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        Array.Resize(ref split, 3);
                        OnCommandChanged?.Invoke(this, new(split));
                    }
                }
                catch (OperationCanceledException ex)
                {
                    if (_debug)
                        using (StreamWriter sw = new("log_file.log", true))
                            sw.WriteLine("Cancel Exception: {0}", ex.Message);
                    return;
                }
                catch (Exception ex) when (ex.Message == "There is no command, maybe pipe is broken.")
                {
                    if (_debug)
                        using (StreamWriter sw = new("log_file.log", true))
                            sw.WriteLine(ex.Message);
                    _pipe.Disconnect();
                }
                catch (Exception ex)
                {
                    if (_debug)
                        using (StreamWriter sw = new("log_file.log", true))
                            sw.WriteLine("Error at HotificationHub: {0}", ex.ToString());
                }
            }
        }

        public async Task AddCamera(int cam_id)
        {
            await semaphore.WaitAsync();
            await sql.OpenAsync();
            using (SqlCommand cmd = sql.CreateCommand())
            {
                cmd.CommandText = $"INSERT INTO IPC_table VALUES({cam_id}, '{_guid}');\n";
                await cmd.ExecuteNonQueryAsync();
            }
            await sql.CloseAsync();
            semaphore.Release();
        }

        public async Task RemoveCamera(int cam_id)
        {
            await semaphore.WaitAsync();
            await sql.OpenAsync();
            using (SqlCommand cmd = sql.CreateCommand())
            {
                cmd.CommandText = $"DELETE FROM IPC_table WHERE cam_id = {cam_id} and ipc_id = '{_guid}';\n";
                await cmd.ExecuteNonQueryAsync();
            }
            await sql.CloseAsync();
            semaphore.Release();
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await semaphore.WaitAsync();
                await sql.OpenAsync();
                using (SqlCommand cmd = sql.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM IPC_table WHERE ipc_id = '{_guid}'";
                    await cmd.ExecuteNonQueryAsync();
                }
                await sql.CloseAsync();
                semaphore.Release();

                _cancellationTokenSource.Cancel();

                if (_debug)
                {
                    using (StreamWriter sw = new("ConnectionId.txt", true))
                        sw.WriteLine("Kill: {0}", _guid.ToString());
                    using (StreamWriter sw = new("ConnectionId.log", true))
                        sw.WriteLine("Close");
                }
            }
            catch (Exception ex)
            {
                if (_debug)
                    using (StreamWriter sw = new("log_file.log", true))
                        sw.WriteLine("Error at DisposeAsync of NatificationHubConn: {0}", ex.Message);
            }

            GC.SuppressFinalize(this);
            Console.WriteLine("Close.");
        }
    }

    public class CommandEventArgs : EventArgs
    {
        public string Command { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public CommandEventArgs(string[] cmd)
        {
            Command = cmd[0];
            Title = cmd[1];
            Message = cmd[2];
        }
    }
}
