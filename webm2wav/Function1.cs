using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;

namespace webm2wavns
{
    public static class webm2wav
    {
        [FunctionName("webm2wav")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var temp = Path.GetTempPath() + Path.GetRandomFileName() + ".webm";
            var tempOut = Path.GetTempPath() + Path.GetRandomFileName() + ".wav";

            //log.LogInformation($"Temp In: {temp}");
            //log.LogInformation($"Temp Out: {tempOut}");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string webmstrdata = requestBody.Substring(23, requestBody.Length - 23);                  // remove "data:audio/webm;base64,"
                byte[] webmbytesdata = Convert.FromBase64String(webmstrdata);
                File.WriteAllBytes(temp, webmbytesdata);
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Exception1: " + ex.Message
                });
            }

            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = @"D:\home\site\ConvertAudioFormatUsingFFMpeg\ffmpeg.exe";
                psi.Arguments = $"-i \"{temp}\" \"{tempOut}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                //log.LogInformation($"Args: {psi.Arguments}");

                var process = Process.Start(psi);
                process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds);

                string stdoutput = process.StandardOutput.ReadToEnd();
                string stderror = process.StandardError.ReadToEnd();

                //log.LogInformation("FFMPEG: exitcode: " + process.ExitCode + "\r\n" + stdoutput + "\r\n" + stderror);
                //log.LogInformation("FFMPEG: stdoutput: " + stdoutput);
                //log.LogInformation("FFMPEG: stderror: " + stderror);

                if (process.ExitCode != 0)
                {
                    return new BadRequestObjectResult(new ProblemDetails
                    {
                        Status = 400,
                        Title = stderror
                    });
                }
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Status = 400,
                    Title = "Exception2: " + ex.Message
                });
            }

            var bytes = File.ReadAllBytes(tempOut);

            File.Delete(tempOut);
            File.Delete(temp);

            await Task.Run(() => { });

            return new FileContentResult(bytes, "audio/wav");

        }
    }
}
