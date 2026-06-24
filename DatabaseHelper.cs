using System.Data.SQLite;

namespace PowerShellAnalyzer
{
    public static class DatabaseHelper
    {
        private static string _dbFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerShellScriptAnalyzer"
        );

        private static string _configFilePath = Path.Combine(_dbFolder, "dbpath.txt");
        private static string _dbFileName;

        public static string DbFileName
        {
            get
            {
                if (_dbFileName == null)
                {
                    if (File.Exists(_configFilePath))
                    {
                        string savedPath = File.ReadAllText(_configFilePath).Trim();
                        if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(Path.GetDirectoryName(savedPath)))
                        {
                            _dbFileName = savedPath;
                            return _dbFileName;
                        }
                    }
                    _dbFileName = Path.Combine(_dbFolder, "scripts_db.sqlite");
                }
                return _dbFileName;
            }
        }

        public static void ChangeDatabasePath(string newPath)
        {
            if (!Directory.Exists(_dbFolder))
            {
                Directory.CreateDirectory(_dbFolder);
            }

            File.WriteAllText(_configFilePath, newPath);
            _dbFileName = newPath;
            InitializeDatabase();
        }

        // Ciąg połączeniowy wskazujący na nasz lokalny plik
        private static string ConnectionString => $"Data Source={DbFileName};Version=3;";

        // Tworzy plik bazy i tabelę (jeśli jeszcze nie istnieją)
        public static void InitializeDatabase()
        {
            string currentDbFolder = Path.GetDirectoryName(DbFileName);
            if (!Directory.Exists(currentDbFolder))
            {
                Directory.CreateDirectory(currentDbFolder);
            }

            if (!File.Exists(DbFileName))
            {
                SQLiteConnection.CreateFile(DbFileName);
            }

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Tabela na skrypty (ta już była)
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Scripts (
                        Path TEXT PRIMARY KEY,
                        FileName TEXT,
                        Status TEXT,
                        Description TEXT
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                    command.ExecuteNonQuery();

                // NOWA TABELA NA FOLDERY
                string createSourcesQuery = "CREATE TABLE IF NOT EXISTS Sources (FolderPath TEXT PRIMARY KEY)";
                using (var command = new SQLiteCommand(createSourcesQuery, connection))
                    command.ExecuteNonQuery();

                // ALBEN
                string createAlbumsQuery = @"
                    CREATE TABLE IF NOT EXISTS Albums (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ParentId INTEGER NULL,
                        Name TEXT NOT NULL
                    )";
                using (var command = new SQLiteCommand(createAlbumsQuery, connection))
                    command.ExecuteNonQuery();

                // SCRIPT_ALBUMS
                string createScriptAlbumsQuery = @"
                    CREATE TABLE IF NOT EXISTS ScriptAlbums (
                        ScriptPath TEXT NOT NULL,
                        AlbumId INTEGER NOT NULL,
                        PRIMARY KEY (ScriptPath, AlbumId)
                    )";
                using (var command = new SQLiteCommand(createScriptAlbumsQuery, connection))
                    command.ExecuteNonQuery();

                // Dodać kolumnę ScriptId, jeśli nie istnieje
                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Scripts ADD COLUMN ScriptId INTEGER", connection))
                        cmd.ExecuteNonQuery();
                }
                catch { } // Ignoruj błąd, jeśli kolumna już istnieje

                // Tabela do ręcznego śledzenia najwyższego ID (jak PESEL), aby nigdy nie nadać tego samego numeru po usunięciu
                string createSeqQuery = "CREATE TABLE IF NOT EXISTS Sequences (Name TEXT PRIMARY KEY, Value INTEGER)";
                using (var command = new SQLiteCommand(createSeqQuery, connection))
                    command.ExecuteNonQuery();

                string initSeqQuery = "INSERT OR IGNORE INTO Sequences (Name, Value) VALUES ('ScriptId', (SELECT COALESCE(MAX(ScriptId), 0) FROM Scripts))";
                using (var command = new SQLiteCommand(initSeqQuery, connection))
                    command.ExecuteNonQuery();

                // Wypełnij istniejące wiersze, które nie mają ScriptId
                try
                {
                    using (var cmd = new SQLiteCommand("SELECT Path FROM Scripts WHERE ScriptId IS NULL", connection))
                    {
                        var nullPaths = new System.Collections.Generic.List<string>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read()) nullPaths.Add(reader["Path"].ToString());
                        }

                        foreach (var p in nullPaths)
                        {
                            using (var seqCmd = new SQLiteCommand("UPDATE Sequences SET Value = Value + 1 WHERE Name = 'ScriptId'; SELECT Value FROM Sequences WHERE Name = 'ScriptId';", connection))
                            {
                                var valResult = seqCmd.ExecuteScalar();
                                int val = valResult != DBNull.Value && valResult != null ? Convert.ToInt32(valResult) : 1;
                                using (var updCmd = new SQLiteCommand("UPDATE Scripts SET ScriptId = @V WHERE Path = @P", connection))
                                {
                                    updCmd.Parameters.AddWithValue("@V", val);
                                    updCmd.Parameters.AddWithValue("@P", p);
                                    updCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch { }

                // TABELA USTAWIEŃ (np. klucze API)
                string createSettingsQuery = "CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT)";
                using (var command = new SQLiteCommand(createSettingsQuery, connection))
                    command.ExecuteNonQuery();
            }
        }

        // Pobiera ustawienie
        public static string GetSetting(string key)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("SELECT Value FROM Settings WHERE Key = @Key", connection))
                {
                    command.Parameters.AddWithValue("@Key", key);
                    var result = command.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        // Zapisuje ustawienie
        public static void SaveSetting(string key, string value)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand("INSERT INTO Settings (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = @Value", connection))
                {
                    command.Parameters.AddWithValue("@Key", key);
                    command.Parameters.AddWithValue("@Value", value);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Zapisuje nowy skrypt lub aktualizuje istniejący (np. po analizie AI lub ręcznej edycji)
        public static void SaveScript(string path, string fileName, string status, string description)
        {
            var info = GetOrInitializeScriptInfo(path, fileName); // Ensure row exists and has an ID
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string updateQuery = @"
                    UPDATE Scripts SET 
                        FileName = @FileName,
                        Status = @Status, 
                        Description = @Description
                    WHERE Path = @Path";

                using (var command = new SQLiteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    command.Parameters.AddWithValue("@FileName", fileName);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Description", description);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Pobiera zapisane informacje o skrypcie na podstawie jego ścieżki i nadaje unikatowe ID
        public static (string Status, string Description, int ScriptId, bool IsNew) GetOrInitializeScriptInfo(string path, string fileName)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Najpierw sprawdzamy, czy skrypt jest w bazie i czy ma nadane ScriptId
                string selectQuery = "SELECT Status, Description, ScriptId FROM Scripts WHERE Path = @Path";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var scriptIdObj = reader["ScriptId"];
                            if (scriptIdObj != DBNull.Value)
                            {
                                return (reader["Status"].ToString(), reader["Description"].ToString(), Convert.ToInt32(scriptIdObj), false);
                            }
                        }
                    }
                }

                // Jeśli nie ma, pobieramy najwyższe zachowane ID w tabeli Sequences i inkrementujemy
                int newId = 1;
                using (var command = new SQLiteCommand("UPDATE Sequences SET Value = Value + 1 WHERE Name = 'ScriptId'", connection))
                    command.ExecuteNonQuery();

                using (var command = new SQLiteCommand("SELECT Value FROM Sequences WHERE Name = 'ScriptId'", connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        newId = Convert.ToInt32(result);
                    }
                }

                // Wstawiamy nowy rekord z nowym ScriptId, a jeśli ścieżka istnieje (bez ID), to uaktualniamy
                string insertQuery = @"
                    INSERT INTO Scripts (Path, FileName, Status, Description, ScriptId) 
                    VALUES (@Path, @FileName, @Status, @Description, @ScriptId)
                    ON CONFLICT(Path) DO UPDATE SET ScriptId = @ScriptId";

                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    command.Parameters.AddWithValue("@FileName", fileName);
                    command.Parameters.AddWithValue("@Status", "⏳");
                    command.Parameters.AddWithValue("@Description", "");
                    command.Parameters.AddWithValue("@ScriptId", newId);
                    command.ExecuteNonQuery();
                }

                // Pobieramy wartości raz jeszcze po utworzeniu
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (reader["Status"].ToString(), reader["Description"].ToString(), Convert.ToInt32(reader["ScriptId"]), true);
                        }
                    }
                }
            }
            return ("⏳", "", 1, true);
        }

        // Zgodność wsteczna, jeśli ktoś nie przekazuje nazwy piku (np do sprawdzenia statusu AI)
        public static (string Status, string Description) GetScriptInfo(string path)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Status, Description FROM Scripts WHERE Path = @Path";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (reader["Status"].ToString(), reader["Description"].ToString());
                        }
                    }
                }
            }
            return ("⏳", ""); // Jeśli skryptu nie ma w bazie, zwracamy wartości domyślne (brak analizy)
        }

        // Zapisuje nowy folder do bazy
        public static void SaveSource(string folderPath)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT OR IGNORE INTO Sources (FolderPath) VALUES (@FolderPath)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@FolderPath", folderPath);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Pobiera zapisane foldery z bazy
        public static System.Collections.Generic.List<string> GetSources()
        {
            var sources = new System.Collections.Generic.List<string>();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT FolderPath FROM Sources";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sources.Add(reader["FolderPath"].ToString());
                        }
                    }
                }
            }
            return sources;
        }

        // --- BIBLIOTEKA (VIRTUAL ALBUMS) ---

        public static int AddAlbum(string name, int? parentId = null)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "INSERT INTO Albums (Name, ParentId) VALUES (@Name, @ParentId); SELECT last_insert_rowid();";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@ParentId", parentId.HasValue ? (object)parentId.Value : DBNull.Value);
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public static void UpdateAlbum(int id, string name, int? parentId = null)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "UPDATE Albums SET Name = @Name, ParentId = @ParentId WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@ParentId", parentId.HasValue ? (object)parentId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteAlbum(int id)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                // Delete album
                string query = "DELETE FROM Albums WHERE Id = @Id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }

                // Delete script associations
                string cleanupQuery = "DELETE FROM ScriptAlbums WHERE AlbumId = @Id";
                using (var cmd2 = new SQLiteCommand(cleanupQuery, connection))
                {
                    cmd2.Parameters.AddWithValue("@Id", id);
                    cmd2.ExecuteNonQuery();
                }

                // Delete children
                string childrenQuery = "SELECT Id FROM Albums WHERE ParentId = @Id";
                using (var cmd3 = new SQLiteCommand(childrenQuery, connection))
                {
                    cmd3.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd3.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DeleteAlbum(Convert.ToInt32(reader["Id"]));
                        }
                    }
                }
            }
        }

        public static System.Data.DataTable GetAlbums()
        {
            var dt = new System.Data.DataTable();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT Id, ParentId, Name FROM Albums ORDER BY Name";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }

        public static void AddScriptToAlbum(string scriptPath, int albumId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "INSERT OR IGNORE INTO ScriptAlbums (ScriptPath, AlbumId) VALUES (@Path, @AlbumId)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Path", scriptPath);
                    command.Parameters.AddWithValue("@AlbumId", albumId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void RemoveScriptFromAlbum(string scriptPath, int albumId)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "DELETE FROM ScriptAlbums WHERE ScriptPath = @Path AND AlbumId = @AlbumId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Path", scriptPath);
                    command.Parameters.AddWithValue("@AlbumId", albumId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static bool IsScriptInLibrary(string scriptPath)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(1) FROM ScriptAlbums WHERE ScriptPath = @Path";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Path", scriptPath);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public static System.Data.DataTable GetScriptsInAlbum(int albumId)
        {
            var dt = new System.Data.DataTable();
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = @"
                    SELECT S.Path, S.FileName, S.Description
                    FROM Scripts S
                    INNER JOIN ScriptAlbums SA ON S.Path = SA.ScriptPath
                    WHERE SA.AlbumId = @AlbumId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AlbumId", albumId);
                    using (var reader = command.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }

        public static void MoveScript(string oldPath, string newPath, string newName)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string qScripts = "UPDATE Scripts SET Path = @NewPath, FileName = @NewName WHERE Path = @OldPath";
                        using (var c1 = new SQLiteCommand(qScripts, connection))
                        {
                            c1.Parameters.AddWithValue("@NewPath", newPath);
                            c1.Parameters.AddWithValue("@NewName", newName);
                            c1.Parameters.AddWithValue("@OldPath", oldPath);
                            c1.ExecuteNonQuery();
                        }

                        string qAlbums = "UPDATE ScriptAlbums SET ScriptPath = @NewPath WHERE ScriptPath = @OldPath";
                        using (var c2 = new SQLiteCommand(qAlbums, connection))
                        {
                            c2.Parameters.AddWithValue("@NewPath", newPath);
                            c2.Parameters.AddWithValue("@OldPath", oldPath);
                            c2.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static void UpdateScriptFileName(string path, string newName)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string query = "UPDATE Scripts SET FileName = @FileName WHERE Path = @Path";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FileName", newName);
                    command.Parameters.AddWithValue("@Path", path);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteScriptData(string path)
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string q1 = "DELETE FROM ScriptAlbums WHERE ScriptPath = @Path";
                using (var c1 = new SQLiteCommand(q1, connection))
                {
                    c1.Parameters.AddWithValue("@Path", path);
                    c1.ExecuteNonQuery();
                }

                string q2 = "DELETE FROM Scripts WHERE Path = @Path";
                using (var c2 = new SQLiteCommand(q2, connection))
                {
                    c2.Parameters.AddWithValue("@Path", path);
                    c2.ExecuteNonQuery();
                }
            }
        }

        public static int CleanMissingScripts()
        {
            int removedFilesCount = 0;
            var pathsToCheck = new System.Collections.Generic.List<string>();

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Path FROM Scripts";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            pathsToCheck.Add(reader["Path"].ToString());
                        }
                    }
                }
            }

            foreach (var path in pathsToCheck)
            {
                if (!File.Exists(path))
                {
                    DeleteScriptData(path);
                    removedFilesCount++;
                }
            }
            return removedFilesCount;
        }
    }
}