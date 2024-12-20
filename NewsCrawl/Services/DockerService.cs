using System;
using System.Diagnostics;

namespace NewsCrawl.Services
{
    public class DockerService : BackgroundService
    {

        //projenin dışarıdan müdahale gerektirmeden dockerı kendi ayağa kaldırması için bir servis hazırlayıp, program.cs içerisinde çağırıyoruz
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "compose up -d",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
            });
        }
    }
}

