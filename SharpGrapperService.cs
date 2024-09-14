using CodeMechanic.Types;
using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;

public class SharpGrapperService
{
    private static readonly HttpClient Client = new HttpClient();
    private static readonly HashSet<string> TempFiles = new HashSet<string>();

    private static readonly IMultiGrabber _grabber = GrabberBuilder.New()
        .UseDefaultServices()
        .AddVimeo()
        .AddYouTube()
        .Build();

    public async Task Download(string url = "")
    {
        if (url.IsEmpty()) return;

        try
        {
            Console.WriteLine("Enter FFMPEG path:");
            var ffmpegPath = Environment.GetEnvironmentVariable("YOUTUBE_VAULT_ROOT") ??
                             "/home/nick/Downloads/VideoDownloader";

            FFmpeg.AutoGen.ffmpeg.RootPath = ffmpegPath;

            Console.WriteLine($"grabbing url '{url} ...");
            var result = await _grabber.GrabAsync(new Uri(url, UriKind.Absolute));

            var audioStream = ChooseMonoMedia(result, MediaChannels.Audio);
            var videoStream = ChooseMonoMedia(result, MediaChannels.Video);

            if (audioStream == null)
                throw new InvalidOperationException("No audio stream detected.");
            if (videoStream == null)
                throw new InvalidOperationException("No video stream detected.");

            Console.WriteLine($"Downloading audio to path '{ffmpegPath}'");
            var audioPath = await DownloadMedia(audioStream, result);
            var videoPath = await DownloadMedia(videoStream, result);
            GenerateOutputFile(audioPath, videoPath, videoStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
        finally
        {
            foreach (var tempFile in TempFiles)
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    private static void GenerateOutputFile(string audioPath, string videoPath, GrabbedMedia videoStream)
    {
        Console.WriteLine("Output Path:");
        var outputPath = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new Exception("No output path is specified.");
        var merger = new MediaMerger(outputPath);
        merger.AddStreamSource(audioPath, MediaStreamType.Audio);
        merger.AddStreamSource(videoPath, MediaStreamType.Video);
        merger.OutputMimeType = videoStream.Format.Mime;
        merger.OutputShortName = videoStream.Format.Extension;
        merger.Build();
        Console.WriteLine($"Output file successfully created.");
    }

    private static GrabbedMedia ChooseMonoMedia(GrabResult result, MediaChannels channel)
    {
        var resources = result.Resources<GrabbedMedia>()
            .Where(m => m.Channels == channel)
            .ToList();

        if (resources.Count == 0)
            return null;

        for (var i = 0; i < resources.Count; i++)
        {
            var resource = resources[i];
            Console.WriteLine($"{i}. {resource.Title ?? resource.FormatTitle ?? resource.Resolution}");
        }

        while (true)
        {
            Console.Write($"Choose the {channel} file: ");
            var choiceStr = Console.ReadLine();
            if (!int.TryParse(choiceStr, out var choice))
            {
                Console.WriteLine("Number expected.");
                continue;
            }

            if (choice < 0 || choice >= resources.Count)
            {
                Console.WriteLine("Invalid number.");
                continue;
            }

            return resources[choice];
        }
    }

    private static async Task<string> DownloadMedia(GrabbedMedia media, IGrabResult grabResult)
    {
        Console.WriteLine("Downloading {0}...", media.Title ?? media.FormatTitle ?? media.Resolution);
        using var response = await Client.GetAsync(media.ResourceUri);
        response.EnsureSuccessStatusCode();
        using var downloadStream = await response.Content.ReadAsStreamAsync();
        using var resourceStream = await grabResult.WrapStreamAsync(downloadStream);
        var path = Path.GetTempFileName();

        using var fileStream = new FileStream(path, FileMode.Create);
        TempFiles.Add(path);
        await resourceStream.CopyToAsync(fileStream);
        return path;
    }

    private static async Task Grab(Uri uri)
    {
        Console.WriteLine($"Grabbing from {uri}...");
        var grabResult = await _grabber.GrabAsync(uri).ConfigureAwait(false);

        var reference = grabResult.Resource<GrabbedHlsStreamReference>();
        if (reference != null)
        {
            // Redirect to an M3U8 playlist
            await Grab(reference.ResourceUri);
            return;
        }

        var metadataResources = grabResult.Resources<GrabbedHlsStreamMetadata>().ToArray();
        if (metadataResources.Length > 0)
        {
            // Description for one or more M3U8 playlists
            GrabbedHlsStreamMetadata selection;
            if (metadataResources.Length == 1)
            {
                selection = metadataResources.Single();
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("=== Streams ===");
                for (var i = 0; i < metadataResources.Length; i++)
                {
                    var res = metadataResources[i];
                    Console.WriteLine("{0}. {1}", i + 1, $"{res.Name} {res.Resolution}");
                }

                Console.Write("Select a stream: ");
                selection = metadataResources[int.Parse(Console.ReadLine()) - 1];
            }

            // Get information from the HLS stream
            var grabbedStream = await selection.Stream.Value;
            await Grab(grabbedStream, selection, grabResult);
            return;
        }

        throw new Exception("Could not grab the HLS stream.");
    }

    private static async Task Grab(GrabbedHlsStream stream, GrabbedHlsStreamMetadata metadata, GrabResult grabResult)
    {
        Console.WriteLine();
        Console.WriteLine("=== Downloading ===");
        Console.WriteLine("{0} segments", stream.Segments.Count);
        Console.WriteLine("Duration: {0}", stream.Length);

        var tempFiles = new List<string>();
        try
        {
            for (var i = 0; i < stream.Segments.Count; i++)
            {
                var segment = stream.Segments[i];
                Console.Write($"Downloading segment #{i + 1} {segment.Title}...");
                var outputPath = Path.GetTempFileName();
                tempFiles.Add(outputPath);
                using var responseStream = await Client.GetStreamAsync(segment.Uri);
                using var inputStream = await grabResult.WrapStreamAsync(responseStream);
                using var outputStream = new FileStream(outputPath, FileMode.Create);
                await inputStream.CopyToAsync(outputStream);
                Console.WriteLine(" OK");
            }

            CreateOutputFile(tempFiles, metadata);
        }
        finally
        {
            foreach (var tempFile in tempFiles)
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }

            Console.WriteLine("Cleaned up temp files.");
        }
    }

    private static void CreateOutputFile(List<string> tempFiles, GrabbedHlsStreamMetadata metadata)
    {
        Console.WriteLine("All segments were downloaded successfully.");
        Console.Write("Enter a path for the output file: ");
        var outputPath = Console.ReadLine();
        var concatenator = new MediaConcatenator(outputPath)
        {
            OutputMimeType = metadata.OutputFormat.Mime,
            OutputExtension = metadata.OutputFormat.Extension,
        };
        foreach (var tempFile in tempFiles)
            concatenator.AddSource(tempFile);
        concatenator.Build();
        Console.WriteLine("Output file created successfully!");
    }
}