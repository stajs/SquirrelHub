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

					Log.Text += "\nChecking for update...";

					try
					{
						var updateInfo = await updateManager.CheckForUpdate();

						if (updateInfo == null)
						{
							Log.Text += string.Format("\nNo updates found, staying on v{0}", currentVersion);
						}
						else if (!updateInfo.ReleasesToApply.Any())
						{
							Log.Text += string.Format("\nYou're up to date! v{0}", currentVersion);
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
								Log.Text += string.Format("\nOnly found earlier version v{0}, staying on v{1}", latestVersion, currentVersion);
								return;
							}

							Log.Text += string.Format("\nUpdating to v{0}", latestVersion);

							var releases = updateInfo.ReleasesToApply;
							await updateManager.DownloadReleases(releases);
							await updateManager.ApplyReleases(updateInfo);

							Log.Text += string.Format("\nRestart to finish update from v{0} to v{1}", currentVersion, latestVersion);
						}
					}
					catch (Exception e)
					{
						Log.Text += string.Format("\ncurrentVersion: {0}\n\n{1}", currentVersion, e.StackTrace);
					}
				}
			}
			catch (Exception e)
			{
				if (e.Message.Contains("Update.exe not found"))
				{
					Log.Text += "\nUpdate disabled";
					return;
				}

				throw;
			}
		}
	}
}
