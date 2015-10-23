using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;

namespace UguuSharp
{
    delegate void OnFileProgress(string filename, float progress);
    delegate void OnFileComplete(string filename, string url);

    class Uploader
    {
        public event OnFileProgress FileProgress;
        public event OnFileComplete FileComplete;

        public Task UploadFile(string filename, string name = "")
        {
            return UploadFile(new FileStream(filename, FileMode.Open), name == "" ? Path.GetFileName(filename) : name);
        }

        public Task UploadFile(byte[] data, string name = "image.png")
        {
            return UploadFile(new MemoryStream(data), name);
        }

        public Task UploadFile(Stream input, string name = "image.png")
        {
            return Task.Factory.StartNew(delegate
            {
                string url = "";
                var stream = Upload("http://uguu.se/api.php?d=upload-tool", name, input);

                StreamReader sr = new StreamReader(stream);
                url = sr.ReadToEnd();
                sr.Close();

                if (FileComplete != null)
                    FileComplete(name, url);
            });
        }

        public Stream Upload(string url, string filename, Stream fileStream)
        {
            HttpContent stringContent = new StringContent(filename);
            HttpContent fileStreamContent = new StreamContent(fileStream);

            using (var handler = new ProgressMessageHandler())
            using (var client = HttpClientFactory.Create(handler))
            using (var formData = new MultipartFormDataContent())
            {
                client.Timeout = new TimeSpan(1, 0, 0); // 1 hour should be enough probably

                formData.Add(fileStreamContent, "file", filename);

                handler.HttpSendProgress += (s, e) =>
                {
                    float prog = (float)e.BytesTransferred / (float)fileStream.Length;
                    prog = prog > 1 ? 1 : prog;

                    if (FileProgress != null)
                        FileProgress(filename, prog);
                };

                var response_raw = client.PostAsync(url, formData);

                var response = response_raw.Result;

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                return response.Content.ReadAsStreamAsync().Result;
            }
        }
    }
}
