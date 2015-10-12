﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet;
using System.Collections.Generic;

namespace SquirrelHub.Packager
{
	public class Releasifyer
	{
		public string PackagerDirectory { get; private set; }
		public string BuildDirectory { get; private set; }
		public string ReleasesDirectory { get; private set; }
		public string ClientDirectory { get; private set; }
		public string ClientBinDirectory { get; private set; }
		public string Squirrel { get; private set; }
		public string AppIcon { get; private set; }

		public Releasifyer()
		{
			// Assume we are running in project bin folder.
			PackagerDirectory = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, "..", "..", ".."));

			BuildDirectory = Path.GetFullPath(Path.Combine(PackagerDirectory, "build"));
			ReleasesDirectory = Path.GetFullPath(Path.Combine(BuildDirectory, "Releases"));
			ClientDirectory = Path.GetFullPath(Path.Combine(PackagerDirectory, "..", "SquirrelHub.Desktop"));
			ClientBinDirectory = Path.GetFullPath(Path.Combine(ClientDirectory, "bin", "Release"));
			AppIcon = Path.GetFullPath(Path.Combine(ClientDirectory, "App.ico"));

			var nugetPackagesDirectory = Path.GetFullPath(Path.Combine(ClientDirectory, "..", "packages"));
			var squirrelPackageDirectory = Directory.GetDirectories(nugetPackagesDirectory, "squirrel.windows.*").Single();

			Squirrel = Path.GetFullPath(Path.Combine(squirrelPackageDirectory, "tools", "Squirrel.exe"));
		}

		public void Releasify()
		{
			var nugget = CreateNugget();
			Releasify(nugget);
		}

		private static void StartProcess(string fileName, string args)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = fileName,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true
				}
			};

			try
			{
				process.Start();
			}
			catch (Exception innerException)
			{
				throw new Exception(string.Format("Is {0} in your PATH?", fileName), innerException);
			}

			var reader = process.StandardOutput;
			var output = reader.ReadToEnd();

			Console.WriteLine(output);

			process.WaitForExit();
			process.Close();
		}

		private string CreateNugget()
		{
			var csproj = Path.GetFullPath(Path.Combine(ClientDirectory, "SquirrelHub.Desktop.csproj"));
			var bin = Path.GetFullPath(Path.Combine(ClientDirectory, "bin", "Release"));
			var buildDirectoryInfo = Directory.CreateDirectory(BuildDirectory);

			Directory.SetCurrentDirectory(buildDirectoryInfo.FullName);

			// Clean out build directory.
			buildDirectoryInfo
				.GetFiles("*.nupkg")
				.ToList()
				.ForEach(p => p.Delete());

			// Rely on standard nuget process to build the project and create a starting package to copy metadata from.
			StartProcess("nuget.exe", string.Format("pack {0} -Build -Prop Configuration=Release", csproj));

			var nupkg = buildDirectoryInfo.GetFiles("*.nupkg").Single();
			var package = new ZipPackage(nupkg.FullName);

			// Copy all of the metadata *EXCEPT* for dependencies. Kill those.
			var manifest = new ManifestMetadata
			{
				Id = package.Id,
				Version = package.Version.ToString(),
				Authors = string.Join(", ", package.Authors),
				Copyright = package.Copyright,
				DependencySets = null,
				Description = package.Description,
				Title = package.Title,
				IconUrl = package.IconUrl.ToString(),
				ProjectUrl = package.ProjectUrl.ToString(),
				LicenseUrl = package.LicenseUrl.ToString()
			};

			const string target = @"lib\net45";

			// Include dependencies in the package.
			var files = new List<ManifestFile>
			{
				new ManifestFile { Source = "*.dll", Target = target },
				new ManifestFile { Source = "SquirrelHub.exe", Target = target },
				new ManifestFile { Source = "SquirrelHub.exe.config", Target = target },
			};

			var builder = new PackageBuilder();
			builder.Populate(manifest);
			builder.PopulateFiles(bin, files);

			var nugget = Path.Combine(buildDirectoryInfo.FullName, nupkg.Name);

			using (var stream = File.Open(nugget, FileMode.OpenOrCreate))
			{
				builder.Save(stream);
			}

			return nugget;
		}

		private void Releasify(string nugget)
		{
			StartProcess(Squirrel, string.Format("--releasify={0} --setupIcon={1}", nugget, AppIcon));

			var version = Path.GetFileNameWithoutExtension(nugget);
			version = version.Replace("SquirrelHub.", string.Empty);
			var versionedSetup = Path.Combine(ReleasesDirectory, string.Format("Setup-SquirrelHub-{0}.exe", version));
			var setup = new FileInfo(Path.Combine(ReleasesDirectory, "Setup.exe"));

			if (File.Exists(versionedSetup))
				File.Delete(versionedSetup);

			setup.MoveTo(versionedSetup);
		}
	}
}