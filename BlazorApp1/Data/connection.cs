using BlazorApp1.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.SqlTypes;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.Xml;
using System.Text.Json.Nodes;

namespace BlazorApp1.Data
{
    public class Connection
    {

        public string GetConnectionString()
        {
            return AppEnvironment.DatabaseConnectionString;
        }

        public int AddData(string input_Account, string input_Password)
        {
            using (SqlConnection sqlcn = new SqlConnection())
            {
                sqlcn.ConnectionString = GetConnectionString();
                sqlcn.Open();

                SqlCommand countCmd = new SqlCommand("SELECT COUNT(*) FROM [user_info]", sqlcn);
                int userCount = Convert.ToInt32(countCmd.ExecuteScalar());

                SqlCommand sqlcmd = new SqlCommand("SELECT * FROM [user_info] WHERE User_name = @UserName", sqlcn);
                sqlcmd.Parameters.AddWithValue("@UserName", input_Account);
                SqlDataReader sqldr = sqlcmd.ExecuteReader();

                if (sqldr.HasRows)
                {
                    sqldr.Close();
                    sqlcmd.Dispose();
                    return 0;
                }

                sqldr.Close();
                sqlcmd.Dispose();
                SqlCommand sqlcreate = new SqlCommand("INSERT INTO [user_info] (User_id, User_name, User_password, User_camara_count) VALUES (@UserId, @Username, @Password, @CamaraCount)", sqlcn);
                sqlcreate.Parameters.AddWithValue("@UserId", userCount + 1);
                sqlcreate.Parameters.AddWithValue("@Username", input_Account);
                sqlcreate.Parameters.AddWithValue("@Password", input_Password);
                sqlcreate.Parameters.AddWithValue("@CamaraCount", 1);
                sqlcreate.ExecuteNonQuery();
                return 1;
            }
        }

        public (int, string, bool, string) Find_cam_id()
        {

            string connectionString = GetConnectionString();
            string query = "SELECT * FROM camera_info";

            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand command = new SqlCommand(query, connection);


            //command.Parameters.AddWithValue("@table_name", "camera_info");

            connection.Open();

            SqlDataReader reader = command.ExecuteReader();

            int cam_id = -1;
            String g_key = "@";
            bool is_conn = false;
            String ip = "0.0.0.0";
            // 讀取結果集中的資料
            while (reader.Read())
            {
                cam_id = (int)reader["cam_id"];
                g_key = reader["g_key"]!.ToString()!;
                is_conn = (bool)reader["is_conn"];
                ip = reader["ip"].ToString()!;

            }
            connection.Close();
            return (cam_id, g_key, is_conn, ip);
        }


        public bool Login(string input_email, string input_password)
        {
            JsonNode user_node = new JsonObject();
            string connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);

            SqlCommand command = new SqlCommand("SELECT * FROM user_info WHERE email = @Email AND password = @Password", connection);
            command.Parameters.AddWithValue("@Email", input_email);
            command.Parameters.AddWithValue("@Password", input_password);

            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            /*
            int uid = -1;
            string user_id = "@";
            string email = "@";
            string password = "@";
            string nickname = "@";
            string gender = "@";
            */
            if (reader.HasRows)
            {
                // 讀取結果集中的資料
                reader.Read();
                int uid = (int)reader["uid"];
                user_node["uid"] = (int)reader["uid"];
                user_node["user_id"] = reader["user_id"]!.ToString()!;
                user_node["email "] = reader["email"]!.ToString()!;
                user_node["password"] = reader["password"]!.ToString()!;
                user_node["nickname"] = reader["nickname"]!.ToString()!;
                user_node["gender"] = reader["gender"].ToString()!;
                user_node["cameras"] = GetAllCam(uid);

                Console.Write(user_node.ToString());
                connection.Close();
                return true;
            }

            connection.Close();
            return false;
        }


        public bool isExist(string input_email)
        {
            bool b;
            string connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);
            SqlCommand command = new SqlCommand("SELECT * FROM user_info WHERE email = @Email", connection);
            command.Parameters.AddWithValue("@Email", input_email);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            if (reader.HasRows)
                b = true;
            else
                b = false;
            connection.Close();
            return b;

        }


        public bool Register(string input_user_id, string input_email, string input_password, string input_nickname, int input_gender)
        {
            if (!isExist(input_email))
            {
                string connectionString = GetConnectionString();
                SqlConnection connection = new SqlConnection(connectionString);

                SqlCommand command = new SqlCommand("INSERT INTO [user_info] (user_id, email, password, nickname, gender) VALUES (@user_id, @email, @password, @nickname, @gender)", connection);
                command.Parameters.AddWithValue("@user_id", input_user_id);
                command.Parameters.AddWithValue("@email", input_email);
                command.Parameters.AddWithValue("@password", input_password);
                command.Parameters.AddWithValue("@nickname", input_nickname);
                command.Parameters.AddWithValue("@gender", input_gender);

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
                return true;
            }

            return false;
           

        }


        public JsonNode GetAllCam(int uid)
        {
            JsonNode all_cam_json = new JsonObject();
            string connectionString = GetConnectionString();
            SqlConnection connection = new SqlConnection(connectionString);

            SqlCommand command = new SqlCommand("SELECT camera_info.cam_id,camera_info.is_conn, camera_info.g_key, cam_danger.time\r\nFROM user_cam\r\nJOIN camera_info ON user_cam.cam_id = camera_info.cam_id\r\nJOIN cam_danger ON user_cam.cam_id = cam_danger.cam_id\r\nWHERE uid = @uid;", connection);
            command.Parameters.AddWithValue("@uid", uid);
            if (connection.State == ConnectionState.Closed) connection.Open();
            SqlDataReader reader = command.ExecuteReader();

            if (reader.HasRows)
            {
                string pre_cam_id = "";
                // 讀取結果集中的資料
                while (reader.Read())
                {
                    string cam_id = reader["cam_id"]!.ToString()!;
                    if (cam_id != pre_cam_id)
                    {
                        all_cam_json[cam_id] = new JsonObject() { };
                        all_cam_json[cam_id].AsObject()["danger"] = new JsonArray();
                    }

                    //cam_json["g_key"] = reader["g_key"]!.ToString()!;
                    all_cam_json[cam_id].AsObject()["g_key"] = reader["g_key"]!.ToString()!;
                    //cam_json["is_conn"] = reader["is_conn"]!.ToString()!;
                    all_cam_json[cam_id].AsObject()["is_conn"] = reader["is_conn"]!.ToString()!;
                    all_cam_json[cam_id].AsObject()["danger"].AsArray().Add(reader["time"]!.ToString()!);
                    //all_cam_json[cam_id].AsObject()["danger_num"] = all_cam_json[cam_id].AsObject()["danger"].AsArray().Count();

                    pre_cam_id = cam_id;
                }


            }
            connection.Close();
            return all_cam_json;
        }

        public bool AddCam()
        {
            //先放session 再放資料庫
            return false;
        }
    }
}



