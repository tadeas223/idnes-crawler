using System.Text.Json;

public class ArticleCrawler
{
  private List<string> visited_urls = new();
  private List<Article> loaded_articles = new();
  private FileStream stream;
  private Utf8JsonWriter writer;
  private UInt64 max_size;
  private int max_tasks;
  private string initial_url;
  private Log log;
  
  private Object visted_urls_lock = new Object();
  public ArticleCrawler(string initial_url, string file_path, UInt64 max_size, int max_tasks, Log log)
  {
    this.log = log;

    stream = new FileStream(file_path, FileMode.Create);
    writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
    {
      Indented = true,
      Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    this.max_size = max_size;
    this.max_tasks = max_tasks;
    this.initial_url = initial_url;
  }

  public async Task Run() 
  {
    await CrawlPrepareAsync();

    await CrawlAsync();
  }

  private async Task CrawlPrepareAsync()
  {
    writer.WriteStartArray();
    
    Article article = await LoadArticleAsync(initial_url);
    loaded_articles.Add(article);
    WriteArticle(article);
  }

  private async Task EndAsync()
  {
    writer!.WriteEndArray();
    await writer.DisposeAsync();
    await stream.DisposeAsync();
    log.WriteLine("finished", 1);
  }

  private async Task CrawlAsync() {
    List<Article> newly_loaded_articles = new();

    while((UInt64)stream.Length < max_size)
    {
      foreach(Article article in loaded_articles)
      {
        if(article.Title == "failed") continue;
        
        log.WriteLine("\n" + article.Title!, 2);

        List<string> urls = article.Urls!;
        List<string> new_urls = new();

        foreach(string url in urls)
        {
          if(!visited_urls.Contains(url))
          {
            new_urls.Add(url);
          }
        }

        if(new_urls.Count == 0)
        {
          continue;
        }
        
        await Task.Delay(3000);
        Article[] new_articles = await LoadArticlesAsync(new_urls.ToArray<string>());
        
        int found_good_articles = 0;
        foreach(Article new_article in new_articles)
        {
          if(new_article.Title == "failed") continue;
          
          found_good_articles++;

          log.WriteLine("    -> " + new_article.Title!, 2);
          WriteArticle(new_article);

          if((UInt64)stream.Length > max_size)
          {
            await EndAsync();
            return;
          }
        }

        newly_loaded_articles.AddRange(new_articles);
        log.WriteLine("\nfile size: " + ((float)stream.Length / 1024f / 1024f / 1024f) + " Gb\n", 1);
        log.WriteLine("all links " + visited_urls.Count, 3);
        log.WriteLine("currently found articles " + found_good_articles, 3);
      }


      loaded_articles.Clear();
      loaded_articles.AddRange(newly_loaded_articles);
    }
  }

  private async Task<Article[]> LoadArticlesAsync(string[] urls)
  {
    SemaphoreSlim semaphore = new SemaphoreSlim(max_tasks);
    var tasks = urls.Select(async url =>
    {
      await semaphore.WaitAsync();
      Article article; 
      try
      {
        article = await LoadArticleAsync(url);
      }
      catch 
      {
        article = new Article {Title = "failed"};
      }

      semaphore.Release();
      
      return article;
    });

    Article[] articles = await Task.WhenAll(tasks);
    return articles;
  }

  private async Task<Article> LoadArticleAsync(string url)
  {
    bool contains_url = false;
    lock(visted_urls_lock)
    {
      if(visited_urls.Contains(url))
      {
        contains_url = true;
      }
    }
    
    Article article = new();
    if(contains_url)
    {
      article.Title = "failed";  
    }
    else
    {
      try
      {
        article = await GetArticleAsync(url);
      } catch {
        article.Title = "failed";  
      }
    }
    
    lock(visted_urls_lock)
    {
      visited_urls.Add(url);
    }
    return article;
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
        writer.Flush();
        stream.Flush();
  }
  
    private async Task<Article> GetArticleAsync(string url)
  {
    HttpRetriever retriever = new HttpRetriever("https://idnes.cz");
    string html = await retriever.GetHtmlAsync(url);
    log.WriteLine("---------------html-------------------", 10);
    log.WriteLine(html, 10);
    log.WriteLine("---------------html-------------------", 10);
    return await Article.FromHtmlAsync(html);
  }
}
