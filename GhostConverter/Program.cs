using System;

namespace GhostConverter
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            // Example
            // args[0] = @"C:\input\export.ghost.json";
            // args[1] = @"C:\output\posts";
            
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Input filename and output location parameters are required");
                return;
            }

            Directory.CreateDirectory(args[1]);

            using(var stream = File.OpenRead(args[0]))
            using(var txt = new StreamReader(stream))
            using(var reader = new JsonTextReader(txt))
            {
                var jObject = JObject.Load(reader);

                // build tag list.
                var tags = jObject["db"].First["data"]["tags"].ToDictionary(tag => tag.Value<int>("id"), tag => tag.Value<string>("name"));

                var postTags = new Dictionary<int, List<int>>();
                foreach (var token in jObject["db"].First["data"]["posts_tags"])
                {
                    var postId = token.Value<int>("post_id");
                    var tagId = token.Value<int>("tag_id");
                    if (!postTags.ContainsKey(postId))
                    {
                        postTags.Add(postId, new List<int>());
                    }

                    postTags[postId].Add(tagId);
                }


                foreach (JToken post in jObject["db"].First["data"]["posts"])
                {
                    var filename = Path.Combine(args[1], post.Value<string>("slug") + ".md");
                    Console.WriteLine("Creating " + filename);
                    using (var mdFile = File.CreateText(filename))
                    {
                        var postId = post.Value<int>("id");
                        mdFile.Write("Title: ");
                        mdFile.WriteLine(post.Value<string>("title"));
                        mdFile.Write("Created: ");
                        mdFile.WriteLineAsync(DateTimeOffset.FromUnixTimeMilliseconds(post.Value<long>("created_at")).ToString("O"));

                        var published = post.Value<long?>("published_at");
                        if (published.HasValue)
                        {
                            var pubDate = DateTimeOffset.FromUnixTimeMilliseconds(published.Value);
                            mdFile.WriteLine("Published: {0:O}", pubDate);
                        }
                        

                        if (postTags.ContainsKey(postId) && postTags[postId].Any())
                        {
                            mdFile.WriteLine("Tags: ");
                            foreach (var tagId in postTags[postId])
                            {
                                mdFile.WriteLine(" - {0}", tags[tagId]);
                            }
                        }

                        mdFile.WriteLine("---");
                        mdFile.Write(post.Value<string>("markdown"));
                    }
                }
            }

#if DEBUG
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
#endif
        }
    }
}
