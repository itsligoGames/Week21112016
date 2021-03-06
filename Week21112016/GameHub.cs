﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Reflection;
using System.IO;
using CsvHelper;
using System.Text;
using GameData;
using System.Timers;
using System.Threading.Tasks;

namespace Week21112016
{
    public static class GameState
    {
        public static List<PlayerData> Players = new List<PlayerData>();
        public static List<CollectableData> Collectables = new List<CollectableData>();
        public static int WorldX = 2000;
        public static int WorldY = 2000;
        public static TimeSpan countDown = new TimeSpan(0, 0, 0, 10);
        public static Timer TimeToStart = new Timer(1000);
        public static bool Started;

        public static void createCollectables()
        {
            int noOfCollectables = new Random().Next(5, 10);
            for (int i = 0; i < noOfCollectables; i++)
            {
                Collectables.Add(new CollectableData
                {
                    ACTION = COLLECTABLE_ACTION.DELIVERED,
                    collectableId = Guid.NewGuid().ToString(),
                    CollectableName = "chaser",
                    collectableValue = new Random().Next(0, 100),
                    X = new Random().Next(100, WorldX - 100),
                    Y = new Random().Next(100, WorldY - 100)
                });
            }
           
        }

        public static void StartTimer()
        {
            if (!Started)
            {
                TimeToStart.Elapsed += TimeToStart_Elapsed;
                TimeToStart.Start();
                Started = true;
            }
        }
        private static void TimeToStart_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (GameState.countDown.TotalSeconds > 0)
            {
                GameState.countDown = GameState.countDown.Subtract(new TimeSpan(0, 0, 0, 1));
            }
            else
            {
                GameState.TimeToStart.Stop();
            }

        }

    }

    public class GameHub : Hub
    {
        public GameHub() : base()
        {
            if (GameState.Players.Count() == 0)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "Week21112016.randomNameswithscores.csv";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        CsvReader csvReader = new CsvReader(reader);
                        csvReader.Configuration.HasHeaderRecord = false;
                        csvReader.Configuration.WillThrowOnMissingField = false;
                        GameState.Players = csvReader.GetRecords<PlayerData>().ToList();
                    }
                }
            }

            

        }

        public void Hello()
        {
            Clients.All.hello();
        }

        public override Task OnDisconnected(bool stopCalled)
        {

            return base.OnDisconnected(stopCalled);
        }

        public void join()
        {
            Clients.Caller.joined(GameState.WorldX, GameState.WorldY);
        }

        public void getCollectables()
        {
            if (GameState.Collectables.Count() == 0)
                GameState.createCollectables();

            foreach(CollectableData c in GameState.Collectables)
            {
                Clients.All.deliver(c);
            }

        }

        public void getPlayer(string FirstName, string SecondName)
        {
            var player = GameState.Players.FirstOrDefault(
                            p => p.FirstName == FirstName &&
                            p.SecondName == SecondName);
            if (player != null)
            {
                Clients.Caller.recievePlayer(player);
                
                if (!GameState.Started)
                    GameState.StartTimer();
                Clients.All.recieveCountDown(GameState.countDown.TotalSeconds);

            }
            else
                Clients.Caller.error(" Player does not exist "
                                           + FirstName + " " + SecondName);
        }

        public void getTime()
        {
            if (GameState.countDown.TotalSeconds > 0)
                Clients.All.recieveCountDown(GameState.countDown.TotalSeconds);
            else
                Clients.All.Start();


        }
    }
    
}