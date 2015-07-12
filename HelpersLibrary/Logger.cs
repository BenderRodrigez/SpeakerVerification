using System;
using System.IO;

namespace HelpersLibrary
{
    internal static class Logger
    {
        /// <summary>
        ///     Текущий путь расположения
        /// </summary>
        private static readonly string CurPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");

        /// <summary>
        ///     Записывает информацию в лог-файл
        /// </summary>
        /// <param name="erInAc">Код сообщения:-1 - ошибка, 0 - инфмормация, 1 - успешно</param>
        /// <param name="log">Текст сообщения</param>
        /// <param name="programmName">Название приложения записывающее сообщение в лог</param>
        public static void SetLog(int erInAc, string log, string programmName)
        {
            ClearLog();
            try
            {
                using (var s = new StreamWriter(new FileStream(CurPath, FileMode.Append, FileAccess.Write)))
                {
                    var addit = programmName + ": ";
                    switch (erInAc)
                    {
                        case -1:
                            addit += "<Error>";
                            break;
                        case 1:
                            addit += "<All right>";
                            break;
                        default:
                            addit += "<Info>";
                            break;
                    }
                    s.WriteLine("{2:dd.MM.yyyy HH:mm:ss} {0} : {1};", addit, log, DateTime.Now);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(@"[Error] While create or update log file: " + e.StackTrace);
            }
        }

        /// <summary>
        ///     Отчищает текущий лог от записей, при достижении размера в 10 Мб
        /// </summary>
        private static void ClearLog()
        {
            var filePath = CurPath;
            if (!File.Exists(filePath))
                return;
            var file = new FileInfo(filePath);
            if (file.Length > 1024*1024*10)
            {
                File.Delete(filePath);
            }
        }
    }
}