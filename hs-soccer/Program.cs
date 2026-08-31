using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HsSoccer.Models;
using HsSoccer.Services;

namespace HsSoccer
{
	public class Program
	{
		public static async Task Main( string[] args )
		{
			Console.WriteLine( "============================================================" );
			Console.WriteLine( "  Oregon JV2 Soccer Parent Volunteer Management Console    " );
			Console.WriteLine( "============================================================" );
			Console.WriteLine();

			if ( args == null || args.Length == 0 )
			{
				PrintUsage();
				return;
			}

			var command = args[0].ToLowerInvariant();
			var targetSheetId = args.Length > 1 ? args[1] : string.Empty;

			var rosterManager = new RosterManager();
			var config = rosterManager.LoadConfig();
			var sheetsService = new GoogleSheetsService();
			var gmailHandler = new GmailServiceHandler();
			var emailParser = new EmailParser();

			try
			{
				switch ( command )
				{
					case "setup":
						if ( string.IsNullOrWhiteSpace( targetSheetId ) )
						{
							Console.WriteLine( "Error: Missing target Google Sheet ID." );
							Console.WriteLine( "Usage: dotnet run -- setup <SHEET_ID>" );
							return;
						}
						Console.WriteLine( "Initializing Google Sheet worksheets for " + config.FullTeamTitle + " (Sheet ID: " + targetSheetId + ")..." );
						await sheetsService.SetupSpreadsheetStructureAsync( targetSheetId );
						Console.WriteLine( "SUCCESS: Worksheets initialized (Roster, Budget Ledger, Reimbursements, Team Dinners, Master Schedule)." );
						break;

					case "seed":
						if ( string.IsNullOrWhiteSpace( targetSheetId ) )
						{
							Console.WriteLine( "Error: Missing target Google Sheet ID." );
							Console.WriteLine( "Usage: dotnet run -- seed <SHEET_ID>" );
							return;
						}
						Console.WriteLine( "Loading seed data for " + config.FullTeamTitle + "..." );
						var players = rosterManager.LoadRosterSeed();
						var budget = rosterManager.LoadBudgetSeed();
						var schedule = rosterManager.LoadScheduleSeed();
						var dinners = rosterManager.GenerateDefaultDinnerSlots();

						Console.WriteLine( "Ensuring worksheet tabs exist..." );
						await sheetsService.SetupSpreadsheetStructureAsync( targetSheetId );

						Console.WriteLine( "Seeding Dashboard Landing tab..." );
						await sheetsService.SeedDashboardTabAsync( targetSheetId );

						Console.WriteLine( "Seeding 23 players to Roster tab..." );
						await sheetsService.SeedRosterAsync( targetSheetId, players );

						Console.WriteLine( "Seeding budget ledger..." );
						await sheetsService.SeedBudgetAsync( targetSheetId, budget );

						Console.WriteLine( "Seeding " + schedule.Count + " events to Master Schedule..." );
						await sheetsService.SeedScheduleAsync( targetSheetId, schedule );

						Console.WriteLine( "Seeding team dinner slots (Min 3 / Max 5 parent limits & formulas)..." );
						await sheetsService.SeedDinnersAsync( targetSheetId, dinners );

						Console.WriteLine( "Seeding Team Info tab (dues amount, payment methods & budget breakdown)..." );
						await sheetsService.SeedTeamInfoTabAsync( targetSheetId );

						// Console.WriteLine( "Seeding Co-Managers Dues Log tab..." );
						// await sheetsService.SeedDuesLogAsync( targetSheetId );

						// Console.WriteLine( "Seeding Public Dinner Signup Status tab..." );
						// await sheetsService.SeedPublicDinnerStatusAsync( targetSheetId );

						Console.WriteLine( "SUCCESS: All sheets seeded successfully for " + config.FullTeamTitle + "!" );
						break;

					case "report":
						Console.WriteLine( "=== " + config.FullTeamTitle.ToUpper() + " FINANCIAL & ROSTER SUMMARY ===" );
						var currentPlayers = rosterManager.LoadRosterSeed();
						var currentBudget = rosterManager.LoadBudgetSeed();
						var currentSchedule = rosterManager.LoadScheduleSeed();

						var totalDuesNeeded = currentPlayers.Sum( p => p.DuesRequired );
						var totalDuesPaid = currentPlayers.Sum( p => p.DuesPaid );
						var totalExpenses = currentBudget.Sum( b => b.AllocatedAmount );

						Console.WriteLine( "Program / Team Level:     " + config.ProgramName + " - " + config.TeamLevel + " (" + config.SeasonYear + ")" );
						Console.WriteLine( "Total Players Registered: " + currentPlayers.Count );
						Console.WriteLine( "Communicated Player Dues: " + config.DefaultDuesAmount.ToString( "C2" ) + " per player" );
						Console.WriteLine( "Total Dues Expected:       " + totalDuesNeeded.ToString( "C2" ) );
						Console.WriteLine( "Total Dues Collected:      " + totalDuesPaid.ToString( "C2" ) );
						Console.WriteLine( "Total Planned Expenses:    " + totalExpenses.ToString( "C2" ) );
						Console.WriteLine( "Net Surplus / Slush Fund:  " + ( totalDuesNeeded - totalExpenses ).ToString( "C2" ) );
						Console.WriteLine();
						Console.WriteLine( "=== UNPAID / PENDING DUES LIST ===" );
						foreach ( var player in currentPlayers.Where( p => p.Balance > 0 ) )
						{
							Console.WriteLine( " - " + player.FirstName + " " + player.LastName + " | Balance: " + player.Balance.ToString( "C2" ) + " | Parents: " + string.Join( ", ", player.ParentEmails ) );
						}
						Console.WriteLine();
						Console.WriteLine( "=== UPCOMING EVENTS & DINNERS ===" );
						foreach ( var item in currentSchedule.Take( 10 ) )
						{
							Console.WriteLine( " [" + item.Date + " @ " + item.Time + "] " + item.Category.ToUpper() + ": " + item.OpponentOrEvent + " (" + item.Location + ")" );
						}
						break;

					case "fetch-emails":
						Console.WriteLine( "Scanning Gmail inbox using query: " + config.GmailSearchQuery );
						var emailMessages = await gmailHandler.FetchSoccerEmailsAsync( config.GmailSearchQuery );
						Console.WriteLine( "Found " + emailMessages.Count + " matching email messages in inbox." );
						foreach ( var msg in emailMessages )
						{
							var snippet = msg.Snippet ?? string.Empty;
							var claim = emailParser.ParseReimbursementEmail( "parent@example.com", "Email Item", snippet );
							Console.WriteLine( " - Email ID: " + msg.Id + " | Snippet: " + snippet.Substring( 0, Math.Min( 60, snippet.Length ) ) );
							if ( claim.Amount > 0 )
							{
								Console.WriteLine( "   Detected Claim Amount: $" + claim.Amount + " | Category: " + claim.ExpenseCategory );
							}
						}
						break;

					case "remind":
						Console.WriteLine( "Scanning roster for parents with unpaid dues for " + config.FullTeamTitle + "..." );
						var unpaidPlayers = rosterManager.LoadRosterSeed().Where( p => p.Balance > 0 ).ToList();
						Console.WriteLine( "Found " + unpaidPlayers.Count + " players with unpaid balances." );
						foreach ( var player in unpaidPlayers )
						{
							foreach ( var email in player.ParentEmails )
							{
								Console.WriteLine( "Sending dues reminder to: " + email + " for player " + player.FirstName + " " + player.LastName + "..." );
								await gmailHandler.SendDuesReminderAsync( player, email, config );
							}
						}
						Console.WriteLine( "SUCCESS: All payment reminders dispatched." );
						break;

					case "push-github":
						Console.WriteLine( "Pushing latest code and web portal to GitHub repository..." );
						var ghSvc = new GitHubSyncService();
						await ghSvc.PushAllProjectFilesAsync();
						break;

					case "reset-github":
						var ghSvcReset = new GitHubSyncService();
						await ghSvcReset.ForceResetRepoHistoryAsync();
						break;

					case "crypto-gen":
						var csvUrl = "https://docs.google.com/spreadsheets/d/1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA/gviz/tq?tqx=out:csv&sheet=Team%20Dinners";
						var passcode = "Panthers2026";
						using ( var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes( passcode, 16, 100000, System.Security.Cryptography.HashAlgorithmName.SHA256 ) )
						{
							var salt = pbkdf2.Salt;
							var key = pbkdf2.GetBytes( 32 );
							using ( var aes = new System.Security.Cryptography.AesGcm( key ) )
							{
								var nonce = new byte[12];
								System.Security.Cryptography.RandomNumberGenerator.Fill( nonce );
								var plainBytes = System.Text.Encoding.UTF8.GetBytes( csvUrl );
								var cipherBytes = new byte[plainBytes.Length];
								var tag = new byte[16];
								aes.Encrypt( nonce, plainBytes, cipherBytes, tag );

								var resultObj = new
								{
									salt = Convert.ToBase64String( salt ),
									iv = Convert.ToBase64String( nonce ),
									ciphertext = Convert.ToBase64String( cipherBytes ),
									tag = Convert.ToBase64String( tag )
								};
								Console.WriteLine( System.Text.Json.JsonSerializer.Serialize( resultObj ) );
							}
						}
						break;

					case "update-dinner-formulas":
					case "sync-dinner-form":
						var sheetIdToUse = string.IsNullOrWhiteSpace( targetSheetId ) ? "1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA" : targetSheetId;
						var sheetsDinnerSync = new GoogleSheetsService();
						await sheetsDinnerSync.InitializeAsync();

						Console.WriteLine( "Updating 'Team Dinners' and 'Team Info' tracking tab formulas..." );
						var defaultDinners = rosterManager.GenerateDefaultDinnerSlots();
						await sheetsDinnerSync.SeedDinnersAsync( sheetIdToUse, defaultDinners );
						await sheetsDinnerSync.SeedTeamInfoTabAsync( sheetIdToUse, defaultDinners );

						Console.WriteLine( "SUCCESS: 'Team Dinners' and 'Team Info' tracking tab formulas updated!" );
						break;

					case "sync-dues-dropdown":
						Console.WriteLine( "Reading live roster from Google Sheet..." );
						var allRosterPlayers = rosterManager.LoadRosterSeed();

						Console.WriteLine( "Configuring Dues Form for all " + allRosterPlayers.Count + " roster players..." );
						var formsSvcSync = new GoogleFormsService();
						await formsSvcSync.InitializeAsync();
						await formsSvcSync.ConfigureDuesFormAsync( "1RnY-KJ-r29IKLJN_rtXYahsWdu6TNepmpINRRY4No18", allRosterPlayers );
						break;

					case "clean-form-tabs":
						Console.WriteLine( "Standardizing Form Responses tab layouts..." );
						var sheetsSvcClean = new GoogleSheetsService();
						await sheetsSvcClean.InitializeAsync();
						await sheetsSvcClean.CleanFormResponseTabsAsync( targetSheetId ?? "1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA" );
						break;

					case "inspect-tabs":
						var sheetsSvcInspect = new GoogleSheetsService();
						await sheetsSvcInspect.InitializeAsync();
						await sheetsSvcInspect.InspectAllTabsAsync( targetSheetId ?? "1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA" );
						break;

					case "check-dues":
						var sheetsSvcCheck = new GoogleSheetsService();
						await sheetsSvcCheck.InitializeAsync();
						var unpaidList = await sheetsSvcCheck.GetLiveUnpaidPlayersAsync( targetSheetId ?? "1Cmpw5ENypjUQmuzkmfoYIsimyHQjL8AjI1WxMHVcXnA" );
						Console.WriteLine( "Remaining Unpaid Players Count: " + unpaidList.Count );
						foreach ( var p in unpaidList )
						{
							Console.WriteLine( " - " + p.LastName + ", " + p.FirstName + " (Paid: $" + p.DuesPaid + " / Due: $" + p.Balance + ")" );
						}
						break;

					case "audit-forms":
						Console.WriteLine( "Auditing and verifying Form behavior & Master Sheet data pipelines..." );
						var formAuditService = new GoogleFormsService();
						await formAuditService.AuditAndUpdateFormQuestionsAsync( "1Ol68WmioL42GO_n47N6Cq3g30o_meeqYk9Hjn2SqPD8", "DINNER" );
						await formAuditService.AuditAndUpdateFormQuestionsAsync( "1a38G6PpgwZrqMzIUNCVxXQwvLsdP-va-jLdnUzxE91o", "EXPENSE" );

						Console.WriteLine();
						Console.WriteLine( "=========================================================================" );
						Console.WriteLine( "FORM AUDIT & MASTER SHEET DATA PIPELINE SUMMARY:" );
						Console.WriteLine( "-------------------------------------------------------------------------" );
						Console.WriteLine( "1. TEAM DINNER SIGN-UP FORM (ID: 1Ol68WmioL42GO_n47N6Cq3g30o_meeqYk9Hjn2SqPD8)" );
						Console.WriteLine( "   Target Tab:   Team Dinners & Dinner Signup Status (Public)" );
						Console.WriteLine( "   Required:     Parent Name, Player Name, Email/Phone, Dinner Date, Category" );
						Console.WriteLine( "   Rule:         Capacity Min 3 / Max 5 parents per date (Formula: =IF(H2>=5,\"FULL\",...))" );
						Console.WriteLine();
						Console.WriteLine( "2. EXPENSE & REIMBURSEMENT FORM (ID: 1a38G6PpgwZrqMzIUNCVxXQwvLsdP-va-jLdnUzxE91o)" );
						Console.WriteLine( "   Target Tab:   Reimbursements" );
						Console.WriteLine( "   Required:     Purchaser Name, Email, Category, Amount ($), Description" );
						Console.WriteLine( "   Rule:         Feeds into Reimbursements ledger; Co-managers mark Paid when reimbursed" );
						Console.WriteLine();
						Console.WriteLine( "3. $75 DUES COLLECTION LOG FORM" );
						Console.WriteLine( "   Target Tab:   Dues Log (Co-Managers)" );
						Console.WriteLine( "   Required:     Player Name, Amount ($75), Date, Payment Method, Logged By" );
						Console.WriteLine( "   Rule:         Roster formula =SUMIF() auto-updates Dues Paid to $75.00 & Balance to $0.00 (Paid)" );
						Console.WriteLine( "=========================================================================" );
						break;

					default:
						Console.WriteLine( "Unknown command: '" + command + "'" );
						PrintUsage();
						break;
				}
			}
			catch ( Exception ex )
			{
				Console.WriteLine( "ERROR Executing Command: " + ex.Message );
				Console.WriteLine( ex.StackTrace );
			}
		}

		private static void PrintUsage()
		{
			Console.WriteLine( "Available Commands:" );
			Console.WriteLine( "  dotnet run -- setup <SHEET_ID>        Initializes worksheets in your Google Sheet" );
			Console.WriteLine( "  dotnet run -- seed <SHEET_ID>         Populates live Google Sheet with roster, budget & schedule" );
			Console.WriteLine( "  dotnet run -- report                  Displays financial summary and unpaid dues" );
			Console.WriteLine( "  dotnet run -- fetch-emails            Scans Gmail for claims & RSVPs" );
			Console.WriteLine( "  dotnet run -- remind                  Sends automated payment reminder emails" );
		}
	}
}
