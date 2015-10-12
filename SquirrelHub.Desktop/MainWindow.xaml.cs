using Squirrel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;

namespace SquirrelHub.Desktop
{
	public partial class MainWindow : Window
	{
		public string SquirrelHubVersion { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			CheckForUpdates();
		}

		private async void CheckForUpdates()
		{
			bool isCanary;
			if (!bool.TryParse(ConfigurationManager.AppSettings["IsCanary"], out isCanary))
				isCanary = false;

			try
			{
				using (var updateManager = await UpdateManager.GitHubUpdateManager("https://github.com/stajs/SquirrelHub", prerelease: isCanary))
				{
					var currentVersion = updateManager.CurrentlyInstalledVersion();

					SquirrelHubVersion = "Checking for update...";

					var hasFailed = false;
					var failTitle = "";
					var failMessage = "";

					try
					{
						var updateInfo = await updateManager.CheckForUpdate();

						if (updateInfo == null)
						{
							SquirrelHubVersion = string.Format("No updates found, staying on v{0}", currentVersion);
						}
						else if (!updateInfo.ReleasesToApply.Any())
						{
							SquirrelHubVersion = string.Format("You're up to date! v{0}", currentVersion);
						}
						else
						{
							var latestVersion = updateInfo
								.ReleasesToApply
								.OrderByDescending(u => u.Version)
								.First()
								.Version;

							if (currentVersion != null && currentVersion > latestVersion)
							{
								SquirrelHubVersion = string.Format("Only found earlier version v{0}, staying on v{1}", latestVersion, currentVersion);
								return;
							}

							SquirrelHubVersion = string.Format("Updating to v{0}", latestVersion);

							var releases = updateInfo.ReleasesToApply;
							await updateManager.DownloadReleases(releases);
							await updateManager.ApplyReleases(updateInfo);

							SquirrelHubVersion = string.Format("Restart to finish update from v{0} to v{1}", currentVersion, latestVersion);
						}
					}
					catch (Exception e)
					{
						// TODO: Have better error handling.
						SquirrelHubVersion = "See https://github.com/lic-nz/Malone/wiki/Help-with-updating-Malone";

						hasFailed = true;
						failTitle = e.Message;
						failMessage = string.Format("currentVersion: {0}\n\n{1}", currentVersion, e.StackTrace);
					}

					if (hasFailed && isCanary)
					{
						// Can't await in a catch block. Move back when on C# 6.
						//await _dialogManager.Show(failTitle, failMessage);
					}
				}
			}
			catch (Exception e)
			{
				if (e.Message.Contains("Update.exe not found"))
				{
					SquirrelHubVersion = "Update disabled";
					return;
				}

				throw;
			}
		}
	}
}
