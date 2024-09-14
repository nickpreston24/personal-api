using CodeMechanic.Types;

public class VideoHandler : Enumeration
{
    // YTDL: https://github.com/Bluegrams/YoutubeDLSharp
    public static VideoHandler YTDL =
        new VideoHandler(1, "Youtube-DL", github: "https://github.com/Bluegrams/YoutubeDLSharp");

    // SharpGrapper: https://github.com/dotnettools/SharpGrabber
    public static VideoHandler SharpGrapper =
        new VideoHandler(2, nameof(SharpGrapper), "https://github.com/dotnettools/SharpGrabber");

    //VideoLibrary : https://github.com/omansak/libvideo
    public static VideoHandler VideoLibrary =
        new VideoHandler(3, nameof(VideoLibrary), "https://github.com/omansak/libvideo");

    public VideoHandler(int id, string name, string github) : base(id, name)
    {
    }
}