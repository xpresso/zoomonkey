﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Browser;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;


namespace ZooMonkey
{
	[ScriptableType]
	public partial class MainPage : Canvas
	{
		public MainPage ()
		{
			this.Loaded += delegate { Run (); };
		}

		ZooMonkeyData zmdata;

		ScaleTransform scale_transform = new ScaleTransform ();
		RotateTransform rotate_transform = new RotateTransform ();
		DoubleAnimation [] anims;
		DoubleAnimation aniSX = new DoubleAnimation () { Duration = new Duration (TimeSpan.FromSeconds (1)) };
		DoubleAnimation aniSY = new DoubleAnimation () { Duration = new Duration (TimeSpan.FromSeconds (1)) };
		DoubleAnimation aniCX = new DoubleAnimation () { Duration = new Duration (TimeSpan.FromSeconds (1)) };
		DoubleAnimation aniCY = new DoubleAnimation () { Duration = new Duration (TimeSpan.FromSeconds (1)) };
		DoubleAnimation aniAn = new DoubleAnimation () { Duration = new Duration (TimeSpan.FromSeconds (1)) };
		Storyboard story = new Storyboard ();
		
		void Run ()
		{
			HtmlPage.RegisterScriptableObject ("zooMonkeyController", this);

			anims = new DoubleAnimation [] { aniSX, aniSY, aniCX, aniCY, aniAn };
			story.Children.Add (aniSX);
			story.Children.Add (aniSY);
			story.Children.Add (aniCX);
			story.Children.Add (aniCY);
			Storyboard.SetTarget (aniSX, scale_transform);
			Storyboard.SetTarget (aniSY, scale_transform);
			Storyboard.SetTarget (aniCX, scale_transform);
			Storyboard.SetTarget (aniCY, scale_transform);
			Storyboard.SetTargetProperty (aniSX, new PropertyPath ("ScaleX"));
			Storyboard.SetTargetProperty (aniSY, new PropertyPath ("ScaleY"));
			Storyboard.SetTargetProperty (aniCX, new PropertyPath ("CenterX"));
			Storyboard.SetTargetProperty (aniCY, new PropertyPath ("CenterY"));

			story.Children.Add (aniAn);
			Storyboard.SetTarget (aniAn, rotate_transform);
			Storyboard.SetTargetProperty (aniAn, new PropertyPath ("Angle"));

			var tg = new TransformGroup ();
			tg.Children.Add (scale_transform);
			tg.Children.Add (rotate_transform);
			this.RenderTransform = tg;

			RestoreDataAsync ();
		}

		void ZMDataRestored ()
		{
			//DumpSerializedData(zmdata);
			foreach (var idef in zmdata.Images)
			{
				var img = new Image () { Source = new BitmapImage (new Uri (idef.ImageUrl, UriKind.Relative)), Width = idef.Width, Height = idef.Height };
				Canvas.SetLeft (img, idef.Left);
				Canvas.SetTop (img, idef.Top);
				Children.Add (img);
			}
		}

		void DumpSerializedData (ZooMonkeyData zmd)
		{
			var ser = new DataContractJsonSerializer (typeof (ZooMonkeyData));
			var ms = new MemoryStream ();
			ser.WriteObject (ms, zmd);
			ms.Position = 0;
			Debug.WriteLine (new StreamReader (ms).ReadToEnd ());
		}

		void RestoreDataAsync ()
		{
			var baseUri = Application.Current.Host.Source;
			if (baseUri.Scheme == Uri.UriSchemeFile)
			{
				var ms = Application.GetResourceStream (new Uri("samples/zmdata.json", UriKind.Relative)).Stream;
				var ser = new DataContractJsonSerializer (typeof(ZooMonkeyData));
				zmdata = (ZooMonkeyData) ser.ReadObject (ms);
				ZMDataRestored();
			}
			else
			{
				var wc = new WebClient ();
				wc.DownloadStringCompleted += delegate (object o, DownloadStringCompletedEventArgs e)
				{
					if (e.Cancelled)
						return;
					var s = e.Result;
					var ser = new DataContractJsonSerializer (typeof (ZooMonkeyData));
					zmdata = (ZooMonkeyData) ser.ReadObject (new MemoryStream (Encoding.UTF8.GetBytes (s)));
					ZMDataRestored ();
				};
				var uri = new Uri (Application.Current.Host.Source, "./samples/zmdata.json");
				wc.DownloadStringAsync (uri);
			}
		}

		int current_action = -1;

		[ScriptableMember]
		public void ButtonNextPressed ()
		{
			if (current_action + 1 < zmdata.Actions.Count)
			{
				current_action++;
				var action = zmdata.Actions [current_action];
				RunAction (action);
			}
		}

		[ScriptableMember]
		public void ButtonPreviousPressed ()
		{
			if (current_action > 0)
			{
				--current_action;
				var action = zmdata.Actions [current_action];
				RunAction (action);
			}
		}

		void RunAction (ZooMonkeyAction action)
		{
			foreach (var ani in anims)
				ani.Duration = new Duration (TimeSpan.FromSeconds (action.Duration));
			aniSX.To = action.Zoom;
			aniSY.To = action.Zoom;
			aniCX.To = action.Left;
			aniCY.To = action.Top;
			aniAn.To = action.Angle;

			story.Begin ();
		}
	}
}