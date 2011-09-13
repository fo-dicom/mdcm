using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Monotouch.Dicom.Viewer
{
	/// <summary>
	/// The UIApplicationDelegate for the application. This class is responsible for launching the 
	/// User Interface of the application, as well as listening (and optionally responding) to 
	/// application events from iOS.
	/// </summary>
	public partial class AppDelegate : UIApplicationDelegate
	{
		/// <summary>
		/// This method is invoked when the application has loaded and is ready to run. In this 
		/// method you should instantiate the window, load the UI into it and then make the window
		/// visible.
		/// </summary>
		/// <remarks>
		/// You have 5 seconds to return from this method, or iOS will terminate your application.
		/// </remarks>
		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			window.AddSubview (navigation.View);

			var menu = new RootElement ("DICOM Viewer"){
				new Section ("Connection"){
					new StringElement ("Echo DICOM server", EchoDicomServer),
				}
			};

			var dv = new DialogViewController (menu) {
				Autorotate = true
			};
			navigation.PushViewController (dv, true);				
			
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}
