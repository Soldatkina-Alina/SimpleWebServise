using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Net;
using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace WebApplication.Controllers
{
    public class ValuesController : ApiController
    {
        ConcurrentQueue<ImageUml> queueImages = new ConcurrentQueue<ImageUml>();
        BlockingCollection<Image> queueImagesToAnswer = new BlockingCollection<Image>();
        ResourceQuery query;
        bool isThreadComplete = false;

        [HttpGet]
        public IHttpActionResult GetImages([FromUri] ResourceQuery query)
        {
            if (query.URL == null || query?.ImageCount < 1 || query?.ThreadCount < 1)
                return Json("Неверные параметры в запросе");
            this.query = query;
            Thread thread = new Thread(donwloadHtml);
            thread.Start();
            thread.Join(60000);

            return Json(queueImagesToAnswer);
        }
        void donwloadHtml()
        {
                using (WebClient wc = new WebClient())
                {
                    DownloadStringCompleted(wc.DownloadString(new Uri(query.URL))); //https://zastavok.net/
                };
        }

         void DownloadStringCompleted(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            // <img src="/ts/priroda/159422316733.jpg" alt="горы скалы ночь дом" class="short_prev_img" itemprop="thumbnail">

            var listImgFromHtml = doc.DocumentNode.SelectNodes("//img").Where(x => x.Name == "img").Select(y => y.OuterHtml).ToList();

            Regex regexImg = new Regex(@"img.*src=""\S*""", RegexOptions.ExplicitCapture);
            Regex regexAlt = new Regex(@"img.*alt="".*?""", RegexOptions.ExplicitCapture);
            foreach (var lineImgFromHtml in listImgFromHtml)
            {
                String fileName = "", filePath = "";
                MatchCollection matches = regexImg.Matches(lineImgFromHtml);
                MatchCollection matches2 = regexAlt.Matches(lineImgFromHtml);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        Console.WriteLine(match.Value);
                        if(match.Value.Contains("http") || match.Value.Contains("https"))
                            filePath = match.Value.Substring(match.Value.IndexOf(('\"'))).Replace("\"", "");
                        else
                            filePath = match.Value.Substring(match.Value.IndexOf(('/'))).Replace("\"", "");
                        fileName = match.Value.Substring(match.Value.LastIndexOf(('/')) + 1).Replace("\"", "");
                        Console.WriteLine(match.Value + "\n" + fileName + "  " + filePath);
                    }
                }

                if (matches2.Count > 0)
                    foreach (Match match2 in matches2)
                        Console.WriteLine(match2.Value + "\n");
                if (fileName.Length < 1 && filePath.Length < 1) continue;

                ImageUml img = new ImageUml();
                img.Host = query.URL; // "https://zastavok.net";
                img.Alt = fileName;
                img.Src = filePath;
                if (filePath.Contains("http") || filePath.Contains("https"))
                    img.UmlImage = filePath;
                img.UmlImage = img.Host + filePath;
                queueImages.Enqueue(img);
                
            }

            ImageUml imageUmlOut;
            for (int i = queueImages.Count; i > query.ImageCount; i--)
                queueImages.TryDequeue(out imageUmlOut);

            int MaxThreadCount = Environment.ProcessorCount * 4;
            int MaxThreadCountFromClient = query.ThreadCount <= MaxThreadCount && query.ThreadCount > 0 ? query.ThreadCount : MaxThreadCount;

            Task[] tasks = new Task[MaxThreadCountFromClient];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Task.Factory.StartNew(() => DonwloadImageFile());

            try
            {
                Task.WaitAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // в лог
            }
        }

        void DonwloadImageFile()
        {
            while (!queueImages.IsEmpty)
            {
                using (WebClient wClient = new WebClient())
                {
                    Image img = new Image();
                    ImageUml imgUml;
                    queueImages.TryDequeue(out imgUml);
                    string host = imgUml.Host.Substring(imgUml.Host.IndexOf('/'), (imgUml.Host.IndexOf('.') - imgUml.Host.IndexOf('/'))).Replace("/", "");
                    string directory = @"C:\TestPhoto\" + host + @"\"; //вынести в файл конфигурации
                    if (!System.IO.Directory.Exists(directory)) 
                        System.IO.Directory.CreateDirectory(directory);
                    string path = directory + imgUml.Alt; 
                    img.Alt = imgUml.Alt;
                    img.Src = imgUml.Src;
                    img.Host = imgUml.Host;
                    try
                    {
                        wClient.DownloadFile(new Uri(imgUml.UmlImage), path);
                        byte[] imgByte = wClient.DownloadData(imgUml.UmlImage);
                        img.Size = imgByte.Length;
                        queueImagesToAnswer.Add(img);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex); // в лог
                    }
                }
            }
        }
    }

    public class ResourceQuery
    {
        string url;
        public string URL
        {
            get { return url; }
            set { url = value; }
        } //проверка на корректный url

        int threadCount;
        public int ThreadCount
        {
            get { return threadCount; }
            set { threadCount = value; }
        } // проверка что больше 0 и меньше возможного количества потоков

        int imageCount;
        public int ImageCount
        {
            get { return imageCount; }
            set { imageCount = value; } // проверка, что не меньше 0
        }
    }

    public class ImageUml : Image
    {
        string purtUmlImage;
        public string PurtUmlImage { get { return purtUmlImage; } set { purtUmlImage = value; } }
        string umlImage;
        public string UmlImage { get { return umlImage; } set { umlImage = value; } }
    }

    public class Image
    {
        string host;
        public string Host { get { return host; } set { host = value; } }
        string alt;
        public string Alt { get { return alt; } set { alt = value; } }
        string src;
        public string Src { get { return src; } set { src = value; } }
        int size;
        public int Size { get { return size; } set { size = value; } }
    }
}
