﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tinder.Scoring;

namespace Tinder.AutoSwipper
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            var tinderAuthToken = config.GetValue<string>("TinderClient:Token"); // X-Auth-Token
            if (string.IsNullOrEmpty(tinderAuthToken) || !Guid.TryParse(tinderAuthToken, out Guid tinderAuthTokenGuid))
            {
                throw new Exception("TinderClient:Token is missing in appsettings.json or the token is malformed");
            }     

            var tinderClient = new TinderClient(tinderAuthTokenGuid);
            var serviceProvider = new ServiceCollection()
                .AddLogging(l => l.AddFilter("Microsoft", LogLevel.Debug)
                    .AddFilter("System", LogLevel.Debug)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddDebug())
                .AddSingleton<ITinderClient>(tinderClient)
                .AddSingleton<IScoring, ScoringAgent>()
                .AddTransient<AutoSwipper>()
                .BuildServiceProvider();

            var autoSwipper = serviceProvider.GetService<AutoSwipper>();
            await autoSwipper.ExecuteAsync();

            Console.WriteLine("Press any key to exit ...");
            Console.ReadLine();
        }
    }
}
