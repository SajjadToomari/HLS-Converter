using Serilog;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly object _lockObject = new();
    private readonly TimeSpan _startTime = new TimeSpan(22, 0, 0);
    private readonly TimeSpan _endTime = new TimeSpan(9, 0, 0);

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            //if (DateTime.Now.IsHourBetween(_startTime, _endTime))
            {
                //try
                //{
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                #region Get Folders

                _logger.LogInformation("getting folders", DateTimeOffset.Now);

                var ffmpegLocation = _configuration.GetSection("Folders:FFMPEGFolder").Value;

                _logger.LogInformation($"ffmpegLocation: {ffmpegLocation}", DateTimeOffset.Now);

                var videoPath = _configuration.GetSection("Folders:VideoFolder").Value;

                _logger.LogInformation($"videoPath: {videoPath}", DateTimeOffset.Now);

                var hlsPath = _configuration.GetSection("Folders:HlsFolder").Value;

                _logger.LogInformation($"hlsPath: {hlsPath}", DateTimeOffset.Now);

                var thread = int.Parse(_configuration.GetSection("Thread").Value);

                _logger.LogInformation($"thread: {thread}", DateTimeOffset.Now);

                #endregion

                #region Create Hls Path

                _logger.LogInformation("Creating folders if not exists", DateTimeOffset.Now);

                if (!Directory.Exists(hlsPath))
                {
                    Directory.CreateDirectory(hlsPath);
                }

                #endregion

                #region Get Video

                _logger.LogInformation("Getting videos", DateTimeOffset.Now);

                var videos = Directory.GetFiles(videoPath, "*.mp4", SearchOption.TopDirectoryOnly);

                _logger.LogInformation($"All MP4 Files : {videos.Length}", DateTimeOffset.Now);

                var hlsVideos = Directory.GetFiles(hlsPath, "*.m3u8", SearchOption.TopDirectoryOnly);

                _logger.LogInformation($"All HLS Files : {hlsVideos.Length}", DateTimeOffset.Now);

                #endregion

                #region Start Proccessing

                var tasks = new List<Task>();

                var index = 0;

                await Parallel.ForEachAsync(videos, new ParallelOptions { MaxDegreeOfParallelism = thread, CancellationToken = stoppingToken }, async (video, ct) =>
                {
                    //if (!DateTime.Now.IsHourBetween(_startTime, _endTime) || ct.IsCancellationRequested)
                    //{
                    //    return;
                    //}

                    var thisindex = 0;

                    lock (_lockObject)
                    {
                        index++;
                        thisindex = index;

                        _logger.LogInformation($"Starting Video Number {index}", DateTimeOffset.Now);

                        var videoName = video.Split("/")[^1].Split(".")[0];

                        //the video is proccesed skipping...
                        if (File.Exists($"{hlsPath}/{videoName}/video.m3u8"))
                        {
                            _logger.LogInformation($"Skipping Video Number {index} because it's converted before", DateTimeOffset.Now);
                            return;
                        }
                    }

                    try
                    {
                        #region Sample Command

                        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 640x360 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/360_out.m3u8
                        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 800x480 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/480_out.m3u8
                        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 1280x720 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/720_out.m3u8
                        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 1920x1080 -start_number 0 -hls_time 10 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/1080_out.m3u8

                        #endregion

                        var videoName = video.Split("/")[^1].Split(".")[0];

                        //ffmpeg commands for converting to hls
                        var commands = new string[]
                        {
                        $"-i {video} -profile:v baseline -level 3.0 -s 800x480 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}/{videoName}/480_out.m3u8",
                        $"-i {video} -profile:v baseline -level 3.0 -s 1280x720 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}/{videoName}/720_out.m3u8",
                        $"-i {video} -profile:v baseline -level 3.0 -s 1920x1080 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}/{videoName}/1080_out.m3u8"
                        };

                        Directory.CreateDirectory($"{hlsPath}/{videoName}");

                        var isAllDoneSuccessfully = true;

                        //starting proccess
                        foreach (var command in commands)
                        {
                            //if (!DateTime.Now.IsHourBetween(_startTime, _endTime) || ct.IsCancellationRequested)
                            //{
                            //    isAllDoneSuccessfully = false;
                            //    break;
                            //}
                            //else
                            //{
                            var startInfo = new ProcessStartInfo($"{ffmpegLocation}/ffmpeg", "-y " + command);

                            startInfo.CreateNoWindow = true;
                            startInfo.RedirectStandardInput = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.UseShellExecute = false;
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                            var process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true/*, PriorityClass = ProcessPriorityClass.BelowNormal*/ };

                            var exitCode = await Extension.WaitForExitAsync(process, true, (exitCode) => { }, ct);

                            if (exitCode != 0)
                            {
                                isAllDoneSuccessfully = false;
                                break;
                            }
                            //}
                        }

                        //creating final m3u8 file
                        if (isAllDoneSuccessfully)
                        {

                            //#EXTM3U
                            //#EXT-X-STREAM-INF:BANDWIDTH=375000,RESOLUTION=640x360
                            //360_out.m3u8
                            //#EXT-X-STREAM-INF:BANDWIDTH=750000,RESOLUTION=854x480
                            //480_out.m3u8
                            //#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=1280x720
                            //720_out.m3u8
                            //#EXT-X-STREAM-INF:BANDWIDTH=3500000,RESOLUTION=1920x1080
                            //1080_out.m3u8

                            using var streamWriter = new StreamWriter($"{hlsPath}/{videoName}/video.m3u8", false, Encoding.UTF8);
                            await streamWriter.WriteLineAsync("#EXTM3U");
                            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=750000,RESOLUTION=854x480");
                            await streamWriter.WriteLineAsync("480_out.m3u8");
                            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=1280x720");
                            await streamWriter.WriteLineAsync("720_out.m3u8");
                            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=3500000,RESOLUTION=1920x1080");
                            await streamWriter.WriteLineAsync("1080_out.m3u8");
                            await streamWriter.FlushAsync();
                            streamWriter.Close();
                        }
                        else
                        {
                            //delete because proccess done with error
                            if (Directory.Exists($"{hlsPath}/{videoName}"))
                            {
                                Directory.Delete($"{hlsPath}/{videoName}", true);
                            }
                        }

                        if (isAllDoneSuccessfully)
                        {
                            _logger.LogInformation($"End video number {thisindex}", DateTimeOffset.Now);

                        }
                        else
                        {
                            //if (!DateTime.Now.IsHourBetween(_startTime, _endTime) || ct.IsCancellationRequested)
                            //{
                            //    _logger.LogInformation($"Skip video number {thisindex} because it's out of running time", DateTimeOffset.Now);
                            //}
                            //else
                            //{
                            _logger.LogInformation($"End video number {thisindex} with error", DateTimeOffset.Now);
                            //}
                        }

                    }
                    catch (Exception ex)
                    {
                        var st = new StackTrace(ex, true);
                        var frame = st.GetFrame(0);
                        var line = frame.GetFileLineNumber();
                        var file = frame.GetFileName();
                        var method = frame.GetMethod();

                        _logger.LogError(ex.Message + ";;;" + ex.InnerException?.Message + ";;;" + ex.InnerException?.InnerException?.Message + "\r\n" + $"{file}:{method}:{line}");

                        var videoName = video.Split("/")[^1].Split(".")[0];

                        if (Directory.Exists($"{hlsPath}/{videoName}"))
                        {
                            Directory.Delete($"{hlsPath}/{videoName}", true);
                        }
                    }

                });

                #endregion
                //}
                //catch (Exception ex)
                //{
                //    Log.Error("Error at worker: ", ex);
                //    //var st = new StackTrace(ex, true);
                //    //var frame = st.GetFrame(0);
                //    //var line = frame.GetFileLineNumber();
                //    //var file = frame.GetFileName();
                //    //var method = frame.GetMethod();

                //    //_logger.LogError(ex.Message + ";;;" + ex.InnerException?.Message + ";;;" + ex.InnerException?.InnerException?.Message + "\r\n" + $"{file}:{method}:{line}");

                //    await Task.Delay(5000);
                //}
            }
            await Task.Delay(5000);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}