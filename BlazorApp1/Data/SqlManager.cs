using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BlazorApp1.Data
{
    public class SqlManager
    {
        private readonly SqlConnection _sql;
        private readonly object _lock = new();
        public SqlManager()
        {
            _sql = new(AppEnvironment.DatabaseConnectionString);
        }

        public bool User_Verify(string input_email, string input_password)
        {
            bool comparingResults = false;
            using (SqlCommand command = new("SELECT * FROM user_info WHERE email = @Email ", _sql))
            {
                command.Parameters.AddWithValue("@Email", input_email);

                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            string SaltString = reader["user_id"].ToString()!;
                            byte[] savedSalt = Encoding.UTF8.GetBytes(SaltString);
                            string SavePassword = reader["password"]!.ToString()!;
                            string hashedToVerify = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                                password: input_password,
                                salt: savedSalt,
                                prf: KeyDerivationPrf.HMACSHA256,
                                iterationCount: 100000,
                                numBytesRequested: 256 / 8));
                            Console.WriteLine(hashedToVerify);
                            Console.WriteLine(SavePassword);
                            if (hashedToVerify == SavePassword)
                                comparingResults = true;
                        }
                    }
                    _sql.Close();
                }
            }
            return comparingResults;
        }

        public JsonNode Get_User_Info(string input_email)
        {
            JsonNode user_node = new JsonObject();
            int uid = 0;
            using (SqlCommand command = new("SELECT * FROM user_info WHERE email = @Email", _sql))
            {
                command.Parameters.AddWithValue("@Email", input_email);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        reader.Read();
                        uid = (int)reader["uid"];
                        user_node["uid"] = (int)reader["uid"];
                        user_node["score"] = (int)reader["score"];
                        user_node["user_id"] = reader["user_id"]!.ToString()!;
                        user_node["email"] = reader["email"]!.ToString()!;
                        user_node["nickname"] = reader["nickname"]!.ToString()!;
                        user_node["gender"] = reader["gender"].ToString()!;
                        user_node["mode"] = (bool) reader["mode"]!;
                        user_node["rule"] = new JsonArray();
                        if ((bool)reader["wander"])
                            user_node["rule"]!.AsArray().Add("wander");
                        if ((bool)reader["wait"])
                            user_node["rule"]!.AsArray().Add("wait");
                        if ((bool)reader["gun"])
                            user_node["rule"]!.AsArray().Add("gun");
                        if ((bool)reader["knife"])
                            user_node["rule"]!.AsArray().Add("knife");
                        if ((bool)reader["bat"])
                            user_node["rule"]!.AsArray().Add("bat");
                    }
                    _sql.Close();
                }
            }
            user_node["cameras"] = GetAllCam(uid);
            //Console.Write(user_node.ToString());
            return user_node;
        }

        public JsonNode GetAllCam(int uid)
        {
            JsonNode all_cam_json = new JsonObject();
            List<string> sqlCommands = new()
            {
                "SELECT user_cam.cam_name, camera_info.cam_id, camera_info.is_conn, camera_info.g_key",
                "FROM camera_info JOIN user_cam",
                "ON user_cam.cam_id = camera_info.cam_id",
                "WHERE uid = @uid;",
            };
            
            using (SqlCommand command = new(string.Join(Environment.NewLine, sqlCommands), _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                {
                    lock (_lock)
                    {
                        _sql.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string cam_id = reader["cam_id"]!.ToString()!;
                                    all_cam_json[cam_id] = new JsonObject();
                                    all_cam_json[cam_id]!.AsObject()["cam_name"] = reader["cam_name"].ToString();
                                    all_cam_json[cam_id]!.AsObject()["g_key"] = reader["g_key"]!.ToString()!;
                                    all_cam_json[cam_id]!.AsObject()["is_conn"] = reader["is_conn"]!.ToString()!;
                                    all_cam_json[cam_id]!.AsObject()["danger"] = new JsonArray();
                                    all_cam_json[cam_id]!.AsObject()["record"] = new JsonArray();
                                }
                            }
                        }
                        _sql.Close();
                    }
                }
            }
            all_cam_json = GetAllDanger(uid, all_cam_json);
            all_cam_json = GetAllRecord(uid, all_cam_json);
            //Console.WriteLine(all_cam_json.ToString());
            return all_cam_json;
        }

        public JsonNode GetAllRecord(int uid, JsonNode camerasInfo)
        {
            List<string> sqlCommands = new()
            {
                "SELECT user_cam.cam_id, cam_record.time",
                "FROM user_cam JOIN cam_record",
                "ON user_cam.cam_id = cam_record.cam_id",
                "WHERE uid = @uid ORDER BY cam_record.time;",
            };
            using (SqlCommand command = new(string.Join(Environment.NewLine, sqlCommands), _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string cam_id = reader["cam_id"]!.ToString()!;
                                string time = reader["time"]!.ToString()!;
                                camerasInfo[cam_id]!["record"]!.AsArray().Add(time);
                            }
                        }
                    }
                    _sql.Close();
                }
            }
            return camerasInfo;
        }

        public JsonNode GetAllDanger(int uid, JsonNode cam_json)
        {
            List<string> sqlCommands = new()
            {
                "SELECT user_cam.cam_id, cam_danger.time",
                "FROM user_cam JOIN cam_danger",
                "ON user_cam.cam_id = cam_danger.cam_id",
                "WHERE uid = @uid ORDER BY cam_danger.time;",
            };
            using (SqlCommand command = new(string.Join(Environment.NewLine, sqlCommands), _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                {
                    lock (_lock)
                    {
                        _sql.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    string cam_id = reader["cam_id"]!.ToString()!;
                                    string time = reader["time"]!.ToString()!;
                                    cam_json[cam_id]!["danger"]!.AsArray().Add(time);
                                }
                            }
                        }
                        _sql.Close();
                    }
                }
            }
            return cam_json;
        }


        public bool IsExist(string input_email)
        {
            bool b;
            using (SqlCommand command = new("SELECT * FROM user_info WHERE email = @Email", _sql))
            {
                command.Parameters.AddWithValue("@Email", input_email);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                        b = reader.HasRows;
                    _sql.Close();
                }
            }
            return b;

        }

        public void Register(string input_user_id, string input_email, string input_password, string input_nickname, int input_gender)
        {
            using (SqlCommand command = new("INSERT INTO [user_info] (user_id, email, password, nickname, gender) VALUES (@user_id, @email, @password, @nickname, @gender)", _sql))
            {
                command.Parameters.AddWithValue("@user_id", input_user_id);
                command.Parameters.AddWithValue("@email", input_email);
                command.Parameters.AddWithValue("@password", input_password);
                command.Parameters.AddWithValue("@nickname", input_nickname);
                command.Parameters.AddWithValue("@gender", input_gender);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }  
            }
        }

        public bool AddCamera(int uid, string cam_id, string g_key,string cam_name)
        {
            if (Camera_Verify(cam_id, g_key))
            {
                using (SqlCommand command = new("INSERT INTO [user_cam] VALUES (@uid, @cam_id,@cam_name);", _sql))
                {
                    command.Parameters.AddWithValue("@uid", uid);
                    command.Parameters.AddWithValue("@cam_id", cam_id);
                    command.Parameters.AddWithValue("@cam_name", cam_name);
                    lock (_lock)
                    {
                        _sql.Open();
                        command.ExecuteNonQuery();
                        _sql.Close();
                    }
                    return true;
                }
            }
            return false;
        }

        public bool DelCamera(int uid, string cam_id, string g_key)
        {
            if (Camera_Verify(cam_id, g_key))
            {

                using (SqlCommand command = new("DELETE FROM [user_cam] WHERE uid = @uid AND cam_id = @cam_id;", _sql))
                {
                    command.Parameters.AddWithValue("@uid", uid);
                    command.Parameters.AddWithValue("@cam_id", cam_id);
                    lock (_lock)
                    {
                        _sql.Open();
                        command.ExecuteNonQuery();
                        _sql.Close();
                    }
                }
                return true;
            }
            return false;
        }

        public bool Camera_Verify(string cam_id, string g_key)
        {
            bool b;
            using (SqlCommand command = new SqlCommand("SELECT * FROM [camera_info] WHERE cam_id = @cam_id AND g_key = @g_key;", _sql))
            {
                command.Parameters.AddWithValue("@cam_id", cam_id);
                command.Parameters.AddWithValue("@g_key", g_key);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                        b = reader.HasRows;
                    _sql.Close();
                }
            }
            return b;
        }

        public List<string> GetShares(string cam_id)
        {
            List<string> Shares = new List<string> ();
            using (SqlCommand command = new("SELECT user_info.nickname FROM user_cam\r\nJOIN user_info ON user_cam.uid = user_info.uid\r\nWHERE cam_id = @cam_id order by user_cam.uid ;", _sql))
            {
                command.Parameters.AddWithValue("@cam_id", cam_id);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string sharer = reader["nickname"]!.ToString()!;
                                Shares.Add(sharer);
                            }
                            
                        }
                    _sql.Close();
                }

            }
            return Shares;
        }

        public void ReName(int uid,string NewName)
        {
            using (SqlCommand command = new("UPDATE user_info SET nickname = @NewName WHERE uid = @uid;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@NewName", NewName);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }
            }
        }

        public void ChangePassword(int uid, string NewPassword)
        {
            using (SqlCommand command = new("UPDATE user_info SET password = @NewPassword WHERE uid = @uid;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@NewPassword", NewPassword);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }
            }
        }

        public void ReNameCamera(int uid,string cam_id,string NewNameCamera)
        {
            using (SqlCommand command = new("UPDATE user_cam SET cam_name = @NewNameCamera WHERE uid = @uid AND cam_id = @cam_id;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@cam_id", cam_id);
                command.Parameters.AddWithValue("@NewNameCamera", NewNameCamera);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }
            }
        }

        public bool UserCameraVerify(int uid, string cam_id)
        {
            bool b;
            using (SqlCommand command = new("SELECT * FROM user_cam WHERE uid = @uid AND cam_id = @cam_id;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@cam_id", cam_id);
                lock (_lock)
                {
                    _sql.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                        b = reader.HasRows;
                    _sql.Close();
                }
            }
            return b;
        }

        public bool CheckisConn(string cam_id)
        {
            bool b = false;
            using (SqlCommand command = new("SELECT is_conn FROM camera_info WHERE cam_id = @cam_id;", _sql))
            {
                command.Parameters.AddWithValue("@cam_id", cam_id);
                {
                    lock (_lock)
                    {
                        _sql.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                                while (reader.Read())
                                    b = (bool)reader["is_conn"]!;
                        }
                        _sql.Close();
                    }
                }
            }
            return b;
        }

        public void ReScore(int uid, int NewScore)
        {
            using (SqlCommand command = new("UPDATE user_info SET score = @NewScore, mode = 0 WHERE uid = @uid;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@NewScore", NewScore);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }
            }
        }

        public void ReRule(int uid, Dictionary<string, bool> NewRule)
        {
            using (SqlCommand command = new("UPDATE user_info SET wander = @wander, wait = @wait, gun = @gun, knife = @knife, bat = @bat, mode = 1 WHERE uid = @uid;", _sql))
            {
                command.Parameters.AddWithValue("@uid", uid);
                command.Parameters.AddWithValue("@wander", NewRule["wander"]);
                command.Parameters.AddWithValue("@wait", NewRule["wait"]);
                command.Parameters.AddWithValue("@gun", NewRule["gun"]);
                command.Parameters.AddWithValue("@knife", NewRule["knife"]);
                command.Parameters.AddWithValue("@bat", NewRule["bat"]);
                lock (_lock)
                {
                    _sql.Open();
                    command.ExecuteNonQuery();
                    _sql.Close();
                }
            }
        }

        public void AddRecord(int camId, string recordTime)
        {
            using SqlCommand command = new("INSERT INTO cam_record VALUES(@cam_id, @time)", _sql);
            command.Parameters.AddWithValue("@cam_id", camId);
            command.Parameters.AddWithValue("@time", recordTime);
            lock (_lock)
            {
                _sql.Open();
                command.ExecuteNonQuery();
                _sql.Close();
            }
        }

        public SortedDictionary<DateOnly, int> GetAmount(int camId, int days = 7)
        {
            List<string> commands = new()
            {
                "SELECT wait_amount + wander_amount",
                "FROM danger_amount",
                "WHERE cam_id = @cam_id AND time = @date",
            };
			SortedDictionary<DateOnly, int> amounts = new();
			DateOnly date = DateOnly.FromDateTime(DateTime.Now);
			for (int i = 0; i < days; i++)
			{
                using (SqlCommand command = new(string.Join(Environment.NewLine, commands), _sql))
                {
                    command.Parameters.AddWithValue("@cam_id", camId);
                    command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
                    lock (_lock)
                    {
                        _sql.Open();
                        amounts[date] = (int?)command.ExecuteScalar() ?? 0;
                        _sql.Close();
                    }
                }
				date = date.AddDays(-1);
			}
            return amounts;
		}

        public Dictionary<string, SortedDictionary<DateOnly, int>> GetDetailAmount(int camId, int days = 7)
        {
			List<string> commands = new()
			{
				"SELECT pass_amount, wait_amount, wander_amount",
				"FROM danger_amount",
				"WHERE cam_id = @cam_id AND time = @date",
			};
			Dictionary<string, SortedDictionary<DateOnly, int>> amounts = new()
			{
				["pass"] = new(),
				["wait"] = new(),
                ["wander"] = new(),
			};
            DateOnly date = DateOnly.FromDateTime(DateTime.Now);
            for (int i = 0;i < days; i++)
            {
                using (SqlCommand command = new(string.Join(Environment.NewLine, commands), _sql))
				{
					command.Parameters.AddWithValue("@cam_id", camId);
					command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
					lock (_lock)
					{
						_sql.Open();
						using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                foreach (var key in amounts.Keys)
                                    amounts[key][date] = (int)reader[$"{key}_amount"];
                            }
                            else
                            {
                                foreach (var pair in amounts)
                                    pair.Value[date] = 0;
                            }
                        }
						_sql.Close();
					}
				}
				date = date.AddDays(-1);
			}
            return amounts;
		}
    }
}
