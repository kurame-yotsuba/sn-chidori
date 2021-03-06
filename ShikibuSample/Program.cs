﻿using System;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu.Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Sample();
        }

        public static void Sample()
        {
            var sch = new TimeActionScheduler();
            var ta1 = sch.Add(() => Console.WriteLine(DateTime.Now), TimeSpan.FromSeconds(1));
            var ta2 = sch.Add(() => Console.WriteLine("Hello"), TimeSpan.FromSeconds(5));
            var ta3 = sch.Add(() => throw new Exception(), DateTime.Now.AddSeconds(30));
            Task schTask = sch.Start();

            Console.ReadLine();
        }

        public static void Alert()
        {
            var scheduler = new TimeActionScheduler();
            TimeAction timeAction = new(() =>
            {
                Console.Beep();
                Console.WriteLine("キーを入力してください。");
                string key = Console.ReadLine();
                Console.WriteLine($"{key}が入力されました。");
                if (key == "q") { scheduler.EndImmediately(); }
            }, TimeSpan.FromMinutes(20))
            {
                AdditionType = RepeatAdditionType.AfterExecute,
            };
            scheduler.Add(timeAction);

            scheduler.Start().Wait();
        }
    }
}