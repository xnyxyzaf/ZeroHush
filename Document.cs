namespace PixelStudio;

public class Document
{
    public string Title { get; set; }
    public CanvasEngine Canvas { get; }
    public HistoryManager History { get; }
    public string? FilePath { get; set; }

    public Document(int width, int height, string title)
    {
        Title = title;
        Canvas = new CanvasEngine(width, height);
        History = new HistoryManager();
    }
}