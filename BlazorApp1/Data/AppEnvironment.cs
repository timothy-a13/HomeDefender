namespace BlazorApp1.Data
{
    public static class AppEnvironment
    {
        public static string DatabaseConnectionString =>
            Environment.GetEnvironmentVariable("HOMEDEFENDER_DB_CONNECTION_STRING")
            ?? "Server=localhost;Database=detection_sys_db;Integrated Security=True;TrustServerCertificate=True;";

        public static string VideoStorageRoot =>
            Environment.GetEnvironmentVariable("HOMEDEFENDER_VIDEO_ROOT")
            ?? Path.Combine(AppContext.BaseDirectory, "live");

        public static string? PythonHome =>
            Environment.GetEnvironmentVariable("HOMEDEFENDER_PYTHON_HOME");

        public static string CorePath =>
            Environment.GetEnvironmentVariable("HOMEDEFENDER_CORE_PATH")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Core"));
    }
}
