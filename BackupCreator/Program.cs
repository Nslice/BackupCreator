using System;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;


namespace BackupCreator
{
   // TODO: я же мог использовать App.config для сохранения настроек, а не придумывать свой xml config
   // TODO: вылетает ошибка при сохранеии больше 15 секунд

   public class Program
   {
      public static void Main(string[] args)
      {
         if (args.Length > 0 && args[0] == Arguments.Init)
         {
            ConfigManager.CreateDefaultConfigFile();
            Console.WriteLine($"Config created `{ConfigInfo.CONFIG_PATH}`");
         }
         else
         {
            Config config = ConfigManager.GetConfig();
            using (var connection = new SqlConnection(GetMsConnetionString(config)))
            {
               // TODO: тут могут вылелать ошибки, из-за того что прав не хватает и прочие, надо доработать
               var backuper = new BackupCreator(connection, config);
               string bakFileName = backuper.BackupDatabase();

               Console.WriteLine("\n\nCreating ZIP archive started...");
               backuper.CreateZip(bakFileName);

               backuper.DeleteBakFile(bakFileName);
               Console.WriteLine("SUCCESS!");
            }
         }

         Console.ReadLine();
      }

      private static string GetMsConnetionString(Config config)
      {
         var strBuilder = new SqlConnectionStringBuilder
         {
            DataSource = config.ConnectionDetails?.ServerInstance,
            ConnectTimeout = config.ConnectionDetails?.ConnectTimeout ?? 15,
            InitialCatalog = "master",
            IntegratedSecurity = true
         };

         return strBuilder.ToString();
      }
   }



   public class BackupCreator
   {
      private readonly Config _config;
      private readonly SqlConnection _connection;

      public BackupCreator(SqlConnection connection, Config config)
      {
         this._config = config;
         this._connection = connection;
      }

      public string BackupDatabase()
      {
         _connection.FireInfoMessageEventOnUserErrors = true;
         _connection.InfoMessage += OnInfoMessage;
         _connection.Open();

         string backupFileName = $"{_config.Database}_{DateTime.Now:dd.MM.yyyy_HH.mm}.bak";
         string backFilePath = $@"{_config.BackupPath}\{backupFileName}";
         string cmdText =
            $@"BACKUP DATABASE {_config.Database} TO DISK = N'{backFilePath}' WITH NOFORMAT, INIT, SKIP, NOREWIND, NOUNLOAD, STATS = 5";

         Console.WriteLine(cmdText);
         using (var cmd = new SqlCommand(cmdText, _connection))
         {
            cmd.CommandTimeout = 1800; // 30 минут на выполнение команды
            cmd.ExecuteNonQuery();
         }
         _connection.Close();
         _connection.InfoMessage -= OnInfoMessage;
         _connection.FireInfoMessageEventOnUserErrors = false;

         return backupFileName;
      }

      private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
      {
         foreach (SqlError info in e.Errors)
         {
            if (info.Class > 10)
               Console.WriteLine(info);
            else
               Console.Write($"\r\r\r\r\r\r\r\r\r\r\r\r\\r\r\r\r\r\r\r\r\r\r\r\r\r\r{info.Message}");
         }
      }

      public string CreateZip(string backupFileName)
      {
         string zipFileName = $"{backupFileName}.zip";
         string zipFilePath = Path.Combine(_config.BackupPath, zipFileName);

         using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
         {
            ZipArchiveEntry entry = archive.CreateEntry(backupFileName);
            entry.LastWriteTime = DateTimeOffset.Now;

            using (FileStream input = File.OpenRead(Path.Combine(_config.BackupPath, backupFileName)))
            using (Stream entryStream = entry.Open())
            {
               //TODO: сделать приделать прогресс на архивацию https://stackoverflow.com/questions/22857713/stream-copyto-with-progress-bar-reporting
               byte[] buffer = new byte[16 * 1024];
               int read;
               long progress = 0;
               long fileLength = input.Length;
               Console.WriteLine("Progress:");
               while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
               {
                  entryStream.Write(buffer, 0, read);
                  progress += read;
                  int percent = Convert.ToInt32(100.0f / fileLength * progress);
                  Console.Write($"\r\r\r\r{percent}%");
               }
               Console.WriteLine();
            }
         }

         return zipFileName;
      }

      public void DeleteBakFile(string filename)
      {
         File.Delete(Path.Combine(_config.BackupPath, filename));
      }
   }

}
