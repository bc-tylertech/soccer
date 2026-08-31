using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HsSoccer.Services
{
	public class GitHubSyncService
	{
		private readonly string _token;
		private readonly string _repoOwner = "bc-tylertech";
		private readonly string _repoName = "soccer";
		private readonly HttpClient _client;

		public GitHubSyncService()
		{
			var patPath = Path.Combine( Directory.GetCurrentDirectory(), "github-pat.txt" );
			if ( File.Exists( patPath ) )
			{
				_token = File.ReadAllText( patPath ).Trim();
			}

			_client = new HttpClient();
			_client.DefaultRequestHeaders.UserAgent.Add( new ProductInfoHeaderValue( "hs-soccer-bot", "1.0" ) );
			if ( !string.IsNullOrEmpty( _token ) )
			{
				_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", _token );
			}
		}

		public async Task PushFileAsync( string localFilePath, string remoteRepoPath )
		{
			if ( !File.Exists( localFilePath ) )
			{
				Console.WriteLine( "File not found: " + localFilePath );
				return;
			}

			var fileBytes = await File.ReadAllBytesAsync( localFilePath );
			var base64Content = Convert.ToBase64String( fileBytes );

			var apiUrl = "https://api.github.com/repos/" + _repoOwner + "/" + _repoName + "/contents/" + remoteRepoPath;

			// Get existing file SHA if present
			string existingSha = null;
			try
			{
				var getResponse = await _client.GetAsync( apiUrl );
				if ( getResponse.IsSuccessStatusCode )
				{
					var getJson = await getResponse.Content.ReadAsStringAsync();
					using ( var doc = JsonDocument.Parse( getJson ) )
					{
						if ( doc.RootElement.TryGetProperty( "sha", out var shaProp ) )
						{
							existingSha = shaProp.GetString();
						}
					}
				}
			}
			catch { }

			var bodyObj = new
			{
				message = "Update " + remoteRepoPath + " via HS Soccer Bot",
				content = base64Content,
				sha = existingSha
			};

			var jsonBody = JsonSerializer.Serialize( bodyObj );
			var content = new StringContent( jsonBody, Encoding.UTF8, "application/json" );

			var putResponse = await _client.PutAsync( apiUrl, content );
			if ( putResponse.IsSuccessStatusCode )
			{
				Console.WriteLine( "SUCCESS: Pushed " + remoteRepoPath + " to GitHub (bc-tylertech/soccer)!" );
			}
			else
			{
				var errStr = await putResponse.Content.ReadAsStringAsync();
				Console.WriteLine( "Failed to push " + remoteRepoPath + ": " + putResponse.StatusCode + " - " + errStr );
			}
		}

		public async Task ForceResetRepoHistoryAsync()
		{
			Console.WriteLine( "Wiping commit history and force-resetting repository to a single clean initial commit..." );

			var filesToPush = new (string LocalPath, string RemotePath)[]
			{
				(Path.Combine( Directory.GetCurrentDirectory(), ".gitignore" ), ".gitignore"),
				(Path.Combine( Directory.GetCurrentDirectory(), "Program.cs" ), "hs-soccer/Program.cs"),
				(Path.Combine( Directory.GetCurrentDirectory(), "Services", "GoogleSheetsService.cs" ), "hs-soccer/Services/GoogleSheetsService.cs"),
				(Path.Combine( Directory.GetCurrentDirectory(), "Services", "GoogleFormsService.cs" ), "hs-soccer/Services/GoogleFormsService.cs"),
				(Path.Combine( Directory.GetCurrentDirectory(), "Services", "GitHubSyncService.cs" ), "hs-soccer/Services/GitHubSyncService.cs"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "index.html"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "app.js"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "styles.css" ), "styles.css"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "docs/index.html"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "docs/app.js"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "styles.css" ), "docs/styles.css"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "hs-soccer/web/index.html"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "hs-soccer/web/app.js"),
				(Path.Combine( Directory.GetCurrentDirectory(), "web", "styles.css" ), "hs-soccer/web/styles.css")
			};

			var treeItems = new System.Collections.Generic.List<object>();

			foreach ( var file in filesToPush )
			{
				if ( !File.Exists( file.LocalPath ) )
				{
					continue;
				}

				var bytes = await File.ReadAllBytesAsync( file.LocalPath );
				var base64 = Convert.ToBase64String( bytes );

				// Create Blob
				var blobUrl = "https://api.github.com/repos/" + _repoOwner + "/" + _repoName + "/git/blobs";
				var blobBody = JsonSerializer.Serialize( new { content = base64, encoding = "base64" } );
				var blobResp = await _client.PostAsync( blobUrl, new StringContent( blobBody, Encoding.UTF8, "application/json" ) );

				if ( blobResp.IsSuccessStatusCode )
				{
					var respJson = await blobResp.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse( respJson );
					var blobSha = doc.RootElement.GetProperty( "sha" ).GetString();

					treeItems.Add( new
					{
						path = file.RemotePath,
						mode = "100644",
						type = "blob",
						sha = blobSha
					} );
					Console.WriteLine( "Created blob for " + file.RemotePath );
				}
			}

			// Create Tree
			var treeUrl = "https://api.github.com/repos/" + _repoOwner + "/" + _repoName + "/git/trees";
			var treeBody = JsonSerializer.Serialize( new { tree = treeItems } );
			var treeResp = await _client.PostAsync( treeUrl, new StringContent( treeBody, Encoding.UTF8, "application/json" ) );
			treeResp.EnsureSuccessStatusCode();
			var treeRespJson = await treeResp.Content.ReadAsStringAsync();
			using var treeDoc = JsonDocument.Parse( treeRespJson );
			var treeSha = treeDoc.RootElement.GetProperty( "sha" ).GetString();

			// Create Orphan Root Commit (parents: [])
			var commitUrl = "https://api.github.com/repos/" + _repoOwner + "/" + _repoName + "/git/commits";
			var commitBody = JsonSerializer.Serialize( new
			{
				message = "Initial clean commit - Oregon JV2 Soccer Web Portal & Bot Console",
				tree = treeSha,
				parents = new string[0]
			} );
			var commitResp = await _client.PostAsync( commitUrl, new StringContent( commitBody, Encoding.UTF8, "application/json" ) );
			commitResp.EnsureSuccessStatusCode();
			var commitRespJson = await commitResp.Content.ReadAsStringAsync();
			using var commitDoc = JsonDocument.Parse( commitRespJson );
			var newCommitSha = commitDoc.RootElement.GetProperty( "sha" ).GetString();

			// Force update main branch ref (force: true)
			var refUrl = "https://api.github.com/repos/" + _repoOwner + "/" + _repoName + "/git/refs/heads/main";
			var refBody = JsonSerializer.Serialize( new { sha = newCommitSha, force = true } );
			var refResp = await _client.PatchAsync( refUrl, new StringContent( refBody, Encoding.UTF8, "application/json" ) );
			refResp.EnsureSuccessStatusCode();

			Console.WriteLine( "SUCCESS: Repository history wiped! Single clean root commit force-pushed to main branch." );
		}

		public async Task PushAllProjectFilesAsync()
		{
			Console.WriteLine( "Pushing Oregon JV2 Soccer project files to GitHub..." );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "Program.cs" ), "hs-soccer/Program.cs" );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "Services", "GoogleSheetsService.cs" ), "hs-soccer/Services/GoogleSheetsService.cs" );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "Services", "GoogleFormsService.cs" ), "hs-soccer/Services/GoogleFormsService.cs" );
			
			// Push web files to subfolder, docs/ folder, and repo root for GitHub Pages!
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "hs-soccer/web/index.html" );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "hs-soccer/web/app.js" );

			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "docs/index.html" );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "docs/app.js" );

			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "index.html" ), "index.html" );
			await PushFileAsync( Path.Combine( Directory.GetCurrentDirectory(), "web", "app.js" ), "app.js" );

			Console.WriteLine( "GitHub sync complete!" );
		}
	}
}
