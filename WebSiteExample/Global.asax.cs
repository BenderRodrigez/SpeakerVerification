using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebGrease.Css.Extensions;
using WebSiteExample.Helpers;

namespace WebSiteExample
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitSpeech();
        }

        private void InitSpeech()
        {
            int sampleRate;
            var file = HelpersLibrary.FileReader.ReadFileNormalized(
                "D:\\YandexDisk\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\Жирные сазаны ушли под палубу\\ГРР1.wav",
                out sampleRate);
            var pcmConverted = new short[file.Length];
            for (int i = 0; i < file.Length; i++)
            {
                pcmConverted[i] = (short) Math.Round(file[i]*short.MaxValue);
            }
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            pcmConverted.ForEach(x => writer.Write(x));
            var recognizer = new SpeechRecognizer(stream);
        }
    }
}
