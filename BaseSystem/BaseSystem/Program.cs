//using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Data.SqlClient;
using static System.Console;

namespace BaseSystem
{
    internal class Program
    {
        //private static bool IsNotNull([NotNullWhen(true)] object? obj) => obj != null;
        static void Main(string[] args)
        {
            string host = Environment.GetEnvironmentVariable("HOMEDEFENDER_REGISTRATION_HOST") ?? "0.0.0.0";
            int port = int.TryParse(
                Environment.GetEnvironmentVariable("HOMEDEFENDER_REGISTRATION_PORT"),
                out int configuredPort)
                ? configuredPort
                : 25361;

            TCPServer server = new(host, port);
            server.Listen(20);
            server.AcceptAsync().Wait();
        }
    }

    public class TCPServer
    {
        private readonly IPEndPoint endPoint;
        private readonly TcpListener listener;
        private readonly SqlRegister sql;
        private readonly Socket?[] socketsPool;
        private const int POOL_SIZE = 20;
        public TCPServer(string ip, int port)
        {
            endPoint = new(IPAddress.Parse(ip), port);
            listener = new(endPoint);
            socketsPool = new Socket?[POOL_SIZE];
            sql = new SqlRegister();
        }
        public void Listen(int backlog) {
            listener.Start(backlog);
        }
        public async Task AcceptAsync()
        {
            while (true)
            {
                Socket tmpSocket = await listener.AcceptSocketAsync();
                WriteLine(tmpSocket.RemoteEndPoint?.ToString());
                int ind;
                if ((ind = FindEmptyIndex()) != -1)
                {
                    WriteLine("Pool index: {0}", ind);
                    socketsPool[ind] = tmpSocket;
                    _ = GetData(tmpSocket, ind);
                }
            }
        }
        private async Task GetData(Socket socket, int ind)
        {
            try
            {
                int r = 0;
                byte[] buf = new byte[1024];
                Func<int, Task<string>> GetMsg = async (len) =>
                {
                    byte[] tmp = new byte[512];
                    DateTime bgn = DateTime.Now;
                    TimeSpan limit_cost = new(0, 0, 0, 1);

                    while (r < len && DateTime.Now - bgn < limit_cost)
                    {
                        int l = await socket.ReceiveAsync(tmp, SocketFlags.None, new CancellationTokenSource(500).Token);
                        if (l == 0) throw new Exception("There are some bad men.");
                        for (int i = 0; i < l; i++) buf[r + i] = tmp[i];
                        r += l;
                    }
                    if (r < len) throw new Exception("There are some bad men.");

                    string result = Encoding.UTF8.GetString(buf, 1, len - 1);
                    buf = buf.Skip(len).ToArray();
                    Array.Resize(ref buf, 1024);
                    r -= len;
                    return result;
                };

                string? id = null, key = null;
                while (id is null || key is null)
                {
                    if (r == 0) r += await socket.ReceiveAsync(buf, SocketFlags.None, new CancellationTokenSource(500).Token);
                    switch (buf.FirstOrDefault((byte)255))
                    {
                        case 0: id  = await GetMsg(6);  break;
                        case 1: key = await GetMsg(16); break;
                        default: throw new Exception("There are some bad men.");
                    }
                }
                
                if (!int.TryParse(id, out _))
                    throw new Exception("There are some bad men.");
                
                if (!await sql.AppendToSql(socket, id!, key!))
                    WriteLine("Sql failed...");

                // Wait client to close socket.
                //socket.Receive(buf, SocketFlags.None);
                socket.Close();
            }
            catch (Exception ex)
            {
                WriteLine("Exception: {0}", ex.Message);
            }
            finally
            {
                socketsPool[ind]!.Dispose();
                socketsPool[ind] = null;
                WriteLine("Finish connection.");
            }
        }
        private int FindEmptyIndex()
        {
            for (int i = 0; i < socketsPool.Length; i++)
                if (socketsPool[i] == null || !socketsPool[i]!.Connected)
                    return i;
            return -1;
        }
    }

    public class SqlRegister
    {
        private readonly SqlConnection conn;
        private readonly string storageRoot;

        public SqlRegister()
        {
            string connectionString =
                Environment.GetEnvironmentVariable("HOMEDEFENDER_DB_CONNECTION_STRING")
                ?? "Server=localhost;Database=detection_sys_db;Integrated Security=True;TrustServerCertificate=True;";
            conn = new SqlConnection(connectionString);
            storageRoot =
                Environment.GetEnvironmentVariable("HOMEDEFENDER_VIDEO_ROOT")
                ?? Path.Combine(AppContext.BaseDirectory, "live");
        }
        public async Task<bool> AppendToSql(object sender, string id, string key)
        {
            if (conn.State == System.Data.ConnectionState.Closed) await conn.OpenAsync();
            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT count(cam_id) FROM camera_info WHERE cam_id = {id};";

            string ip  = (sender as Socket)!.RemoteEndPoint!.ToString()!.Split(':')[0];
            WriteLine(ip);
            string path = Path.Combine(storageRoot, key);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path + "/dangerous");
				Directory.CreateDirectory(path + "/record");
			}
            if (!Convert.ToBoolean(await cmd.ExecuteScalarAsync()))
            {
                cmd.CommandText  = $"INSERT INTO camera_info  VALUES ({id}, '{key}', 0, '{ip}');\n";
                cmd.CommandText += $"INSERT INTO process_info VALUES ({id}, NULL, NULL);\n";
                cmd.CommandText += $"INSERT INTO storage_info VALUES ({id}, '{path}');";
            }
            else
            {
                cmd.CommandText = $"SELECT is_conn FROM camera_info WHERE cam_id = {id};";
                if (Convert.ToBoolean(await cmd.ExecuteScalarAsync()))
                    throw new Exception("仍是連線狀態");
                cmd.CommandText  = $"UPDATE camera_info  SET ip = '{ip}' WHERE cam_id = {id};\n";
                //cmd.CommandText += $"UPDATE process_info SET core_pid = {corePid}, save_pid = {savePid} WHERE cam_id = {id};\n";
                cmd.CommandText += $"UPDATE storage_info SET path = '{path}' WHERE cam_id = {id};";
            }
            await cmd.ExecuteNonQueryAsync();
            Task<int>? t = (sender as Socket)?.SendAsync(Encoding.UTF8.GetBytes("suc"), SocketFlags.None);
            return t is not null && Convert.ToBoolean(t.Result);
        }
    }
}
