using AngleSharp;
using AngleSharp.Dom;
using System.Text.RegularExpressions;

public class Article
{
  public string? Title {get; set;}
  public string[]? Categories {get; set;}
  public int Comments {get; set;}
  public int Images {get; set;}
  public string ?Content {get; set;}
  public DateTime Date {get; set;}

  public List<string>? Urls {get; set;}

  public static string[] GetCategories(IDocument document)
  {
    var category_div = document.GetElementsByClassName("tag-list");
    if(category_div.Length == 0) 
    {
      return new string[0];
    }

  
    bool first = true;
    List<string> category_list = new();
    foreach(var category_element in category_div[0].Children) 
    {
      if(first)
      {
        first = false;
      } 
      else 
      {
      string category = category_element.TextContent;
      category_list.Add(category);
      }
    }

    return category_list.ToArray<string>();
  }

  public static string GetTitle(IDocument document)
  {
    var title_elements = document.GetElementsByTagName("h1");
    if(title_elements.Length == 0) 
    {
      throw new Exception("title not found"); 
    }
    
    return title_elements[0].TextContent;
  }

  public static int GetComments(IDocument document)
  {
    var span_elements = document.GetElementsByTagName("span");
    foreach(var span_element in span_elements)
    {
      if(span_element.TextContent.Contains("příspěvků"))
      {
        var match = Regex.Match(span_element.TextContent, @"\((\d+)");
        if (match.Success)
        {
            int number = int.Parse(match.Groups[1].Value);
            return number;
        }
      }
    }
  
    return 0;
  }

  public static int GetImages(IDocument document)
  {
    var image_elements = document.GetElementsByTagName("img");
    return image_elements.Length;
  }

  public static DateTime GetDate(IDocument document)
  {
    var date_elements = document.GetElementsByClassName("time-date");
    if(date_elements.Length == 0 || date_elements == null)
    {
      throw new Exception("date not found");
    }

    string date_content = date_elements[0].GetAttribute("content")!;
    if(date_content == null)
    {
      throw new Exception("date not found");
    }

    return DateTime.Parse(date_content);
  }

  public static string GetContent(IDocument document)
  {
    var content_element = document.GetElementById("art-text");
    if(content_element == null)
    {
      throw new Exception("content not found"); 
    }
    var p_elements = content_element.GetElementsByTagName("p");
    string content = ""; 
    foreach(var p in p_elements)
    {
      content += p.TextContent + "\n";
    }

    return content;
  }

  public static List<string> GetUrls(IDocument document)
  {
    var url_elements = document.GetElementsByTagName("a");
    List<string> urls = new(); 
    foreach(var e in url_elements) {
      string? url = e.GetAttribute("href");
      if(url != null && url.Contains("www.idnes.cz") && !url.Contains(".jpg")&& !url.Contains("/premium/") && !url.Contains("/ucet/") && url.Contains("https://") && url.Count(c => c == '/') > 3)
      {
        if(url.Contains('?'))
        {
          int index = url.IndexOf('?');
          url = url.Substring(0, index);
        }
        urls.Add(url); 
      }
    }

    return urls;
  }

  public async static Task<Article> FromHtmlAsync(string html)
  {
    Article article = new Article();
    
    var config = Configuration.Default;
    var context = BrowsingContext.New(config);

    var document = await context.OpenAsync(req => req.Content(html).Header("Content-Type", "text/html; charset=utf-8"));
    
    article.Title = GetTitle(document);
    article.Categories = GetCategories(document);
    article.Comments = GetComments(document);
    article.Images = GetImages(document);
    article.Content = GetContent(document);
    article.Date = GetDate(document);
    article.Urls = GetUrls(document);

    return article;
  }
  
  public static async Task<string[]> GetIdnesUrls(string html)
  {
    var config = Configuration.Default;
    var context = BrowsingContext.New(config);
    
    var document = await context.OpenAsync(req => req.Content(html).Header("Content-Type", "text/html; charset=utf-8"));
        
    var url_elements = document.GetElementsByTagName("a");
    List<string> urls = new(); 
    foreach(var e in url_elements) {
      string? url = e.GetAttribute("href");
      if(url != null && url.Contains("www.idnes.cz") 
          && !url.Contains(".jpg")
          && !url.Contains(".JPG")
          && !url.Contains("/databanka")
          && !url.Contains("/premium/") 
          && !url.Contains("/foto") 
          && !url.Contains("/databanka") 
          && !url.Contains("online-") 
          && !url.Contains("/ucet/") 
          && !url.EndsWith("/diskuse") 
          && url.Length > 70
          && url.Contains("https://"))
      {
        if(url.Contains('?'))
        {
          int index = url.IndexOf('?');
          url = url.Substring(0, index);
        }
        urls.Add(url);
      } 
    }

    return urls.ToArray();
  }
}
