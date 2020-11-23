using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsoleApp
{
    class Program
    {
        private const string APP_PATH = @"https://localhost:44319";
        static void Main(string[] args)
        {
            GetImages();
        } 

        static void GetImages()
        {
            bool isResponse = true;
            while(isResponse)
            {
                Console.WriteLine("Здравствуй, человек! С какого сайта желаешь скачать изображения?");
                string url = Console.ReadLine();
                Regex regexImg = new Regex(@"http.*://\w*\..*", RegexOptions.ExplicitCapture);
                MatchCollection matches = regexImg.Matches(url);
                if (matches.Count > 0)
                    Console.WriteLine("На сайт похоже)\n");

                int countImage = 0;
                while (countImage < 1)
                {
                    Console.WriteLine("Сколько изображений желаешь?");
                    if (!int.TryParse(Console.ReadLine(), out countImage))
                    {
                        Console.WriteLine("Неверное значение. Попробуй еще раз ввести число)");
                        countImage = 0;
                    }
                }

                int countThread = 0;
                while (countThread < 1)
                {
                    Console.WriteLine("На сколько потоков разбить скачивание для повышения скорости?");
                    if (!int.TryParse(Console.ReadLine(), out countThread))
                    {
                        Console.WriteLine("Неверное значение. Попробуй еще раз ввести число)");
                        countThread = 0;
                    }
                }

                int numberPort = 0;
                while (numberPort < 1000)
                {
                    Console.WriteLine("Подскажи мне, пожалуйста, номер порта");
                    if (!int.TryParse(Console.ReadLine(), out numberPort))
                    {
                        Console.WriteLine("Неверное значение. Попробуй еще раз ввести число)");
                        numberPort = 0;
                    }
                }

                //https://localhost:44319/api/values/?P1=qw&P2=https://techarks.ru/qa/rest/kak-peredat-neskolko-param-GZ/
                using (var client = new HttpClient())
                {
                    //var response = client.GetAsync(APP_PATH + @"/api/values").Result;
                    //var response = client.GetAsync(APP_PATH + @"/api/values/" + "?URL=https://zastavok.net/" + "&ThreadCount=2" + "&ImageCount=10").Result;
                    var response = client.GetAsync(APP_PATH + @"/api/values/" + $"?URL={url}" + $"&ThreadCount={countThread}" + $"&ImageCount={countImage}").Result;
                    var result = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(result);
                }
                Console.WriteLine("\nКонец. Желаешь отправить запрос еще раз? Нажми y.\nЕсли нет, то любую другую клавишу");
                if (!(Console.ReadLine() == "y" || Console.ReadLine() == "н"))
                {
                    Console.WriteLine("Пока");
                    isResponse = false;
                }
            }
        }
    }
}
