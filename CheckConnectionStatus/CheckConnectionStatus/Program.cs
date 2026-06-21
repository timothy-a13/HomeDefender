using System.Text.Json.Nodes;
using System.Data.SqlClient;
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
            string command_str;

            Killer killer = new();
            Runner runner = new();

            while (true)
            {
                Thread.Sleep(250);
                HttpResponseMessage response = client.GetAsync(url).Result;
                string content = response.Content.ReadAsStringAsync().Result;
                JsonNode cur_json = JsonNode.Parse(content)!;
                int cur_stream_num = cur_json["streams"]!.AsArray().Count;
                command_str = "";

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
                            command_str += Add_Query(cur_json["streams"]![i]!["name"]!.ToString(), 1);
                            Console.WriteLine(cur_json["streams"]![i]!["name"]!.ToString() + " has been ADD");
                            int cam_id = Find_cam_id(cur_json["streams"]![i]!["name"]!.ToString());
                            if (cam_id != -1)
                                runner.RunSubprocess(cam_id);
                        }
                    }
                    //找刪除的stream
                    for (int i = 0; i < pre_stream_num; i++)
                    {
                        if (!cur_state.Contains(pre_json["streams"][i]["name"].ToString()))
                        {
                            command_str += Add_Query(pre_json["streams"][i]["name"].ToString(), 0);
                            Console.WriteLine(pre_json["streams"][i]["name"].ToString() + " has been DEL");
                            int cam_id = Find_cam_id(pre_json["streams"][i]["name"].ToString());
                            if (cam_id != -1)
                                killer.DelSubprocess(cam_id);
                        }
                    }
                    //SQL 指令執行
                    if (command_str != "")
                    {
                        using (SqlConnection connection = new SqlConnection(ConnectionString))
                        {
                            using (SqlCommand command = new SqlCommand(command_str, connection))
                            {
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                            }
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
        static string Add_Query(string name, int b)
        {
            return " UPDATE camera_info SET is_conn =" + b.ToString() + " WHERE g_key ='" + name + "';";
        }

        static int Find_cam_id(string name)
        {
            int cam_id = -1;
            string query = "SELECT * FROM camera_info WHERE g_key = @name;";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // 讀取結果集中的資料
                        while (reader.Read())
                        {
                            cam_id = (int)reader["cam_id"];
                        }
                    }

                    connection.Close();
                }
            }

            return cam_id;
        }
    }
}
