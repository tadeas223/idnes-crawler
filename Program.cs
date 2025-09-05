ArticleCrawler crawler = new("https://www.idnes.cz/zpravy/revue/zajimavosti/siamska-dvojcata-abby-a-brittany-hensel-miminko-spekulace-manzel.A250902_082153_zajimavosti_potu", "test.json", (UInt64)2147483648, 64, new Log(3));
await crawler.Run();

