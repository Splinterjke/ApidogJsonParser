using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ApidogJsonParser
{
    class Program
    {
        private struct Header
        {
            [JsonProperty("v")]
            public string version;

            [JsonProperty("p")]
            public string p;

            [JsonProperty("a")]
            public string a;

            [JsonProperty("t")]
            public string Name;

            [JsonProperty("d")]
            public uint BaseData;
        }

        private struct MessageModel
        {
            [JsonProperty("i")]
            public uint MessageID;

            [JsonProperty("f")]
            public uint RecipientID;

            [JsonProperty("t")]
            public string Content;

            [JsonProperty("d")]
            public uint DateOffset;

            [JsonProperty("a")]
            public object[] Attachments;
        }

        private struct AttachmentModel
        {
            [JsonProperty("s")]
            public object AttachmentLinks;
        }

        private struct AttachmentLinkModel
        {
            [JsonProperty("t")]
            public string MiniImageUri;
        }

        static void Main(string[] args)
        {            
            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                string path = args[0];
                var prog = new Program();
                prog.ParseJson(path);
            }
        }

        private void ParseJson(string path)
        {
            try
            {                
                string jsonString = string.Empty;
                using (var sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        jsonString += line;
                    }
                }
                var baseModel = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                var header = JsonConvert.DeserializeObject<Header>(baseModel["meta"].ToString());
                var messages = JsonConvert.DeserializeObject<List<MessageModel>>(baseModel["data"].ToString());                
                using (var sw = File.CreateText($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\{header.Name}.html"))
                {
                    var htmlText = "<link rel='stylesheet' type='text/css' href='style.css'>" +
                    "<script src='clusterize.js' async></script>" +
                    "<script>" +
                    "\ndocument.onreadystatechange = function () {" +
                    "\nvar state = document.readyState" +
                    "\nif (state == 'interactive') {" +
                    "\ndocument.getElementById('contents').style.visibility='hidden';" +
                    "\n} else if (state == 'complete') {" +
                    "\nsetTimeout(function(){" +
                    "\ndocument.getElementById('interactive');" +
                    "\ndocument.getElementById('loader').style.visibility='hidden';" +
                    "\ndocument.getElementById('contents').style.visibility='visible';" +
                    "\n},1000);" +
                    "\n}\n}\n</script>\n" +
                    "<div id='loader' class='spinner'><div class='dot1'></div><div class='dot2'></div></div>" +
                    "<div class='widget' id='contents' style='visibility: hidden;'>" +
                    "<div class='widget-header'>Dialog Dumper v1.2 [by Splinter]</div>" +
                    "<div class='widget-conversation' id='scrollArea'>" +
                    "<ul id='conversation'>";
                    sw.Write(htmlText);
                    var defaultRecipId = messages[0].RecipientID;
                    for (int i = 0; i < 1500; i++)
                    {
                        htmlText = string.Empty;
                        var message = messages[i];
                        string className = "message-left";
                        string authorName = header.Name.Split(' ')[0];
                        string avaUri = "";
                        if (message.RecipientID == defaultRecipId)
                        {
                            className = "message-right";
                            authorName = "SomeAuthorName";
                            avaUri = "";
                        }
                        htmlText += $"<li class='{className}'>" +
                            "<div class='message-avatar'>" +
                            $"<div class='avatar' style='background-image: url({avaUri}); background-size:contain;'></div>" +
                            $"<div class='name'>{authorName}</div></div>";
                        if (message.Attachments != null)
                        {
                            string link = string.Empty;
                            foreach (var obj in message.Attachments)
                            {
                                var attach = JsonConvert.DeserializeObject<AttachmentModel>(obj.ToString());
                                if (attach.AttachmentLinks != null && attach.AttachmentLinks.ToString().Contains("http"))
                                    link = JsonConvert.DeserializeObject<AttachmentLinkModel>(attach.AttachmentLinks.ToString()).MiniImageUri;
                            }
                            htmlText += $"<div class='message-text'>{message.Content}<img style='max-width:160px;' src='{link}'/></div>";
                        }
                        else htmlText += $"<div class='message-text'>{message.Content}</div>";
                        var time = ConvertFromUnixTimestamp(header.BaseData - message.DateOffset);
                        htmlText += $"<div class='message-hour'>{time.ToShortDateString()} {time.ToShortTimeString()}</div></li>";
                        sw.Write(htmlText);
                        Console.Out.WriteLine($"index: {i}; author: {authorName}");
                    }
                    htmlText = "</ul></div></div><script>var clusterize = new Clusterize({scrollId: 'scrollArea',contentId: 'conversation', blocks_in_cluster: 2, rows_in_block: 12});</script>";
                    sw.Write(htmlText);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine($"error: {ex.GetType()} {ex.Message}");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Enter)
                        return;
                }
            }
        }

        private DateTime ConvertFromUnixTimestamp(uint timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
    }
}