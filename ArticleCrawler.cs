using System.Text.Json;
using System.Collections.Concurrent;

public class ArticleCrawler
{
  private int written = 0;
  
  private SemaphoreSlim semaphore;

  private ConcurrentDictionary<string, byte> visited_urls = new();
  private ConcurrentDictionary<string, byte> will_visit_urls = new();

  public int MaxDepthSize {get;set;}
  public int MaxTasks {get; set;}
  public UInt64 MaxSize {get; set;}
  public string InitialUrl {get;set;}
  public Log Log {get;set;}

  private List<Article> loaded_articles = new();
  private FileStream stream;
  private Utf8JsonWriter writer;
  
  private Object visted_urls_lock = new Object();
  public ArticleCrawler(string initial_url, string file_path)
  {
    InitialUrl = initial_url;

    stream = new FileStream(file_path, FileMode.Create);
    writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
    {
      Indented = true,
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    MaxTasks= 10;
    MaxSize = 5 * 1024 * 1024; // 5 Mb default size
    Log = new Log(0);
    MaxDepthSize = 500;

    semaphore = new SemaphoreSlim(MaxTasks);
  }

  public async Task Run() 
  {
    CrawlPrepare();

    await CrawlAsync();
  }

  private void CrawlPrepare()
  {
    writer.WriteStartArray();
    will_visit_urls.TryAdd(InitialUrl, 0);
  }

  private async Task CrawlAsync()
  {
    Object write_lock = new Object();
    while((UInt64)stream.Length < MaxSize && will_visit_urls.Count > 0)
    {
      var tasks = will_visit_urls.Select(async url =>
      {
        await semaphore.WaitAsync();
        await Task.Delay(Random.Shared.Next(500, 1000));
        byte uaoi;
        will_visit_urls.Remove(url.Key, out uaoi);

        try
        {
          if((UInt64)stream.Length > MaxSize)
          {
            return;
          }

          HttpRetriever retriever = new("https://www.idnes.cz");
          
          string html;
          try
          {
            html = await retriever.GetHtmlAsync(url.Key);
          }
          catch
          {
            throw new Exception("failed to load url");
          }

          List<string> next_urls = new();
          try
          {
            next_urls = (await Article.GetIdnesUrls(html)).ToList();
          }
          catch
          {

          }
          
          foreach(string next_url in next_urls)
          {
            if(will_visit_urls.Count <= MaxDepthSize)
            {
              will_visit_urls.TryAdd(next_url, 0);
            }
          }
          
          try
          {
            Article article = await Article.FromHtmlAsync(html);
            lock(write_lock)
            {
              try
              {
                WriteArticle(article);
                written++;
              }
              catch
              {
                
              }
            }
              
          }
          catch
          {

          }          

          visited_urls.TryAdd(url.Key, 0);
          
          Log.WriteLine(url.Key, 2);
          Log.WriteLine("url pool: " + will_visit_urls.Count, 2);
          Log.WriteLine("written: " + written, 2);
          Log.WriteLine("visited urls: " + visited_urls.Count, 2);
          Log.WriteLine("file size: " + ((float)stream.Length / 1024f / 1024f / 1024f) + " GB", 2);
        }
        catch {
          Log.WriteLine("BAD ERROR", 1);
          return;
        }
        finally
        {
          if(written % 500 == 0) {
            writer.Flush();
            stream.Flush();
          }
          semaphore.Release();
        }
      });
    
      await Task.WhenAll(tasks);
    }
    
    await EndAsync();
  }

  private async Task EndAsync()
  {
    writer!.WriteEndArray();
    await writer.DisposeAsync();
    await stream.DisposeAsync();
    Log.WriteLine("finished", 1);
  }

  private void WriteArticle(Article article)
  {
        writer.WriteStartObject();

        if (article.Title != null)
            writer.WriteString("Title", article.Title);

        if (article.Categories != null)
        {
            writer.WriteStartArray("Categories");
            foreach (var cat in article.Categories)
            {
                writer.WriteStringValue(cat);
            }
            writer.WriteEndArray();
        }

        writer.WriteNumber("Comments", article.Comments);
        writer.WriteNumber("Images", article.Images);

        if (article.Content != null)
            writer.WriteString("Content", article.Content);

        writer.WriteString("Date", article.Date.ToString("O"));

        writer.WriteEndObject();
  }
  
  private async Task<Article> GetArticleAsync(string url)
  {
    HttpRetriever retriever = new HttpRetriever("https://idnes.cz");
    string html = await retriever.GetHtmlAsync(url);
    Log.WriteLine("---------------html-------------------", 10);
    Log.WriteLine(html, 10);
    Log.WriteLine("---------------html-------------------", 10);
    return await Article.FromHtmlAsync(html);
  }

  private async Task<List<string>> GetUrlsAsync(string url)
  {
    HttpRetriever retriever = new HttpRetriever("https://idnes.cz");
    string html = await retriever.GetHtmlAsync(url);
    return (await Article.GetIdnesUrls(html)).ToList();
  }

}
