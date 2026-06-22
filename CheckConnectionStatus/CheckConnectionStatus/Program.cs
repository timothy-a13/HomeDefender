using System.Text.Json.Nodes;
using System.Data;
using Microsoft.Data.SqlClient;
using SubprocessHandler;

namespace RetrieveJson
{
    class Program
    {
        private static readonly string ConnectionString =
            Environment.GetEnvironmentVariable("HOMEDEFENDER_DB_CONNECTION_STRING")
            ?? "Server=localhost;Database=detection_sys_db;Integrated Security=True;TrustServerCertificate=True;";

        static void Main(string[] args)
        {
            // 設定 API 網址
            string url =
                Environment.GetEnvironmentVariable("HOMEDEFENDER_SRS_STREAMS_API")
                ?? "http://127.0.0.1:1985/api/v1/streams";
            HttpClient client = new();

            HashSet<string> pre_state = new();
            JsonNode? pre_json = null;
            int pre_stream_num = 0;
            Killer killer = new();
            Runner runner = new();

            while (true)
            {
                Thread.Sleep(250);
                HttpResponseMessage response = client.GetAsync(url).Result;
                string content = response.Content.ReadAsStringAsync().Result;
                JsonNode cur_json = JsonNode.Parse(content)!;
                int cur_stream_num = cur_json["streams"]!.AsArray().Count;
                foreach (JsonNode? stream in cur_json["streams"]!.AsArray())
                {
                    stream!["name"] = stream!["name"]!.ToString().Split('.')[0];
                }

				if (cur_stream_num == 0 && pre_stream_num == 0) Thread.Sleep(250);
                else
                {
                    //遍歷cur_stream
                    HashSet<string> cur_state = new();
                    List<JsonNode> exits = new();
                    for (int i = 0; i < cur_stream_num; i++)
                    {
                        if ((bool)cur_json["streams"]![i]!["publish"]!["active"]! != false)
                        {
                            exits.Add(cur_json["streams"]![i]!);
                            cur_state.Add(cur_json["streams"]![i]!["name"]!.ToString());
                        }
                    }
                    cur_json["streams"]!.AsArray().Clear();
                    foreach (JsonNode node in exits)
                    {
                        cur_json["streams"]!.AsArray().Add(node);
                    }
                    cur_stream_num = cur_json["streams"]!.AsArray().Count;
                    //找新增的stream
                    for (int i = 0; i < cur_stream_num; i++)
                    {
                        if (!pre_state.Contains(cur_json["streams"]![i]!["name"]!.ToString()))
                        {
                            string streamName =
                                cur_json["streams"]![i]!["name"]!.ToString();
                            Console.WriteLine(streamName + " has been ADD");
                            int cam_id = FindCameraId(streamName);
                            if (cam_id != -1)
                            {
                                runner.RunSubprocess(cam_id);
                            }
                            UpdateConnectionState(streamName, true);
                        }
                    }
                    //找刪除的stream
                    if (pre_json?["streams"] is JsonArray previousStreams)
                    {
                        foreach (JsonNode? previousStream in previousStreams)
                        {
                            string? streamName =
                                previousStream?["name"]?.ToString();
                            if (streamName is null || cur_state.Contains(streamName))
                            {
                                continue;
                            }

                            Console.WriteLine(streamName + " has been DEL");
                            int cam_id = FindCameraId(streamName);
                            if (cam_id != -1)
                            {
                                killer.DelSubprocess(cam_id);
                            }
                            UpdateConnectionState(streamName, false);
                        }
                    }
                    //新舊cur_stream交接
                    pre_json = cur_json;
                    //新舊cur_state交接
                    pre_state = cur_state;
                    //新舊cur_stream_num交接
                    pre_stream_num = cur_stream_num;
                }
            }
        }
        static void UpdateConnectionState(string name, bool isConnected)
        {
            const string query =
                "UPDATE camera_info SET is_conn = @isConnected WHERE g_key = @name;";

            using SqlConnection connection = new(ConnectionString);
            using SqlCommand command = new(query, connection);
            command.Parameters.Add("@isConnected", SqlDbType.Bit).Value = isConnected;
            command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = name;

            connection.Open();
            command.ExecuteNonQuery();
        }

        static int FindCameraId(string name)
        {
            const string query =
                "SELECT cam_id FROM camera_info WHERE g_key = @name;";

            using SqlConnection connection = new(ConnectionString);
            using SqlCommand command = new(query, connection);
            command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = name;

            connection.Open();
            object? cameraId = command.ExecuteScalar();
            return cameraId is int value ? value : -1;
        }
    }
}
