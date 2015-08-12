using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;

using NDesk.Options;

namespace UguuSharp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string filename = "";
            bool clipboard = false;
            bool image_in_clipboard = false;
            string name = "";

            OptionSet set = new OptionSet();
            set = new OptionSet() 
            { 
                {"c|clipboard", "Copy the URL to the clipboard.", c => clipboard = true},
                {"i|imgclip", "Retrieve the image from the image clipboard.", c => image_in_clipboard = true},
                {"n|name=", "Sets the upload name.", n => name = n },
                {"h|?|help", "Display this text", c => GetHelp(set)}
            };

            filename = set.Parse(args).FirstOrDefault();

            if(image_in_clipboard && Clipboard.ContainsFileDropList())
            {
                image_in_clipboard = false;
                filename = Clipboard.GetFileDropList()[0];
            }

            if (!image_in_clipboard)
            {
                if (!File.Exists(filename))
                {
                    Console.Error.WriteLine("Invalid file or filename.");
                    return;
                }

                // get absolute path so we don't get affected by the currentdir change
                filename = Path.GetFullPath(filename);
            }
            else if (image_in_clipboard && Clipboard.ContainsFileDropList())
            {
                image_in_clipboard = false;
                filename = Clipboard.GetFileDropList()[0];
            }

            // so that .NET can find the lib
            string str = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            Environment.CurrentDirectory = str;

            Uploader uploader = new Uploader();

            int x = Console.CursorTop;
            int y = Console.CursorLeft;

            uploader.FileProgress += (s, p) =>
            {
                Console.SetCursorPosition(y, x);
                Console.WriteLine(GetProgressBar(p, 20, s));
            };

            uploader.FileComplete += (s, url) =>
            {
                Console.WriteLine("{0} done, view it at {1}", s, url);

                if(clipboard)
                {
                    Console.WriteLine("Copied URL to clipboard.");
                    SetClipboardText(url);
                }
            };

            if (image_in_clipboard)
            {
                MemoryStream ms = new MemoryStream();

                if(Clipboard.ContainsImage())
                    Clipboard.GetImage().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                else
                {
                    Console.Error.WriteLine("No image in clipboard.");
                    return;
                }

                ms.Seek(0, SeekOrigin.Begin);

                if (string.IsNullOrWhiteSpace(name))
                    uploader.UploadFile(ms).Wait();
                else
                    uploader.UploadFile(ms, name).Wait();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(name))
                    uploader.UploadFile(filename).Wait();
                else
                    uploader.UploadFile(filename, name).Wait();
            }
        }

        static void SetClipboardText(string text)
        {
            Thread thr = new Thread(() =>
            {
                Clipboard.SetText(text);
            });

            thr.SetApartmentState(ApartmentState.STA);
            thr.Start();
        }

        static void GetHelp(OptionSet set)
        {
            Console.WriteLine();
            Console.WriteLine("Usage: uguusharp [OPTIONS] filename");
            Console.WriteLine("Uploads the specified file to uguu.se and returns the URL.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            set.WriteOptionDescriptions(Console.Out);

            Environment.Exit(0);
        }

        static string GetProgressBar(float fraction, int length, string label)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("[");

            bool place_tip = true;

            for (float f = 0; f < length; f++)
            {
                if ((f / (float)length) < fraction)
                {
                    builder.Append("=");
                }
                else
                {
                    if (place_tip)
                    {
                        builder.Append('>');
                        place_tip = false;
                    }
                    else
                    {
                        builder.Append(" ");
                    }
                }
            }

            builder.Append("] ");

            builder.AppendFormat("{0} {1:0.00}%", label, fraction * 100f);

            return builder.ToString();
        }
    }
}
