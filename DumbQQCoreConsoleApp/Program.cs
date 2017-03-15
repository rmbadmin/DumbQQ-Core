﻿using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DumbQQ.Client;
using HttpResponse = System.Net.Http.HttpResponseMessage;


namespace DumbQQCoreConsoleApp
{
    public class Program
    {
        private static readonly DumbQQClient Client = new DumbQQClient { CacheTimeout = TimeSpan.FromDays(1) };
        private const string CookiePath = "dump.json";

        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // 好友消息回调
            Client.FriendMessageReceived += (sender, message) =>
            {
                var s = message.Sender;
                Console.WriteLine($"{s.Alias ?? s.Nickname}:{message.Content}");
            };
            // 群消息回调
            Client.GroupMessageReceived += (sender, message) =>
            {
                var s = message.Sender;
                Console.WriteLine($"[{message.Group.Name}]{s.Alias ?? s.Nickname}:{message.Content}");
            };
            // 讨论组消息回调
            Client.DiscussionMessageReceived += (sender, message) =>
            {
                Console.WriteLine($"[{message.Discussion.Name}]{message.Sender.Nickname}:{message.Content}");
            };
            // 消息回显
            Client.MessageEcho += (sender, e) =>
            {
                Console.WriteLine($"{e.Target.Name}>{e.Content}");
            };
            if (File.Exists(CookiePath))
            {
                // 尝试使用cookie登录
                if (Client.Start(File.ReadAllText(CookiePath)) != DumbQQClient.LoginResult.Succeeded)
                {
                    // 登录失败，退回二维码登录
                    QrLogin();
                }
            }
            else
            {
                QrLogin();
            }
            Console.WriteLine($"Login Success，{Client.Nickname}!");
            // 导出cookie
            try
            {
                File.WriteAllText(CookiePath, Client.DumpCookies());
            }
            catch
            {
                // Ignored
            }
            // 防止程序终止
            while (Client.Status == DumbQQClient.ClientStatus.Active)
            {
            }
        }

        private static void QrLogin()
        {
            while (true)
            {
                switch (Client.Start(array =>
                {
                    using (MemoryStream ms = new MemoryStream(array))
                    {
                        Bitmap bmp = new Bitmap(Image.FromStream(ms));
                        Console.WriteLine(QrChar(bmp, 5, 5));
                        Console.WriteLine("二维码已打印在屏幕，请反选二维码区域在使用手机扫描。");
                    }
                }))
                {
                    case DumbQQClient.LoginResult.Succeeded:
                        return;
                    case DumbQQClient.LoginResult.QrCodeExpired:
                        continue;
                    default:
                        Console.WriteLine("登录失败，需要重试吗？(y/n)");
                        var response = Console.ReadLine();
                        if (response.Contains("y"))
                        {
                            continue;
                        }
                        Environment.Exit(1);
                        return;
                }
            }
        }

        /// <summary>
        /// 解析二维码成字符
        /// </summary>
        /// <param name="bmp">图片</param>
        /// <param name="RowSize">二维码每格占几像素 宽</param>
        /// <param name="ColSize">二维码每格占几像素 高</param>
        /// <returns></returns>
        private static string QrChar(Bitmap bmp, int RowSize, int ColSize)
        {
            StringBuilder stringBuilder = new StringBuilder();
            char[] array = new char[]
            {
                '　','▇'
            };
            int height = bmp.Height;
            int width = bmp.Width;
            for (int i = 0; i < height / RowSize; i++)
            {
                int num = i * RowSize;
                for (int j = 0; j < width / ColSize; j++)
                {
                    int num2 = j * ColSize;
                    float num3 = 0f;
                    for (int k = 0; k < RowSize; k++)
                    {
                        for (int l = 0; l < ColSize; l++)
                        {
                            try
                            {
                                num3 += bmp.GetPixel(num2 + l, num + k).GetBrightness();
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                num3 += 0f;
                            }
                        }
                    }
                    num3 /= (float)(RowSize * ColSize);
                    int num4 = (int)(num3 * (float)array.Length);
                    if (num4 == array.Length)
                    {
                        num4--;
                    }
                    stringBuilder.Append(array[array.Length - 1 - num4]);
                }
                stringBuilder.Append("\r\n");
            }
            return stringBuilder.ToString();
        }
    }
}