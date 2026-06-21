using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;

namespace BlazorApp1.Data
{
    public class SessionManager
    {
        private readonly ProtectedLocalStorage _localStorage;
        private readonly SqlManager _sqlManager;
        private bool _hasInitial;
        public SessionManager(ProtectedLocalStorage localStorage, SqlManager sqlManeger)
        {
            _hasInitial = false;
            _localStorage = localStorage;
            _sqlManager = sqlManeger;
            Console.WriteLine("Constructor");
        }

        public async Task<bool> IsInitialized()
        {
            if (!_hasInitial)
            {
                await Initialized();
                _hasInitial = true;
            }
            return _hasInitial;
            /*
            return await Task.Run(new Func<Task<bool>>(async () =>
            {
                int millisecondsDelay = 2;
                timeout = timeout * 1000 / millisecondsDelay;
                for (int i = 0; i < timeout; i++)
                {
                    if (_hasInitial) return true;
                    await Task.Delay(millisecondsDelay);
                }
                return false;
            }));
            */
        }

        public async Task Initialized()
        {
            if ((await _localStorage.GetAsync<int>("uid")).Success)
            {
                int uid = (await _localStorage.GetAsync<int>("uid")).Value;
                JsonNode node = await Task.Run(() => _sqlManager.GetAllCam(uid));
                await _localStorage.SetAsync("cameras", node);
                Console.WriteLine("UID: {0}", uid);
            }
        }

        public async ValueTask<bool> IsLogin()
        {
            return (await _localStorage.GetAsync<int>("uid")).Success;
        }

        public async Task<ProtectedBrowserStorageResult<T>> GetElement<T>(string key)
        {
            int counter = 0;
            while (true)
            {
                try
                {
                    var result = await _localStorage.GetAsync<T>(key);
                    return result;
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine("{0}: {1}", ex.Message, counter++);
                    continue;
                }
            }
        }

        public async Task Set_User_Info(string email)
        {
            //只有在登入才會執行一次
            JsonNode user_json = _sqlManager.Get_User_Info(email);
            Console.WriteLine(user_json.ToString());
            await _localStorage.SetAsync("uid", (int)user_json["uid"]!);
            await _localStorage.SetAsync("mode", user_json["mode"]!);
            await _localStorage.SetAsync("score", (int)user_json["score"]!);
            await _localStorage.SetAsync("rule", user_json["rule"]!);
            await _localStorage.SetAsync("user_id", user_json["user_id"]!.ToString());
            await _localStorage.SetAsync("email", user_json["email"]!.ToString());
            await _localStorage.SetAsync("nickname", user_json["nickname"]!.ToString());
            await _localStorage.SetAsync("gender", user_json["gender"]!.ToString());
            await _localStorage.SetAsync("cameras", user_json["cameras"]!);
            /*
             Console.WriteLine("11111111111111111");
             var result = await _localStorage.GetAsync<int>("uid0");
             Console.WriteLine(result.Success);
             Console.WriteLine(result.Value);
             Console.WriteLine("22222222222222222");*/
        }

        public async Task Set_Cam_Info(int uid)
        {
            JsonNode cam_json = _sqlManager.GetAllCam(uid);
            await _localStorage.SetAsync("cameras", cam_json);
        }

        public async Task<List<string>> GetCamId()
        {
            List<string> cam_ids = new();
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            if (cam_json != null)
            {
                foreach (KeyValuePair<string, JsonNode?> pair in cam_json.AsObject())
                {
                    cam_ids.Add(pair.Key);
                }
            }

            return cam_ids;
        }

        public async Task<string> GetCamName(string cam_id)
        {
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            return cam_json[cam_id]!["cam_name"]!.ToString();
        }

        public async Task<string> GetG_Key(string cam_id)
        {
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            return cam_json[cam_id]!["g_key"]!.ToString();
        }

        public async Task<string> GetConn(string cam_id)
        {
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            return cam_json[cam_id]!["is_conn"]!.ToString();
        }

        public async Task<List<string>> GetDanger(string cam_id)
        {
            List<string> times = new List<string>();
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            JsonNode cam_time = cam_json[cam_id]!["danger"]!.AsArray();
            if (cam_time.AsArray() != null)
            {
                foreach (string? time in cam_time.AsArray())
                {
                    times.Add(time!);
                }
            }
            return times;
        }

        public async Task<List<string>> GetRecord(string camId)
        {
            List<string> recordTimes = new();
            JsonNode camerasInfo = (await GetElement<JsonNode>("cameras")).Value!;
            JsonArray camRecordTimes = camerasInfo[camId]!["record"]!.AsArray();
            if (camRecordTimes is not null)
            {
                foreach (string recordTime in camRecordTimes.Select(v => (string)v!))
                {
                    recordTimes.Add(recordTime!);
                }
            }
            return recordTimes;
        }

        public async Task AddRecord(string cam_id, string recordTime)
        {
            JsonNode camerasInfo = (await GetElement<JsonNode>("cameras")).Value!;
            camerasInfo[cam_id]!["record"]!.AsArray().Add(recordTime);
            await _localStorage.SetAsync("cameras", camerasInfo);
        }

        public async Task SetDanger(string cam_id, string time)
        {
            JsonNode cam_json = (await GetElement<JsonNode>("cameras")).Value!;
            cam_json[cam_id]!["danger"]!.AsArray().Add(time);
            await _localStorage.SetAsync("cameras", cam_json);
        }

        public async Task<List<string>> GetRule()
        {
            List<string> list = new List<string>();
            JsonNode rules = (await GetElement<JsonNode>("rule")).Value!;
            if (rules != null)
            {
                foreach (string? rule in rules.AsArray())
                {
                    list.Add(rule!);
                }

            }

            return list;
        }

        public async ValueTask Clear()
        {
            await _localStorage.DeleteAsync("uid");
            await _localStorage.DeleteAsync("user_id");
            await _localStorage.DeleteAsync("email");
            await _localStorage.DeleteAsync("password");
            await _localStorage.DeleteAsync("nickname");
            await _localStorage.DeleteAsync("gender");
            await _localStorage.DeleteAsync("cameras");
            await _localStorage.DeleteAsync("score");
            await _localStorage.DeleteAsync("rule");
            await _localStorage.DeleteAsync("mode");
        }
    }
}
