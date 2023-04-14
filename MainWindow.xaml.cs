using HtmlAgilityPack;
using Models;
using Newtonsoft.Json;
using ObisoftNet.Encoders;
using ObisoftNet.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;

using System.Windows;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using MessageBox = System.Windows.MessageBox;

namespace Fch_Vpn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		public static HttpServer server;
		public static System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();

		private System.Windows.Forms.ContextMenu contextMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		public MainWindow()
		{
			InitializeComponent();

			ServicePointManager.DefaultConnectionLimit = 100;
			
			nIcon.Icon = new Icon(@".\Imagenes\icon.ico");
			nIcon.Visible = true;
			nIcon.ShowBalloonTip(5000, "Free Chunk Vpn", "Server Runing In Background!", System.Windows.Forms.ToolTipIcon.Info);
			nIcon.Click += nIcon_Click;
			this.contextMenu1 = new System.Windows.Forms.ContextMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			// Initialize contextMenu1
			this.contextMenu1.MenuItems.AddRange(
			new System.Windows.Forms.MenuItem[] { this.menuItem1 });
			// Initialize menuItem1
			this.menuItem1.Index = 0;
			this.menuItem1.Text = "Salir";
			this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
			nIcon.ContextMenu = this.contextMenu1;
			try
			{
				startServer();
			}
			catch (Exception m)
			{
				System.Windows.MessageBox.Show(m.Message);
			}
		}



		private void menuItem1_Click(object Sender, EventArgs e)
		{
			// Close the form, which closes the application.
			nIcon.Visible = false;
			server.Dispose();
			this.Close();
			Process current = Process.GetCurrentProcess();
			current.Kill();
		}
		void nIcon_Click(object sender, EventArgs e)
		{
			//events comes here
			//this.Visibility = Visibility.Hidden;
			//this.WindowState = WindowState.Normal;
		}

		public static void startServer()
		{
			try
			{
				if (server == null)
				{
					server = new HttpServer(baseGet);
					server.Run(7890);
				}
			}
			catch (Exception m)
			{
				System.Windows.MessageBox.Show(m.Message);
			}
		}

		public static HttpSession MakeSessFromData(ChunkData data)
		{
			HttpSession sess = new HttpSession();

			var text = $"sid:{data.sid}\n";
			text += $"user:{data.username}\n";
			text += $"passw:{data.password}\n";
			/*
			Dispatcher.BeginInvokeOnMainThread(()=>{
			   		Toast.Show(text,Toast.ToastLength.Long);
			   	});
			*/
			if (data.sid == "moodle")
			{

				string login = $"{data.host}login/index.php";
				var html = sess.GetString(login);


				var tags = new Dictionary<string, string>();

				string anchor = "";
				string logintoken = "";

				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(html);

				var inputs = doc.DocumentNode.Descendants("input");

				foreach (HtmlNode inp in inputs)
				{
					if (inp.Attributes["name"].Value == "anchor")
						anchor = inp.Attributes["value"].Value;
					if (inp.Attributes["name"].Value == "logintoken")
						logintoken = inp.Attributes["value"].Value;
				}

				var payload = new Dictionary<string, string>();
				payload.Add("anchor", anchor);
				payload.Add("logintoken", logintoken);
				payload.Add("username", data.username);
				payload.Add("password", data.password);
				payload.Add("rememberusername", "1");

				if (logintoken == "") return null;

				var resp = sess.Post(login, data: payload) as HttpWebResponse;


				if (resp.ResponseUri.ToString() != login)
				{
					return sess;
				}


			}
			else
			{

				string login = $"{data.host}index.php/{data.sid}/login";
				var html = sess.GetString(login);
				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(html);

				string csrftoken = "";

				var inputs = doc.DocumentNode.Descendants("input");
				foreach (HtmlNode inp in inputs)
				{
					if (inp.Attributes["name"].Value == "csrfToken")
						csrftoken = inp.Attributes["value"].Value;

				}

				if (csrftoken == "") return null;

				var payload = new Dictionary<string, string>();
				payload.Add("csrfToken", csrftoken);
				payload.Add("source", "");
				payload.Add("username", data.username);
				payload.Add("password", data.password);
				payload.Add("remember", "1");

				login += "/signIn";

				var resp = sess.Post(login, data: payload);

				if (resp.ResponseUri.ToString() != login)
					return sess;

			}


			return null;
		}

		public static ChunkData GetData(string url)
		{
			var sess = new HttpSession();


			string json = sess.GetString(url);

			//ChunkData data = JsonSerializer.Deserialize<ChunkData>(json);
			ChunkData data = JsonConvert.DeserializeObject<ChunkData>(json);
			if (data != null)
			{
				data.username = S6Encoder.Decrypt(data.username);
				data.password = S6Encoder.Decrypt(data.password);
			}


			return data;
		}

		public static Dictionary<string, ChunkData> Datas = new Dictionary<string, ChunkData>();

		public static void baseGet(HttpListenerRequest req, HttpListenerResponse resp, RouteResult result)
		{
			try
			{
				if (result.Url.Contains("chunks"))
				{

					string key = "obisoftdev2023";
					string url = result.Url.Replace("127.0.0.1:7890", $"freechunkdl.s3.ydns.eu/get/{key}");



					ChunkData data = null;

					if (Datas.TryGetValue(url, out data)){}
					else
					{
						data = GetData(url);
					}
					
					bool geted = true;
					if (data != null)
					{

						HttpSession sess = null;

						if (data.session != null)
						{
							sess = data.session;
						}
						else
						{
							sess = MakeSessFromData(data);
						}
						
						if (sess != null)
						{
							try
							{
								data.session = sess;
								Datas.Add(url, data);
							}catch{}


							resp.ContentLength64 = (long)data.filesize;
							resp.ContentType = "applicacion/octet-stream";
							resp.Headers.Set("Content-Disposition", $"attachment; filename=\"{data.filename}\"");
							resp.Headers.Add("Accept-Ranges","bytes");

							int ichunk = 0;
							Dictionary<string, string> headers = new Dictionary<string, string>();
							bool part = false;

							long bmax = -1;
							double length = 0;
							
							try{
								// get range responses
			    	
								string range = req.Headers.Get("Range").Replace("bytes=","");
			    	
								long bindex = long.Parse(range.Split('-')[0]);
								try
								{
									bmax = long.Parse(range.Split('-')[1]);
								}catch{}

								if (bindex > 0)
								{
									part = true;

									length = data.filesize - bindex;

									long rangesize = (long)data.filesize;
									
									if (bmax != -1)
									{
										length = bmax - bindex;
										rangesize = bmax;
									}

									resp.Headers.Add("Content-Range",$"bytes {bindex}-{rangesize}/{rangesize}");
									
									double chunkbytes = 1024 * 1024 * data.chunksize;

									ichunk = (int)Math.Round(bindex / (double)(1024*1024*data.chunksize));

									float origin = bindex - (1024 * 1024 * data.chunksize * ichunk);
									headers.Add("Range",$"bytes={origin}-");
									
									resp.ContentLength64 = (long)(length);
									
									resp.StatusCode = 206;
								}
								
			    	
							}catch{
			    	
							}
							
							while (true)
							{

								if (!geted)
									data = GetData(url);

								if (geted)
									geted = false;


								long chunkcount = 0;
								
								for (int i = ichunk; i < data.chunks.Length; i++)
								{
									ichunk = i;
									string chunkurl = data.chunks[i];

									if (!part)
										headers = null;
									else
										part = false;
									
									var temp = sess.Get(chunkurl,headers:headers) as HttpWebResponse;
									

									using (Stream stream = temp.GetResponseStream())
									{
										byte[] bytes = new byte[1024];
										int read = 0;
										while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
										{
											resp.OutputStream.Write(bytes, 0, read);
											resp.OutputStream.Flush();

											chunkcount += read;

											if (bmax != -1)
											{
												if(chunkcount>=length)
													break;
											}
										}
									}
								}

								if (bmax != -1)
								{
									if(chunkcount>=length)
										break;
								}
								if (data.state == "finish") break;
							}


						}
					}



				}

			}
			catch (Exception ex)
			{
				nIcon.ShowBalloonTip(5000, "Error", ex.Message, System.Windows.Forms.ToolTipIcon.Error);
				//MessageBox.Show(ex.Message);
			}


		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			server.Dispose();
			e.Cancel = true;
			this.Visibility = Visibility.Hidden;
		}
	}
}
